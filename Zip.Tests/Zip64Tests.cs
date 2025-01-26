// Zip64Tests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011, 2025 Dino Chiesa
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// This module defines the tests for the ZIP64 capability within DotNetZip.  These
// tests can take a long time to run, as the files can be quite large - 10g or
// more. Merely generating the content for these tests can take an hour.  Most tests
// in the DotNetZip test suite are self-standing - they generate the content they
// need, and then remove it after completion, either success or failure. With ZIP64,
// because content creation is expensive, for update operations this module uses a
// cache of large zip files.  See _CreateOrFindHugeZipfile().  The method looks for
// large files in a well-known location on the filesystem, in fodderDir.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Apache 2.0 License.
// See the file LICENSE.txt that accompanies the source code, for the license details.
//
// ------------------------------------------------------------------


using Ionic.Zip.Tests.Utilities;
using Xunit.Abstractions;
using Assert = XunitAssertMessages.AssertM;

namespace Ionic.Zip.Tests
{
    public class Zip64Tests : IonicTestClass
    {
        private static string HUGE_ZIPFILE_NAME = "Zip64Test-createdBy-DNZ.zip";
        private string Z64_DIR = null;
        private string _HugeZipFile;

        public Zip64Tests(ITestOutputHelper output)
        {
            _output = output;
            Z64_DIR = _CreateOrFindHugeZipFile();
        }

        // public void Dispose()
        // {
        //     if (_HugeZipFiles != null)
        //     {
        //         // Keep this huge zip file around, because it takes so much
        //         // time to create it. But Delete the directory if one of the files no
        //         // longer exists.
        //         if (!File.Exists(_HugeZipFiles[0]) ||
        //             !File.Exists(_HugeZipFiles[1]))
        //         {
        //             //File.Delete(_HugeZipFile);
        //             string d= Path.GetDirectoryName(_HugeZipFiles[0]);
        //             if (Directory.Exists(d))
        //                 Directory.Delete(d, true);
        //         }
        //     }
        // }

        EncryptionAlgorithm[] crypto =
            {
                EncryptionAlgorithm.None,
                EncryptionAlgorithm.PkzipWeak,
                EncryptionAlgorithm.WinZipAes128,
                EncryptionAlgorithm.WinZipAes256,
            };

        Ionic.Zlib.CompressionLevel[] compLevels =
            {
                Ionic.Zlib.CompressionLevel.None,
                Ionic.Zlib.CompressionLevel.BestSpeed,
                Ionic.Zlib.CompressionLevel.Default,
                Ionic.Zlib.CompressionLevel.BestCompression,
            };

        Zip64Option[] z64 =
            {
                Zip64Option.Never,
                Zip64Option.AsNecessary,
                Zip64Option.Always,
            };

        /// <summary>
        ///   Create a large zip64 zip file via DNZ. Winzip can create these too, but
        ///   I don't have that tool. Each test that updates a zip file will use this.
        /// </summary>
        private string _CreateOrFindHugeZipFile()
        {
            _output.WriteLine("_CreateOrFindHugeZipFile");
            _output.WriteLine("Start - " + DateTime.Now.ToString("G"));
            // STEP 1:
            // look for existing directories, and re-use the large zip files
            // there, if it exists, and if it is large enough.
            var oldDirs = Directory.GetDirectories(TEMP, "*.Zip64Tests");
            string found = null;

            _output.WriteLine("Looking in old directories...");
            foreach (var dir in oldDirs)
            {
                if (found!=null)
                {
                    _output.WriteLine("Not needed {0}, deleting...", dir);
                    // we don't need more than one of these directories
                    TestUtilities.DeleteDirectoryRecursive(0, _output, new DirectoryInfo(dir));
                    continue;
                }

                _output.WriteLine("Looking in {0}...", dir);
                _output.WriteLine("check for fodder files");
                string _fodderDir = Path.Combine(dir, "dir");
                if (Directory.Exists(_fodderDir))
                {
                    // check for files
                    var fodderFiles = Directory.GetFiles(_fodderDir, "*.*");
                    if (fodderFiles != null && fodderFiles.Length > 20)
                    {
                        _output.WriteLine("found {0} fodder files...", fodderFiles.Length);
                        // we have fodder files
                        string zipFileCandidate = Path.Combine(dir, HUGE_ZIPFILE_NAME);
                        if (File.Exists(zipFileCandidate))
                        {
                            _output.WriteLine("huge zip file exists: {0}", zipFileCandidate);
                            FileInfo fi = new FileInfo(zipFileCandidate);
                            if (fi.Length < (long)System.UInt32.MaxValue)
                            {
                                _output.WriteLine("but it's too small ({0} bytes size rating {2:F2}%), deleting it.",
                                    fi.Length, (double)fi.Length/(0.01*System.UInt32.MaxValue) );
                                File.Delete(zipFileCandidate);
                            }
                            else
                            {
                                _output.WriteLine("huge zip file {0} ({1} bytes, size rating {2:F2}%)",
                                    zipFileCandidate.Replace(TEMP,""), fi.Length, (double)fi.Length/(0.01*System.UInt32.MaxValue) );
                                // large zip, and plenty of fodder files, which we
                                // assume are large enough
                                found = dir;
                            }
                        }
                        else
                        {

                            // Fodder files exist, but a huge zip does not exist. So let's
                            // create a few more fodder files, and then create a zip from
                            // them, and see if that's enough.
                            int n = _rnd.Next(2) + 2;
                            _output.WriteLine("creating a few more fodder files {0}...", n);
                            CreateSomeLargeFiles(_fodderDir, n);
                            found = CreateHugeZip(dir);
                        }
                    }
                    else
                    {
                        // not enough fodder files
                    }
                }

                if (found == null)
                {
                    _output.WriteLine("Not sufficient {0}...", dir);
                    // this candidate directory doesn't contain what we need
                    TestUtilities.DeleteDirectoryRecursive(0, _output, new DirectoryInfo(dir));
                }
            }

            if (found!=null) {
                _HugeZipFile = Path.Combine(found, HUGE_ZIPFILE_NAME);
                return found;
            }

            // No fodder files exist. Create some, which will later be zipped.
            string dirContainingHugeZip = TestUtilities.GenerateUniquePathname("Zip64Tests");
            _output.WriteLine("Creating dir {0}...", dirContainingHugeZip);
            Directory.CreateDirectory(dirContainingHugeZip);
            string fodderDir = Path.Combine(dirContainingHugeZip,"dir");
            Directory.CreateDirectory(fodderDir);

            int numFilesToAdd = _rnd.Next(7) + 21;
            CreateSomeLargeFiles(fodderDir, numFilesToAdd);

            // Now, Create a zip file from those fodder files, using DotNetZip.
            // For 4gb+, this will take 50 minutes or so.

            CreateHugeZip(dirContainingHugeZip);

            // The old logic deleted the fodder dir.
            // But it's better to retain the fodder dir, to be able to test the creation of large files.

            // // Delete the fodder dir only if we have both zips.
            // // This is helpful when modifying or editing this method.
            // // With repeated runs you don't have to re-create all the data
            // // each time.
            // if (File.Exists(zipFileToCreate)) {
            //     Directory.Delete(fodderZip, true);
            // }

            _output.WriteLine("All done - " + DateTime.Now.ToString("G"));
            return dirContainingHugeZip;
        }


        private void CreateSomeLargeFiles(string fodderDir, int numFilesToAdd)
        {
            _output.WriteLine("Creating new fodder files...");
            // These params define the size range for the large, random text
            // files that are created below. Creating files this size takes
            // about 1 minute per file
            int _sizeBase =   0x24000000;
            int _sizeRandom = 0x18000000;

            // _output.WriteLine("....FIXME small size for testing...");
            // int _sizeBase =   0x160000;
            // int _sizeRandom = 0x200000;

            for (int i = 0; i < numFilesToAdd; i++)
            {
                string filename = Path.Combine(fodderDir,
                    TestUtilities.GenerateRandomName(_rnd.Next(18) + 9) + ".txt");
                _output.WriteLine("{0} ({1}/{2})", filename.Replace(TEMP,""), i+1, numFilesToAdd);

                int totalSize = _sizeBase + _rnd.Next(_sizeRandom);
                int writtenSoFar = 0;
                int cycles = 0;

                using (var input = new Ionic.Zip.Tests.Utilities.RandomTextInputStream(totalSize))
                {
                    using (var output = File.Create(filename))
                    {
                        int n;
                        var buf = new byte[2048];
                        while ((n = input.Read(buf,0,buf.Length)) > 0)
                        {
                            output.Write(buf,0,n);
                            writtenSoFar+=n;
                            cycles++;
                            if (cycles % 32768 == 0)
                            {
                                _output.WriteLine("  block {0} {1:F2}%", cycles, ((100.0) * writtenSoFar)/totalSize);
                            }
                        }
                    }
                }
            }
        }


        private string CreateHugeZip(string containingDir)
        {
            _output.WriteLine("Zip64 Create Huge Zip file");
            string fodderDir = Path.Combine(containingDir, "dir");
            string zipFileToCreate = Path.Combine(containingDir, HUGE_ZIPFILE_NAME);
            using (ZipFile zip = new ZipFile())
            {
                zip.SaveProgress += SaveProgress("createhugefile");
                zip.AddProgress += AddProgress("createhugefile");
                zip.UpdateDirectory(fodderDir, "");
                // foreach (var e in zip)
                // {
                //     if (e.FileName.EndsWith(".pst") ||
                //         e.FileName.EndsWith(".ost") ||
                //         e.FileName.EndsWith(".zip"))
                //         e.CompressionMethod = CompressionMethod.None;
                // }

                zip.UseZip64WhenSaving = Zip64Option.Always;
                // use large buffer to speed up save for large files:
                zip.BufferSize = 1024 * 756;
                zip.CodecBufferSize = 1024 * 128;
                zip.Save(zipFileToCreate);
            }

            FileInfo fi = new FileInfo(zipFileToCreate);
            if (fi.Length < (long)System.UInt32.MaxValue)
            {
                _output.WriteLine("deleting file {0} ({1} bytes, too small, {2:F2}%)",
                    zipFileToCreate, fi.Length, (double)fi.Length/(0.01*System.UInt32.MaxValue) );
                File.Delete(zipFileToCreate);
                throw new Exception("cannot start - need more files.");
            }
            else
            {
                _output.WriteLine("huge zip file {0} ({1} bytes, size rating {2:F2}%)",
                    zipFileToCreate.Replace(TEMP,""), fi.Length, (double)fi.Length/(0.01*System.UInt32.MaxValue) );
            }

            _HugeZipFile = zipFileToCreate;
            return containingDir;
        }


        [Fact]
        public void Create_basic()
        {
            Zip64Option[] Options = {
                Zip64Option.Always,
                Zip64Option.Never,
                Zip64Option.AsNecessary
            };

            // These will not create >4.2gb files. Just files that do, or do not,
            // use the z64 extensions. Each cycle takes about 8-10 minutes.
            for (int k = 0; k < Options.Length; k++)
            {
                _output.WriteLine("\n\n==================Trial {0}...", k);
                string zipFileToCreate = Path.Combine(TopLevelDir, $"Zip64_Create-{k}.zip");

                _output.WriteLine("Creating file {0}", zipFileToCreate);
                _output.WriteLine("  ZIP64 option: {0}", Options[k].ToString());
                var checksums = new Dictionary<string, string>();
                using (ZipFile zip1 = new ZipFile())
                {
                    string fodderDir = Path.Combine(Z64_DIR, "dir");
                    if (!Directory.Exists(fodderDir)) {
                        throw new ArgumentException("unexpected: fodder dir does not exist");
                    }

                    var fodderFiles = Directory.GetFiles(fodderDir, "*.*");

                    Assert.True(fodderFiles.Length > 20, $"fodderFiles.Length {fodderFiles.Length}");
                    // select 12 files at random
                    fodderFiles = fodderFiles
                                    .OrderBy(_ => Guid.NewGuid())
                                    .Take(_rnd.Next(3) + 8)
                                    .OrderBy(x => Path.GetFileName(x))
                                    .ToArray();

                    for (int i = 0; i < fodderFiles.Length; i++)
                    {
                        string filename = fodderFiles[i];
                        _output.WriteLine("  add: {0}", filename.Replace(Z64_DIR,""));
                        zip1.AddFile(filename, "");
                        var chk = TestUtilities.ComputeChecksum(filename);
                        checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
                    }

                    zip1.BufferSize = 1024 * 756;
                    zip1.CodecBufferSize = 1024 * 128;
                    zip1.SaveProgress += SaveProgress("Create_basic");
                    zip1.UseZip64WhenSaving = Options[k];
                    zip1.Comment = String.Format("This archive uses zip64 option: {0}", Options[k].ToString());
                    zip1.Save(zipFileToCreate);

                    Assert.NotNull(zip1.OutputUsedZip64, "OutputUsedZip64 exists");

                    if (Options[k] == Zip64Option.Always)
                    {
                        Assert.True(zip1.OutputUsedZip64.Value);
                    }
                    else if (Options[k] == Zip64Option.Never)
                    {
                        Assert.False(zip1.OutputUsedZip64.Value);
                    }
                }

                Zip64VerifyZip(zipFileToCreate);

                _output.WriteLine("---------------Reading {0}...", zipFileToCreate);
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    string extractDir = Path.Combine(TopLevelDir, String.Format("extract{0}", k));
                    foreach (var e in zip2)
                    {
                        _output.WriteLine(" Entry: {0}  c({1})  unc({2}) c%({3:F2}%)",
                            e.FileName, e.CompressedSize, e.UncompressedSize,
                            100.0 - ((double)e.CompressedSize/(0.01 * e.UncompressedSize)));

                        e.Extract(extractDir);
                        string filename = Path.Combine(extractDir, e.FileName);
                        string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                        Assert.True(checksums.ContainsKey(e.FileName), "Checksum is missing");
                        Assert.Equal(checksums[e.FileName], actualCheckString, $"Checksums for ({e.FileName}) do not match." );
                        _output.WriteLine("     Checksums match ({0}).\n", actualCheckString);
                    }
                    Directory.Delete(extractDir, true);
                }
                // do not keep these large files around
                File.Delete(zipFileToCreate);
            }
        }


        private string NewFileName(string extension)
        {
            string filename = null;
            int tries = 0;
            do {
                filename = Path.Combine(TopLevelDir, String.Format("Data{0}.{1}", tries, extension));
                tries++;
            }
            while(File.Exists(filename) && tries<128);
            if (File.Exists(filename))
            {
                throw new ArgumentException("cannot find a file that does not exist");
            }
            return filename;
        }


        [Fact]
        public void Convert_Z64Option()
        {
            string trialDescription = "\n\n==============\nTrial {0}/{1}:  create archive as 'zip64={2}', then open it and re-save with 'zip64={3}'";
            Zip64Option[] z64a = {
                Zip64Option.Never,
                Zip64Option.Always,
                Zip64Option.AsNecessary};

            // Create some files
            var filesToAdd = new List<String>();
            int entries = _rnd.Next(14) + 6;
            //int entries = 2;
            _output.WriteLine("Convert_Z64Option Creating files to add,, {0} entries", entries);

            var checksums = new Dictionary<string, string>();
            string filename = null;

            for (int i = 0; i < entries; i++)
            {
                if (_rnd.Next(2) == 1)
                {

                    filename = NewFileName("bin");
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(44000) + 5000);
                }
                else
                {
                    filename = NewFileName("txt");
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(44000) + 5000);
                }
                filesToAdd.Add(filename);

                var chk = TestUtilities.ComputeChecksum(filename);
                checksums.Add(Path.GetFileName(filename), TestUtilities.CheckSumToString(chk));
            }

            // two cycles here - one update includes rename/remove, one does not
            int cycle = 0;
            for (int u = 0; u < 2; u++)
            {
                for (int m = 0; m < z64a.Length; m++)
                {
                    for (int n = 0; n < z64a.Length; n++)
                    {
                        cycle++;
                        _output.WriteLine(trialDescription, cycle, (z64a.Length * z64a.Length) * 2, z64a[m], z64a[n]);

                        string zipFileToCreate = Path.Combine(TopLevelDir, $"Zip64_Convert-{cycle}.A.zip");
                        using (ZipFile zip1 = new ZipFile())
                        {
                            foreach(var _filename in filesToAdd)
                            {
                                zip1.AddFile(_filename, "");
                            }
                            _output.WriteLine("---------------Saving to {0} with Zip64={1}...",
                                Path.GetFileName(zipFileToCreate), z64a[m].ToString());

                            zip1.UseZip64WhenSaving = z64a[m];
                            zip1.Comment = String.Format("This archive uses Zip64Option={0}", z64a[m].ToString());
                            zip1.Save(zipFileToCreate);
                        }

                        Assert.Equal(entries, CountEntries(zipFileToCreate),
                            "The Zip file has the wrong number of entries.");

                        string newFile = zipFileToCreate.Replace(".A.", ".B.");
                        using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                        {
                            _output.WriteLine("---------------Extracting {0} ...", Path.GetFileName(zipFileToCreate));
                            string extractDir = Path.Combine(TopLevelDir, $"extract-{cycle}-{u}.A");
                            foreach (var e in zip2)
                            {
                                _output.WriteLine(" {0}  crc({1:X8})  c({2:X8}) unc({3:X8}) c%({4:F2})",
                                    e.FileName, e.Crc,
                                    e.CompressedSize, e.UncompressedSize,
                                    (100.0 - (double)e.CompressedSize/ (0.01* e.UncompressedSize)) );
                                e.Extract(extractDir);
                                filename = Path.Combine(extractDir, e.FileName);
                                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                                Assert.True(checksums.ContainsKey(e.FileName), "Checksum is missing");
                                Assert.Equal(checksums[e.FileName], actualCheckString, $"Checksums for ({e.FileName}) do not match." );
                            }
                            Directory.Delete(extractDir, true);

                            if (u==1)
                            {
                                _output.WriteLine("---------------Updating:  Renaming an entry...");
                                zip2[4].FileName += ".renamed";

                                string entriesToRemove = (_rnd.Next(2) == 0) ? "*.txt" : "*.bin";
                                _output.WriteLine("---------------Updating:  Removing {0} entries...", entriesToRemove);
                                zip2.RemoveSelectedEntries(entriesToRemove);
                            }

                            _output.WriteLine("---------------Saving to {0} with Zip64={1}...",
                                Path.GetFileName(newFile), z64a[n].ToString());

                            zip2.UseZip64WhenSaving = z64a[n];
                            zip2.Comment = String.Format("This archive uses Zip64Option={0}", z64a[n].ToString());
                            zip2.Save(newFile);
                        }

                        using (ZipFile zip3 = ZipFile.Read(newFile))
                        {
                            _output.WriteLine("---------------Extracting {0} ...",
                                Path.GetFileName(newFile));
                            string extractDir = Path.Combine(TopLevelDir, $"extract-{cycle}-{u}.B");
                            foreach (var e in zip3)
                            {
                                _output.WriteLine(" {0}  crc({1:X8})  c({2:X8}) unc({3:X8}) c%({4:F2})",
                                    e.FileName, e.Crc, e.CompressedSize, e.UncompressedSize,
                                    (100.0 - (double)e.CompressedSize/ (0.01* e.UncompressedSize)) );

                                e.Extract(extractDir);
                                filename = Path.Combine(extractDir, e.FileName);
                                string actualCheckString = TestUtilities.CheckSumToString(TestUtilities.ComputeChecksum(filename));
                                if (!e.FileName.EndsWith(".renamed"))
                                {
                                    Assert.True(checksums.ContainsKey(e.FileName), "Checksum is missing");
                                    Assert.Equal(checksums[e.FileName], actualCheckString, $"Checksums for ({e.FileName}) do not match." );
                                }
                            }
                            Directory.Delete(extractDir, true);

                        }
                    }
                }
            }
        }


        private EventHandler<ExtractProgressEventArgs> ExtractProgress(string label, int numFilesToExtract)
        {
            int _numExtracted = 0;
            int _epCycles = 0;;

            return (object sender, ExtractProgressEventArgs e) => {

                switch (e.EventType)
                {
                    case ZipProgressEventType.Extracting_BeforeExtractEntry:
                        _epCycles = 0;
                        break;

                    case ZipProgressEventType.Extracting_EntryBytesWritten:
                        _epCycles++;
                        if ((_epCycles % 512) == 0)
                        {
                            string ename = e.CurrentEntry.FileName;
                            if (e.CurrentEntry.FileName.StartsWith("Users"))
                            {
                                ename = ename.Replace(TEMP.Substring(3), "");
                            }

                            _output.WriteLine("{0} entry {1}/{2} :: {3} :: {4}/{5}mb ::  {6:N0}%",
                                label,
                                _numExtracted, numFilesToExtract,
                                ename,
                                e.BytesTransferred/(1024*1024),
                                e.TotalBytesToTransfer/(1024*1024),
                                ((double)e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer)
                            );
                        }
                        break;

                    case ZipProgressEventType.Extracting_AfterExtractEntry:
                        _numExtracted++;
                        if (numFilesToExtract < 1024 || (_numExtracted % 128) == 0)
                        {
                            while (_numExtracted > numFilesToExtract) _numExtracted--;
                            if (_numExtracted == numFilesToExtract)
                            {
                                _output.WriteLine("All done " + label);
                            }
                            else
                            {
                                _output.WriteLine("{0} entry {1}/{2} {3:N0}%",
                                    label,
                                    _numExtracted, numFilesToExtract,
                                    _numExtracted / (0.01 *numFilesToExtract));
                            }
                        }
                        break;
                }
            };
        }


        private EventHandler<SaveProgressEventArgs> SaveProgress(string label)
        {
            int _numSaving = 0;
            int _totalToSave = 0;
            int _spCycles = 0;
            return (object sender, SaveProgressEventArgs e) => {
                switch (e.EventType)
                {
                    case ZipProgressEventType.Saving_Started:
                        _output.WriteLine("{0} saving started...", label);
                        _numSaving = 1;
                        _spCycles = 0;
                        _totalToSave = e.EntriesTotal;
                        break;

                    case ZipProgressEventType.Saving_BeforeWriteEntry:
                        _output.WriteLine("Compressing {0}", e.CurrentEntry.FileName);
                        _totalToSave = e.EntriesTotal;
                        break;

                    case ZipProgressEventType.Saving_EntryBytesRead:
                        _spCycles++;
                        if ((_spCycles % 128) == 0)
                        {
                            _output.WriteLine("Saving entry {0}/{1} :: {2} :: {3}/{4}mb {5:F1}%",
                                _numSaving, _totalToSave,
                                e.CurrentEntry.FileName,
                                e.BytesTransferred/(1024*1024), e.TotalBytesToTransfer/(1024*1024),
                                ((double)e.BytesTransferred) / (0.01 * e.TotalBytesToTransfer));
                        }
                        break;

                    case ZipProgressEventType.Saving_AfterWriteEntry:
                        _numSaving++;
                        break;

                    case ZipProgressEventType.Saving_Completed:
                        _output.WriteLine("Save completed");
                        break;
                }
            };
        }


        private EventHandler<AddProgressEventArgs> AddProgress(string label)
        {
            return (object sender, AddProgressEventArgs e) => {
                switch (e.EventType)
                {
                    case ZipProgressEventType.Adding_Started:
                        _output.WriteLine("Adding files to the zip...");
                        break;
                    case ZipProgressEventType.Adding_AfterAddEntry:
                        _output.WriteLine("Adding file {0}", e.CurrentEntry.FileName);
                        break;
                    case ZipProgressEventType.Adding_Completed:
                        _output.WriteLine("Added all files");
                        break;
                }
            };
        }



        private void Zip64VerifyZip(string zipfile)
        {
            Zip64VerifyZip(zipfile, null, null);
        }

        // this usually takes 10-12 minutes
        private void Zip64VerifyZip(string zipfile, string password, string label)
        {
            if (label == null ) {
                label = "Zip64VerifyZip";
            }
            Stream bitBucket = Stream.Null;
            _output.WriteLine("");
            _output.WriteLine("Checking file {0}", zipfile.Replace(TEMP,""));
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                if (password!=null){
                    zip.Password = password;
                }
                // large buffer better for large files
                zip.BufferSize = 65536*4; // 65536 * 8 = 512k
                //zip.ExtractProgress += ExtractProgress(label, zip.Entries.Count);
                int n = 0;
                int mod = zip.Entries.Count / 32;
                foreach (var s in zip.EntryFileNames)
                {
                    // less output when there aere many entries
                    if ((zip.Entries.Count < 512) || (n % mod == 0))
                    {
                        _output.WriteLine("  Entry: {0}", s);
                    }
                    zip[s].Extract(bitBucket);
                    n++;
                }
            }
        }


        [Fact] // Timeout(3 * 60*60*1000),  60*60*1000 = 1hr
        public void Zip64_Update_WZ()
        {
            if (!WinZipIsPresent) {
                _output.WriteLine("skipping test [Zip64_Update_WZ()] : winzip is not present");
                return;
            }
            // this should take a little less than an hour
            Zip64UpdateAddFiles("WinZip");
        }


        [Fact] // Timeout(3 * 60*60*1000), 60*60*1000 = 1hr
        public void Zip64_Update_DNZ()
        {
            // this should take a little less than an hour
            Zip64UpdateAddFiles("DNZ");
        }


        private void Zip64UpdateAddFiles(string marker)
        {
            // Sanity: make sure the starting zip is larger than the 4.2gb size
            FileInfo fi = new FileInfo(_HugeZipFile);
            Assert.True(fi.Length > (long)System.UInt32.MaxValue,
                $"The zip file ({_HugeZipFile}) is not large enough." );

            int numUpdates = 2;
            int baseSize = _rnd.Next(0x1000ff) + 80000;
            string zipFileToUpdate = Path.Combine(TopLevelDir, $"Z64Update.{marker}.zip");
            File.Copy(_HugeZipFile, zipFileToUpdate);
            Assert.True(File.Exists(zipFileToUpdate),
                $"The required ZIP file does not exist ({zipFileToUpdate.Replace(TEMP,"")})" );

            _output.WriteLine("Verifying the zip - " + DateTime.Now.ToString("G"));
            Zip64VerifyZip(zipFileToUpdate);

            //var sw = new StringWriter();
            for (int j=0; j < numUpdates; j++)
            {
                // make sure the zip is larger than the 4.2gb size
                fi = new FileInfo(zipFileToUpdate);
                Assert.True(fi.Length > (long)System.UInt32.MaxValue,
                $"The zip file ({zipFileToUpdate}) is not large enough." );

                _output.WriteLine("Zip64 Update - {0} - ({1}/{2})",
                    marker, j+1, numUpdates);

                // create another folder with a single file in it
                string subdir = Path.Combine(TopLevelDir, $"newfolder-{j}");
                Directory.CreateDirectory(subdir);
                string fileName = Path.Combine(subdir,
                    System.Guid.NewGuid().ToString() + ".txt");
                long size = baseSize + _rnd.Next(28000);
                TestUtilities.CreateAndFillFileText(fileName, size);

                _output.WriteLine("");
                _output.WriteLine("Updating the zip file, cycle {0}...{1}", j, DateTime.Now.ToString("G"));
                // Update the zip with that new folder+file.
                // This will take a long time for large files.
                using (ZipFile zip = ZipFile.Read(zipFileToUpdate))
                {
                    zip.BufferSize = 1024 * 512;
                    zip.CodecBufferSize = 1024 * 128;
                    zip.SaveProgress += SaveProgress("UpdateAdd");
                    //zip.StatusMessageTextWriter = sw;
                    zip.UpdateDirectory(subdir, subdir);
                    zip.UseZip64WhenSaving = Zip64Option.Always;
                    zip.BufferSize = 65536*8; // 65536 * 8 = 512k
                    zip.Save(zipFileToUpdate);
                }

                _output.WriteLine("Save complete "  + DateTime.Now.ToString("G"));
                //zipFileToUpdate = workingZipFile; // for subsequent updates
                // // emit status into the log if available
                // string status = sw.ToString();
                // if (status != null && status != "")
                // {
                //     var lines = status.Split('\n');
                //     _output.WriteLine("status: ({0})", DateTime.Now.ToString("G") );
                //     foreach (string line in lines)
                //         _output.WriteLine(line);
                // }
            }

            _output.WriteLine("");
            _output.WriteLine("Verifying the zip again... " + DateTime.Now.ToString("G"));
            Zip64VerifyZip(zipFileToUpdate);
        }


        // [Fact]
        // public void Zip64_Winzip_Unzip_OneFile()
        // {
        //     if (!WinZipIsPresent) {
        //         _output.WriteLine("skipping test [Zip64_Winzip_Unzip_OneFile()] : winzip is not present");
        //         return;
        //     }
        //     string testBin = TestUtilities.GetTestBinDir(CurrentDir);
        //     string fileToZip = Path.Combine(testBin, "Ionic.Zip.dll");
        //
        //     Directory.SetCurrentDirectory(TopLevelDir);
        //
        //     for (int p=0; p < compLevels.Length; p++)
        //     {
        //         for (int n=0; n < crypto.Length; n++)
        //         {
        //             for (int m=0; m < z64.Length; m++)
        //             {
        //                 string zipFile = String.Format("WZ-Unzip.OneFile.{0}.{1}.{2}.zip",
        //                     compLevels[p].ToString(),
        //                     crypto[n].ToString(),
        //                     z64[m].ToString());
        //                 string password = Path.GetRandomFileName();
        //
        //                 _output.WriteLine("=================================");
        //                 _output.WriteLine("Creating {0}...", Path.GetFileName(zipFile));
        //                 _output.WriteLine("Encryption:{0}  Zip64:{1} pw={2}  compLevel={3}",
        //                     crypto[n].ToString(), z64[m].ToString(), password, compLevels[p].ToString());
        //
        //                 using (var zip = new ZipFile())
        //                 {
        //                     zip.Comment = String.Format("Encryption={0}  Zip64={1}  pw={2}",
        //                         crypto[n].ToString(), z64[m].ToString(), password);
        //                     zip.Encryption = crypto[n];
        //                     zip.Password = password;
        //                     zip.CompressionLevel = compLevels[p];
        //                     zip.UseZip64WhenSaving = z64[m];
        //                     zip.AddFile(fileToZip, "file");
        //                     zip.Save(zipFile);
        //                 }
        //
        //                 if (WinZipIsPresent) {
        //                     _output.WriteLine("Unzipping with WinZip...");
        //
        //                     string extractDir = String.Format("extract.{0}.{1}.{2}",p,n,m);
        //                     Directory.CreateDirectory(extractDir);
        //
        //                     // this will throw if the command has a non-zero exit code.
        //                     this.Exec(wzunzip,
        //                         String.Format("-s{0} -d {1} {2}\\", password, zipFile, extractDir));
        //                 }
        //             }
        //         }
        //     }
        // }


        // [Fact] // Timeout((int)(1 * 60*60*1000)), in milliseconds.
        // public void Zip64_Winzip_Unzip_Huge()
        // {
        //     if (!WinZipIsPresent) {
        //         _output.WriteLine("skipping test [Zip64_Winzip_Unzip_Huge] : winzip is not present");
        //         return;
        //     }
        //
        //     string[] zipFilesToExtract = GetHugeZipFiles(); // may take a long time
        //     int baseSize = _rnd.Next(0x1000ff) + 80000;
        //
        //     string extractDir = Path.Combine(TopLevelDir, "extract");
        //     Directory.SetCurrentDirectory(TopLevelDir);
        //     Directory.CreateDirectory(extractDir);
        //
        //     for (int k=0; k < zipFilesToExtract.Length; k++)
        //     {
        //         string zipFileToExtract = zipFilesToExtract[k];
        //         Assert.True(File.Exists(zipFileToExtract),
        //             $"required ZIP file does not exist ({zipFileToExtract})");
        //
        //         // make sure the zip is larger than the 4.2gb size
        //         FileInfo fi = new FileInfo(zipFileToExtract);
        //         Assert.True(fi.Length > (long)System.UInt32.MaxValue,
        //                       "The zip file ({zipFileToExtract}) is not large enough." );
        //
        //         _output.WriteLine("Counting entries in the zip file...");
        //
        //         int numEntries = CountEntries(zipFileToExtract);
        //
        //         _output.WriteLine("Using WinZip to list the entries...");
        //
        //         // Examine and unpack the zip archive via WinZip
        //         // first, examine the zip entry metadata:
        //         string wzzipOut = this.Exec(wzzip, "-vt " + zipFileToExtract);
        //         _output.WriteLine(wzzipOut);
        //
        //         int x = 0;
        //         int y = 0;
        //         int wzzipEntryCount=0;
        //         string textToLookFor= "Filename: ";
        //         _output.WriteLine("================");
        //         _output.WriteLine("Files listed by WinZip:");
        //         while (true)
        //         {
        //             x = wzzipOut.IndexOf(textToLookFor, y);
        //             if (x < 0) break;
        //             y = wzzipOut.IndexOf("\n", x);
        //             string name = wzzipOut.Substring(x + textToLookFor.Length, y-x-1).Trim();
        //             _output.WriteLine("  {0}", name);
        //             if (!name.EndsWith("\\"))
        //             {
        //                 wzzipEntryCount++;
        //                 if (wzzipEntryCount > numEntries * 3) throw new Exception("too many entries!");
        //             }
        //         }
        //
        //         _output.WriteLine("================");
        //         Assert.Equal(numEntries, wzzipEntryCount,
        //                         "Unexpected number of entries found by WinZip.");
        //
        //         x=0; y = 0;
        //         _output.WriteLine("status Extracting the entries...");
        //         int nCycles = 0;
        //         while (true)
        //         {
        //             _output.WriteLine("test Zip64 WinZip unzip - " +
        //                        Path.GetFileName(zipFileToExtract));
        //             x = wzzipOut.IndexOf(textToLookFor, y);
        //             if (x < 0) break;
        //             if (nCycles > numEntries * 4) throw new Exception("too many entries?");
        //             y = wzzipOut.IndexOf("\n", x);
        //             string name = wzzipOut.Substring(x + textToLookFor.Length, y-x-1).Trim();
        //             if (!name.EndsWith("\\"))
        //             {
        //                 nCycles++;
        //                 _output.WriteLine("status Extracting {1}/{2} :: {0}", name, nCycles, wzzipEntryCount);
        //                 this.Exec(wzunzip,
        //                           String.Format("-d {0} {1}\\ \"{2}\"",
        //                                         Path.GetFileName(zipFileToExtract),
        //                                         extractDir, name));
        //                 string path = Path.Combine(extractDir, name);
        //                 Assert.True(File.Exists(path), $"extracted file ({path}) does not exist");
        //                 File.Delete(path);
        //                 System.Threading.Thread.Sleep(120);
        //             }
        //         }
        //     }
        // }


        // private void CreateLargeFiles(int numFilesToAdd, int baseSize, string dir)
        // {
        //     bool firstFileDone = false;
        //     string fileName = "";
        //     long fileSize = 0;
        //
        //     Action<Int64> progressUpdate = (x) =>
        //         {
        //             _output.WriteLine("Creating {0}, [{1}/{2}] ({3:N0}%)",
        //                                      fileName, x, fileSize, ((double)x)/ (0.01 * fileSize) );
        //         };
        //
        //     // It takes some time to create a large file. And we need
        //     // a bunch of them.
        //     for (int i = 0; i < numFilesToAdd; i++)
        //     {
        //         // randomly select binary or text
        //         int n = _rnd.Next(2);
        //         fileName = string.Format("Pippo{0}.{1}", i, (n==0) ? "bin" : "txt" );
        //         if (i != 0)
        //         {
        //             int x = _rnd.Next(6);
        //             if (x != 0)
        //             {
        //                 string folderName = string.Format("folder{0}", x);
        //                 fileName = Path.Combine(folderName, fileName);
        //                 if (!Directory.Exists(Path.Combine(dir, folderName)))
        //                     Directory.CreateDirectory(Path.Combine(dir, folderName));
        //             }
        //         }
        //         fileName = Path.Combine(dir, fileName);
        //         // first file is 2x larger
        //         fileSize = (firstFileDone) ? (baseSize + _rnd.Next(0x880000)) : (2*baseSize);
        //         if (n==0)
        //             TestUtilities.CreateAndFillFileBinary(fileName, fileSize, progressUpdate);
        //         else
        //             TestUtilities.CreateAndFillFileText(fileName, fileSize, progressUpdate);
        //         firstFileDone = true;
        //     }
        // }


        // [Fact] // 60*60*1000 == 1 hr
        // public void Zip64_Winzip_Zip_Huge()
        // {
        //     if (!WinZipIsPresent) {
        //         _output.WriteLine("skipping test [Zip64_Winzip_Zip_Huge()] : winzip is not present");
        //         return;
        //     }
        //
        //     // This Fact tests if DNZ can read a huge (>4.2gb) zip64 file
        //     // created by winzip.
        //     int baseSize = _rnd.Next(80000) + 0x1000ff;
        //     string contentDir = "fodder";
        //     //Directory.SetCurrentDirectory(TopLevelDir);
        //     Directory.CreateDirectory(contentDir);
        //
        //     if (CreateLinksToLargeFiles(contentDir))
        //         return;
        //
        //     _output.WriteLine("Creating large files..." + DateTime.Now.ToString("G"));
        //
        //     CreateLargeFiles(_rnd.Next(4) + 4, baseSize, contentDir);
        //     _output.WriteLine("Creating a new Zip with winzip - " +
        //                           DateTime.Now.ToString("G"));
        //
        //     var fileList = Directory.GetFiles(contentDir, "*.*", SearchOption.AllDirectories);
        //
        //     // create a zip archive via WinZip
        //     string wzzipOut= null;
        //     string zipFileToCreate = "Zip64-WinZip-Zip-Huge.zip";
        //     int nCycles= 0;
        //
        //     // Add one file at a time, invoking wzzip.exe for each. After it
        //     // completes, delete the just-added file. This allows coarse-grained
        //     // status updates in the progress window.  Not sure about the exact
        //     // impact on disk space, or execution time; my observation is that the
        //     // time-cost to add one entry increases, as the zip file gets
        //     // larger. Each successive cycle takes a little longer.  It's tolerable
        //     // I guess.  A tradeoff to get visual progress feedback.
        //     foreach (var filename in fileList)
        //     {
        //         nCycles++;
        //         wzzipOut = this.Exec(wzzip, String.Format("-a -p -r -yx \"{0}\" \"{1}\"",
        //                                                   zipFileToCreate,
        //                                                   filename));
        //         _output.WriteLine(wzzipOut);
        //         System.Threading.Thread.Sleep(420);
        //         File.Delete(filename);
        //     }
        //
        //     // Create one additional small text file and add it to the zip.  For
        //     // this test, it must be added last, at the end of the ZIP file.
        //     _output.WriteLine("Inserting one additional file with wzzip.exe - " +
        //                           DateTime.Now.ToString("G"));
        //     nCycles++;
        //     var newfile = Path.Combine(contentDir, "zzz-" + Path.GetRandomFileName() + ".txt");
        //     int filesize = _rnd.Next(50000) + 440000;
        //     TestUtilities.CreateAndFillFileText(newfile, filesize);
        //     wzzipOut = this.Exec(wzzip, String.Format("-a -p -r -yx \"{0}\" \"{1}\"", zipFileToCreate, newfile));
        //     _output.WriteLine(wzzipOut);
        //     System.Threading.Thread.Sleep(120);
        //     File.Delete(newfile);
        //
        //     System.Threading.Thread.Sleep(120);
        //
        //     // make sure the zip is larger than the 4.2gb size
        //     FileInfo fi = new FileInfo(zipFileToCreate);
        //     Assert.True(fi.Length > (long)System.UInt32.MaxValue,
        //                   "The zip file ({0}) is not large enough.",
        //                   zipFileToCreate);
        //
        //     // Now use DotNetZip to extract the large zip file to the bit bucket.
        //     _output.WriteLine("Verifying the new Zip with DotNetZip - " +
        //                           DateTime.Now.ToString("G"));
        //
        //     verb = "verifying";
        //     Zip64VerifyZip(zipFileToCreate);
        // }
        //
        //
        //
        // [Fact]  /* , Timeout((int)(2 * 60*60*1000)) */ // 60*60*1000 = 1 hr
        // public void Zip64_Winzip_Setup()
        // {
        //     // Not really a test.  This thing just sets up the big zip file.
        //     _output.WriteLine("This test merely checks for existence of two large zip");
        //     _output.WriteLine("files, in a well-known place, and creates them as");
        //     _output.WriteLine("necessary. The zips are then used by other tests.");
        //     _output.WriteLine(" ");
        //     GetHugeZipFiles(); // usually takes about an hour
        // }

        private string CreateVeryVeryLargeFodderFile()
        {
            // a little larger that Int32 MaxValue
            Int64 desiredSize= System.UInt32.MaxValue + (Int64)(System.Int32.MaxValue)/4 + _rnd.Next(0x1000000);
            string nameOfFodderFile = Path.Combine(TopLevelDir, "VeryVeryLargeFile.txt");
            _output.WriteLine("");
            _output.WriteLine("Creating a large file..." + DateTime.Now.ToString("G"));

            // create a very large file
            int nEvents = 0;
            Action<Int64> createProgress = (x) =>
            {
                if (nEvents % 8 == 0)
                {
                    _output.WriteLine("Creating {0}, [{1}/{2}mb] ({3:N0}%)",
                        nameOfFodderFile.Replace(TEMP, ""),
                        x / (1024 * 1024),
                        desiredSize / (1024 * 1024),
                        ((double)x) / (0.01 * desiredSize));
                }
                nEvents++;
            };

            // This takes a few minutes...
            TestUtilities.CreateAndFillFileText(nameOfFodderFile,
                                                desiredSize,
                                                createProgress);

            // make sure it is larger than 4.2gb
            FileInfo fi = new FileInfo(nameOfFodderFile);
            Assert.True(fi.Length > (long)System.UInt32.MaxValue,
                $"The fodder file ({nameOfFodderFile}) is not large enough." );

            return nameOfFodderFile;
        }

        private void EmitStatus(String s)
        {
            _output.WriteLine("status:");
            foreach (string line in s.Split('\n'))
                _output.WriteLine(line);
        }


        [Fact]  /* , Timeout(1 * 60*60*1000) */
        public void Zip64_EntryLargerThan4gb()
        {
            _output.WriteLine("Zip64 Create/Zip/Extract a file > 4.2gb");
            string zipFileToCreate = Path.Combine(TopLevelDir, "Zip64_EntryLargerThan4gb.zip");
            string extractDir = Path.Combine(TopLevelDir, "extracted");
            string nameOfFodderFile = CreateVeryVeryLargeFodderFile();

            _output.WriteLine("");
            _output.WriteLine("computing checksum..." + DateTime.Now.ToString("G"));
            var chk1 = TestUtilities.ComputeChecksum(nameOfFodderFile);
            var sw = new StringWriter();
            using (var zip = new ZipFile())
            {
                zip.StatusMessageTextWriter = sw;
                zip.UseZip64WhenSaving = Zip64Option.Always;
                zip.BufferSize = 65536*8; // 65536 * 8 = 512k
                zip.SaveProgress += SaveProgress("Zip64_Over_4gb()");
                var e = zip.AddFile(nameOfFodderFile, "");
                _output.WriteLine("zipping one file......" + DateTime.Now.ToString("G"));
                zip.Save(zipFileToCreate);
            }

            EmitStatus(sw.ToString());
            File.Delete(nameOfFodderFile);
            _output.WriteLine("");
            _output.WriteLine("Extracting the zip..." + DateTime.Now.ToString("G"));

            var options = new ReadOptions { StatusMessageWriter= new StringWriter() };

            string nameOfExtractedFile = null;
            using (var zip = ZipFile.Read(zipFileToCreate, options))
            {
                Assert.Equal<int>(1, zip.Entries.Count, "Incorrect number of entries in the zip file");
                zip.BufferSize = 65536*4; // 65536 * 8 = 512k
                zip.ExtractProgress += ExtractProgress("Zip64_Over_4gb()", zip.Entries.Count);
                ZipEntry e = zip[0];
                e.FileName = Path.GetFileName(e.FileName);
                nameOfExtractedFile = Path.Combine(extractDir, e.FileName);
                _output.WriteLine("extracting to: {0}", nameOfExtractedFile);
                e.Extract(extractDir);
            }
            EmitStatus(options.StatusMessageWriter.ToString());
            _output.WriteLine("");
            _output.WriteLine("computing checksum..." + DateTime.Now.ToString("G"));

            var chk2 = TestUtilities.ComputeChecksum(nameOfExtractedFile);
            Assert.Equal<String>(TestUtilities.CheckSumToString(chk1),
                                    TestUtilities.CheckSumToString(chk2),
                                    "Checksum mismatch");
        }


        [Fact]  /* , Timeout(1 * 60*60*1000) */
        public void Z64_ManyEntries_NoEncryption_DefaultCompression_AsNecessary()
        {
            _Zip64_Over65534Entries(Zip64Option.AsNecessary, EncryptionAlgorithm.None, Ionic.Zlib.CompressionLevel.Default);
        }

        [Fact]  /* , Timeout(1 * 60*60*1000) */
        public void Z64_ManyEntries_PkZipEncryption_DefaultCompression_AsNecessary()
        {
            _Zip64_Over65534Entries(Zip64Option.AsNecessary, EncryptionAlgorithm.PkzipWeak, Ionic.Zlib.CompressionLevel.Default);
        }

        [Fact]  /* , Timeout(2 * 60*60*1000) */
        public void Z64_ManyEntries_WinZipEncryption_DefaultCompression_AsNecessary()
        {
            _Zip64_Over65534Entries(Zip64Option.AsNecessary, EncryptionAlgorithm.WinZipAes256, Ionic.Zlib.CompressionLevel.Default);
        }


        [Fact]  /* , Timeout(1 * 60*60*1000) */
        public void Z64_ManyEntries_NoEncryption_DefaultCompression_Always()
        {
            _Zip64_Over65534Entries(Zip64Option.Always, EncryptionAlgorithm.None, Ionic.Zlib.CompressionLevel.Default);
        }

        [Fact]  /* , Timeout(1 * 60*60*1000) */
        public void Z64_ManyEntries_PkZipEncryption_DefaultCompression_Always()
        {
            _Zip64_Over65534Entries(Zip64Option.Always, EncryptionAlgorithm.PkzipWeak, Ionic.Zlib.CompressionLevel.Default);
        }

        [Fact]  /* , Timeout(2 * 60*60*1000) */
        public void Z64_ManyEntries_WinZipEncryption_DefaultCompression_Always()
        {
            _Zip64_Over65534Entries(Zip64Option.Always, EncryptionAlgorithm.WinZipAes256, Ionic.Zlib.CompressionLevel.Default);
        }




        [Fact]  /* , Timeout(30 * 60*1000) */
        public void Z64_ManyEntries_NOZIP64()
        {
            Assert.Throws<ZipException>(() => {
            _Zip64_Over65534Entries(Zip64Option.Never, EncryptionAlgorithm.None, Ionic.Zlib.CompressionLevel.Default);
            });
        }


        void _Zip64_Over65534Entries(Zip64Option z64option,
                                            EncryptionAlgorithm encryption,
                                            Ionic.Zlib.CompressionLevel compression)
        {
            // Emitting a zip file with > 65534 entries requires the use of ZIP64 in
            // the central directory.
            int numTotalEntries = _rnd.Next(4616)+65534;
            //int numTotalEntries = _rnd.Next(461)+6534;
            //int numTotalEntries = _rnd.Next(46)+653;
            string enc = encryption.ToString();
            if (enc.StartsWith("WinZip")) enc = enc.Substring(6);
            else if (enc.StartsWith("Pkzip")) enc = enc.Substring(0,5);

            string baseName = String.Format("Zip64.ZF_Over65534.{0}.{1}.{2}.zip",
                z64option.ToString(),
                enc,
                compression.ToString());
            string zipFileToCreate = Path.Combine(TopLevelDir, baseName);

            _output.WriteLine("ZipFile #{0} 64({1}) E({2}), C({3})",
                                       numTotalEntries,
                                       z64option.ToString(),
                                       enc,
                                       compression.ToString());

            string password = Path.GetRandomFileName();

            _output.WriteLine(" Encrypt:{0} Compress:{1}...", enc, compression.ToString());

            int numSaved = 0;
            var saveProgress = new EventHandler<SaveProgressEventArgs>( (sender, e) =>
            {
                switch (e.EventType)
                {
                    case ZipProgressEventType.Saving_Started:
                        numSaved= 0;
                        break;

                    case ZipProgressEventType.Saving_AfterWriteEntry:
                        numSaved++;
                        if ((numSaved % 128) == 0)
                        {
                            _output.WriteLine("Saving entry {0}/{1} ({2:N0}%)",
                                numSaved, numTotalEntries,
                                numSaved / (0.01 * numTotalEntries) );
                        }
                        break;

                    case ZipProgressEventType.Saving_Completed:
                        _output.WriteLine("status Save completed");
                        break;
                }
            });

            string contentFormatString =
                "This is the content for entry #{0}.\r\n\r\n" +
                "AAAAAAA BBBBBB AAAAA BBBBB AAAAA BBBBB AAAAA\r\n"+
                "AAAAAAA BBBBBB AAAAA BBBBB AAAAA BBBBB AAAAA\r\n";

            int dirCount= 0;
            using (var zip = new ZipFile())
            {
                zip.Password = password;
                zip.Encryption = encryption;
                zip.CompressionLevel = compression;
                zip.SaveProgress += saveProgress;
                zip.UseZip64WhenSaving = z64option;
                // save space when saving the file:
                zip.EmitTimesInWindowsFormatWhenSaving = false;
                zip.EmitTimesInUnixFormatWhenSaving = false;

                // add files:
                for (int m=0; m<numTotalEntries; m++)
                {
                    if (_rnd.Next(7)==0)
                    {
                        string entryName = String.Format("{0:D5}", m);
                        zip.AddDirectoryByName(entryName);
                        dirCount++;
                    }
                    else
                    {
                        string entryName = String.Format("{0:D5}.txt", m);
                        if (_rnd.Next(12)==0)
                        {
                            string contentBuffer = String.Format(contentFormatString, m);
                            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(contentBuffer);
                            zip.AddEntry(entryName, contentBuffer);
                        }
                        else
                            zip.AddEntry(entryName, Stream.Null);
                    }

                    if (m % 1024 == 0)
                    {
                        _output.WriteLine("adding entry {0}/{1}  ({2:N0}%)",
                                            m, numTotalEntries, (m/(0.01*numTotalEntries)));
                    }
                }

                zip.Save(zipFileToCreate);
            }

            // verify the zip by unpacking.
            Zip64VerifyZip(zipFileToCreate, password, baseName);
            Assert.Equal<int>(numTotalEntries-dirCount,
                                 CountEntries(zipFileToCreate));
        }


        [Fact]    // Timeout(3 * 60*60*1000),  60*60*1000 = 1 hr
        public void Zip64_UpdateEntryComment_wi9214_WZ()
        {
            // Should take 2.5 hrs when creating the huge files, about 1 hr when the
            // files already exist.
            if (!WinZipIsPresent) {
                _output.WriteLine("skipping test [Zip64_Update_WZ()] : winzip is not present");
                return;
            }
            Z64UpdateHugeZipWithComment("WinZip");
        }

        [Fact]    // Timeout(3 * 60*60*1000),   60*60*1000 = 1 hr
        public void Zip64_UpdateEntryComment_wi9214_DNZ()
        {
            // Takes about 1 hr when the large zip file already exists.
            Z64UpdateHugeZipWithComment("DNZ");
        }

        private void Z64UpdateHugeZipWithComment(string marker)
        {
            // Test whether opening a huge zip64 file and then re-saving, allows the file to
            // remain valid. Must use a zip64 file > 4gb, AND with
            // at least one entry with uncompressed size greather than 4gb.  Want to check that
            // UseZip64WhenSaving is automatically selected as appropriate.

            _output.WriteLine("Z64UpdateHugeZipWithComment Create Zip with entry > 4.2gb");

            // Sanity: make sure the starting zip is larger than the 4.2gb size
            FileInfo fi = new FileInfo(_HugeZipFile);
            Assert.True(fi.Length > (long)System.UInt32.MaxValue,
                $"The zip file ({_HugeZipFile}) is not large enough." );
            string baseName = $"Z64UpdateWithComment.{marker}.zip";
            string zipFileToUpdate = Path.GetFullPath(Path.Combine(TopLevelDir, baseName));
            //File.Copy(_HugeZipFile, zipFileToUpdate);
            string nameOfFodderFile = CreateVeryVeryLargeFodderFile();

            // According to workitem 9214, the comment must be modified
            // on an entry that is larger than 4gb (uncompressed).

            // Steps:
            // 1. start with a known-huge zip file
            // 2. Open it and add one entry which is larger than 4.2gb uncompressed
            // 3. attach a comment to that entry
            // 4. save
            // 5. re-open (read) and verify the comment on the entry
            // 6. re-open (read) and modify the comment on the entry
            // 7. save
            // 8. re-open (read) and verify the modified comment on the entry

            Action<String> verifyComment = (expectedComment) =>
            {
                using (ZipFile zip = ZipFile.Read(zipFileToUpdate))
                {
                    // required: the option must be set automatically and intelligently
                    Assert.True(zip.UseZip64WhenSaving == Zip64Option.Always,
                        $"The UseZip64WhenSaving option is set incorrectly ({zip.UseZip64WhenSaving})" );

                    ZipEntry bigEntry = null;
                    foreach (var e in zip)
                    {
                        if (bigEntry == null) {
                            if (e.FileName == Path.GetFileName(nameOfFodderFile)) {
                                bigEntry = e;
                                break;
                            }
                        }
                    }
                    // redundant with the check above
                    Assert.True(bigEntry != null &&
                                bigEntry.UncompressedSize > (long)System.UInt32.MaxValue,
                        "Could not find a zip with an entry meeting the minimum size constraint.");
                    Assert.Equal(bigEntry.Comment, expectedComment, "comment not as expected");
                }
            };

            // Steps 1...4
            string comment1 = $"a comment on the large entry in the zip. ({DateTime.Now.ToString("u")})";
            using (ZipFile zip = ZipFile.Read(_HugeZipFile))
            {
                var e = zip.AddFile(nameOfFodderFile, "");
                e.Comment = comment1;
                zip.BufferSize = 1024 * 512;
                zip.SaveProgress += SaveProgress(baseName);
                zip.Save(zipFileToUpdate);
            }

            fi = new FileInfo(zipFileToUpdate);
            Assert.True(fi.Length > (long)System.UInt32.MaxValue,
                $"The zip file ({zipFileToUpdate}) is not large enough." );
            _output.WriteLine("Updated zip size {0} bytes, rating {1:F2}%)",
                fi.Length, (double)fi.Length/(0.01*System.UInt32.MaxValue) );

            // reclaim some space
            File.Delete(nameOfFodderFile);

            // Step 5: Read the zip and verify the comment
            verifyComment(comment1);

            // Steps 6 & 7: Read the zip and modify the comment
            string comment2 = $"updated comment on the large entry. ({DateTime.Now.ToString("u")})";
            using (ZipFile zip = ZipFile.Read(zipFileToUpdate))
            {
                // required: the option must be set automatically and intelligently
                Assert.True(zip.UseZip64WhenSaving == Zip64Option.Always,
                    $"The UseZip64WhenSaving option is set incorrectly ({zip.UseZip64WhenSaving})" );

                ZipEntry bigEntry = null;
                foreach (var e in zip)
                {
                    if (bigEntry == null) {
                        if (e.FileName == Path.GetFileName(nameOfFodderFile)) {
                            bigEntry = e;
                            break;
                        }
                    }
                }
                Assert.True(bigEntry != null &&
                            bigEntry.UncompressedSize > (long)System.UInt32.MaxValue,
                    "Could not find a zip with an entry meeting the minimum size constraint.");
                bigEntry.Comment = comment2;
                zip.SaveProgress += SaveProgress(baseName);
                zip.Save(zipFileToUpdate);
            }

            // Step 8: Read the zip and verify the updated comment
            verifyComment(comment2);

            _output.WriteLine("Verifying the updated zip... " + DateTime.Now.ToString("G"));
            Zip64VerifyZip(zipFileToUpdate); // this can take an hour or more
            _output.WriteLine("All done... {0}", DateTime.Now.ToString("G"));
        }
    }

}
