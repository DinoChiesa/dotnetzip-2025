// BasicTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011,2025 Dino Chiesa
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License.
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs):
// Time-stamp: <2025-January-22 17:21:37>
//
// ------------------------------------------------------------------
//
// This module defines basic unit tests for DotNetZip.
//
// ------------------------------------------------------------------

using RE = System.Text.RegularExpressions;
using Ionic.Zip.Tests.Utilities;
using Xunit.Abstractions;
using Assert = XunitAssertMessages.AssertM;

namespace Ionic.Zip.Tests
{
    public class BasicTests : IonicTestClass
    {
        EncryptionAlgorithm[] crypto =
        {
            EncryptionAlgorithm.None,
            EncryptionAlgorithm.PkzipWeak,
            EncryptionAlgorithm.WinZipAes128,
            EncryptionAlgorithm.WinZipAes256,
        };

        public BasicTests(ITestOutputHelper output)
        {
            // I could not get this to work with a BaseClass ctor
            // see https://stackoverflow.com/questions/73685218/get-at-itestoutputhelper-in-xunit-without-injecting-it
            _output = output;
        }

        [Fact]
        public void CheckOutput() {
            Assert.NotNull(_output);
        }


        [Fact]
        public void CreateZip_AddItem_WithDirectory()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddItem_WithDir.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("files-{0}", TestUtilities.GetMarker()));
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            using (ZipFile zip1 = new ZipFile())
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddItem(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }

            Assert.Equal<int>(filesToZip.Length, CountEntries(zipFileToCreate) );
        }

        [Fact]
        public void CreateZip_AddItem_NoDirectory()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddItem_NoDir.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("files-{0}", TestUtilities.GetMarker()));
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            using (ZipFile zip1 = new ZipFile())
            {
                foreach (var f in filesToZip)
                    zip1.AddItem(f);
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<int>(filesToZip.Length,CountEntries(zipFileToCreate) );
        }


        [Fact]
        public void FileNotAvailableFails_wi10387()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "FileNotAvailableFails.zip");
            using (var zip1 = new ZipFile(zipFileToCreate)) { zip1.Save(); }
            Assert.Throws<Ionic.Zip.ZipException>(() =>
            {
                using (new FileStream(zipFileToCreate, FileMode.Open,
                                      FileAccess.ReadWrite,
                                      FileShare.None))
                {
                    using (new ZipFile(zipFileToCreate)) { }
                }
            });
        }


        [Fact]
        public void CreateZip_AddFile()
        {
            int i;
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddFile.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("addfile-{0}", TestUtilities.GetMarker()));
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            using (ZipFile zip1 = new ZipFile())
            {
                for (i = 0; i < filesToZip.Length; i++)
                    zip1.AddFile(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<int>(CountEntries(zipFileToCreate),
                                 filesToZip.Length);
        }

        [Fact]
        public void CreateZip_AddFile_CharCase_wi13481()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddFile_CharCase_wi13481.zip");
            string marker = TestUtilities.GetMarker();
            string subdir = Path.Combine(TopLevelDir, String.Format("addfile-charcase-{0}", marker));

            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);
            Array.ForEach(filesToZip, x => {
                File.Move(x, Path.Combine(subdir, Path.GetFileName(x).ToUpper()));
            });

            filesToZip = Directory.GetFiles(subdir);

            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                    zip1.AddFile(filesToZip[i], "files");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<int>(CountEntries(zipFileToCreate),
                                 filesToZip.Length);

            int nEntries = 0;
            // now, verify that we have not downcased the filenames
            using (ZipFile zip2 = new ZipFile(zipFileToCreate))
            {
                foreach (var entry in zip2.Entries)
                {
                    var fname1 = Path.GetFileName(entry.FileName);
                    var fnameLower = fname1.ToLower();
                    Assert.False(fname1.Equals(fnameLower));
                    nEntries++;
                }
            }
            Assert.False(nEntries < 2);
        }


        [Fact]
        public void CreateZip_AddFileInDirectory()
        {
            string subdir = Path.Combine(TopLevelDir, String.Format("afid-{0}", TestUtilities.GetMarker()));
            TestUtilities.GenerateFilesFlat(subdir);
            for (int m = 0; m < 2; m++)
            {
                string directoryName = "";
                for (int k = 0; k < 4; k++)
                {
                    // select the name of the zip file
                    string zipFileToCreate = Path.Combine(TopLevelDir, $"CreateZip_AddFileInDirectory-trial{m}.{k}.zip");
                    _output.WriteLine("=====================");
                    _output.WriteLine("Trial {0}", k);
                    _output.WriteLine("Zipfile: {0}", zipFileToCreate);

                    directoryName = Path.Combine(directoryName, String.Format("{0:D2}", k));

                    string[] filesToSelectFrom =
                        Directory.GetFiles(subdir, "*.*", SearchOption.AllDirectories);

                    _output.WriteLine("using dirname: {0}", directoryName);

                    int n = _rnd.Next(filesToSelectFrom.Length / 2) + 2;
                    _output.WriteLine("Zipping {0} files", n);

                    // Create the zip archive
                    var addedFiles = new List<String>();
                    using (ZipFile zip1 = new ZipFile())
                    {
                        // add n files
                        int j=0;
                        for (int i = 0; i < n; i++)
                        {
                            // select files at random
                            while (addedFiles.Contains(filesToSelectFrom[j]))
                                j = _rnd.Next(filesToSelectFrom.Length);
                            zip1.AddFile(filesToSelectFrom[j], directoryName);
                            addedFiles.Add(filesToSelectFrom[j]);
                        }
                        zip1.Save(zipFileToCreate);
                    }

                    // Verify the number of files in the zip
                    Assert.Equal<int>(n, CountEntries(zipFileToCreate),
                        String.Format("Wrong number of entries in the zip file {0}",
                            zipFileToCreate));

                    using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                    {
                        foreach (var e in zip2)
                        {
                            _output.WriteLine("Check entry: {0}", e.FileName);
                            Assert.Equal<String>(directoryName, Path.GetDirectoryName(e.FileName),
                                String.Format("Wrong directory on zip entry {0}", e.FileName));
                        }
                    }
                }

                // add progressively more files to the fodder directory
                TestUtilities.GenerateFilesFlat(subdir);
            }

        }


        [Fact]
        public void CreateZip_AddFile_LeadingDot()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddFile_LeadingDot.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("leadingdot-{0}", TestUtilities.GetMarker()));
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < filesToZip.Length; i++)
                {
                    zip1.AddFile(filesToZip[i]);
                }
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<int>(filesToZip.Length, CountEntries(zipFileToCreate) );
        }


        [Fact]
        public void CreateZip_AddFiles_LeadingDot_Array()
        {
            // select the name of the zip file
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddFiles_LeadingDot_Array.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("leadingdot-array-{0}", TestUtilities.GetMarker()));
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            // Create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFiles(filesToZip);
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(filesToZip.Length, CountEntries(zipFileToCreate));
        }


        [Fact]
        public void CreateZip_AddFiles_PreserveDirHierarchy()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddFiles_PreserveDirHierarchy.zip");
            string dirToZip = Path.Combine(TopLevelDir, String.Format("zipthis-{0}", marker));

            // create a bunch of files
            int subdirCount;
            int entries = TestUtilities.GenerateFilesOneLevelDeep(_output, "PreserveDirHierarchy", dirToZip, null, out subdirCount);

            string[] filesToZip = Directory.GetFiles(dirToZip, "*.*", SearchOption.AllDirectories);

            Assert.Equal<int>(filesToZip.Length, entries);

            // Create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFiles(filesToZip, true, "");
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(filesToZip.Length, CountEntries(zipFileToCreate) );
        }

        private bool ArraysAreEqual(byte[] b1, byte[] b2)
        {
            return (CompareArrays(b1, b2) == 0);
        }


        private int CompareArrays(byte[] b1, byte[] b2)
        {
            if (b1 == null && b2 == null) return 0;
            if (b1 == null || b2 == null) return 0;
            if (b1.Length > b2.Length) return 1;
            if (b1.Length < b2.Length) return -1;
            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] > b2[i]) return 1;
                if (b1[i] < b2[i]) return -1;
            }
            return 0;
        }


        [Fact]
        public void CreateZip_AddEntry_ByteArray()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddEntry_ByteArray.zip");
            int entriesToCreate = _rnd.Next(42) + 12;
            var dict = new Dictionary<string, byte[]>();

            // Create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                for (int i = 0; i < entriesToCreate; i++)
                {
                    var b = new byte[_rnd.Next(1000) + 1000];
                    _rnd.NextBytes(b);
                    string filename = String.Format("Filename{0:D3}.bin", i);
                    var e = zip1.AddEntry(Path.Combine("data", filename), b);
                    dict.Add(e.FileName, b);
                }
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesToCreate, CountEntries(zipFileToCreate) );

            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                foreach (var e in zip1)
                {
                    // extract to a stream
                    using (var ms1 = new MemoryStream())
                    {
                        e.Extract(ms1);
                        Assert.True(ArraysAreEqual(ms1.ToArray(), dict[e.FileName]));
                    }
                }
            }
        }


        [Fact]
        public void CreateZip_AddFile_AddItem()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddFile_AddItem.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("additem-{0}", TestUtilities.GetMarker()));
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            // use the parameterized ctor
            using (ZipFile zip1 = new ZipFile(zipFileToCreate))
            {
                for (int i = 0; i < filesToZip.Length; i++)
                {
                    if (_rnd.Next(2) == 0)
                        zip1.AddFile(filesToZip[i], "files");
                    else
                        zip1.AddItem(filesToZip[i], "files");
                }
                zip1.Save();
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(filesToZip.Length, CountEntries(zipFileToCreate) );
        }


        private void DumpZipFile(ZipFile z)
        {
            _output.WriteLine("found {0} entries", z.Entries.Count);
            _output.WriteLine("RequiresZip64: '{0}'", z.RequiresZip64.HasValue ? z.RequiresZip64.Value.ToString() : "not set");
            _output.WriteLine("listing the entries in {0}...", String.IsNullOrEmpty(z.Name) ? "(zipfile)" : z.Name);
            foreach (var e in z)
            {
                _output.WriteLine("{0}", e.FileName);
            }
        }


        [Fact]
        public void CreateZip_ZeroEntries()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_ZeroEntries.zip");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Save(zipFileToCreate);
                DumpZipFile(zip1);
            }

            // workitem 7685
            using (ZipFile zip1 = new ZipFile(zipFileToCreate))
            {
                DumpZipFile(zip1);
            }

            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                DumpZipFile(zip1);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(0, CountEntries(zipFileToCreate) );
        }


        [Fact]
        public void CreateZip_Basic_ParameterizedSave()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_Basic_ParameterizedSave.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("parameterizedsave-{0}", TestUtilities.GetMarker()));
            int numFilesToCreate = _rnd.Next(23) + 14;
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir, numFilesToCreate);

            using (ZipFile zip1 = new ZipFile())
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < filesToZip.Length; i++)
                {
                    if (_rnd.Next(2) == 0)
                        zip1.AddFile(filesToZip[i], "files");
                    else
                        zip1.AddItem(filesToZip[i], "files");
                }
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(filesToZip.Length, CountEntries(zipFileToCreate) );
        }


        [Fact]
        public void CreateZip_AddFile_OnlyZeroLengthFiles()
        {
            _Internal_ZeroLengthFiles(_rnd.Next(33) + 3, "CZ_AddFile_OnlyZeroLengthFiles", null);
        }

        [Fact]
        public void CreateZip_AddFile_OnlyZeroLengthFiles_Password()
        {
            _Internal_ZeroLengthFiles(_rnd.Next(33) + 3, "CZ_AddFile_OnlyZeroLengthFiles_pwd", Path.GetRandomFileName());
        }

        [Fact]
        public void CreateZip_AddFile_OneZeroLengthFile()
        {
            _Internal_ZeroLengthFiles(1, "CZ_AddFile_OneZeroLengthFile", null);
        }


        [Fact]
        public void CreateZip_AddFile_OneZeroLengthFile_Password()
        {
            _Internal_ZeroLengthFiles(1, "CZ_AddFile_OneZeroLengthFile_Password", Path.GetRandomFileName());
        }


        private void _Internal_ZeroLengthFiles(int fileCount, string nameStub, string password)
        {
            _output.WriteLine("_Internal_ZeroLengthFiles TopLevelDir({0})", TopLevelDir);
            string zipFileToCreate = Path.Combine(TopLevelDir, nameStub + ".zip");
            int i;
            string[] filesToZip = new string[fileCount];
            for (i = 0; i < fileCount; i++)
                filesToZip[i] = TestUtilities.CreateUniqueFile("zerolength", TopLevelDir);

            var sw = new StringWriter();
            using (ZipFile zip = new ZipFile())
            {
                zip.StatusMessageTextWriter = sw;
                zip.Password = password;
                for (i = 0; i < filesToZip.Length; i++)
                    zip.AddFile(filesToZip[i]);
                zip.Save(zipFileToCreate);
            }

            string status = sw.ToString();
            _output.WriteLine("save output: " + status);

            BasicVerifyZip(zipFileToCreate, password);

            Assert.Equal<int>(CountEntries(zipFileToCreate), filesToZip.Length,
                                 "The zip file created has the wrong number of entries.");
        }


        [Fact]
        public void CreateZip_UpdateDirectory()
        {
            int i, j;
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_UpdateDirectory.zip");
            string marker = TestUtilities.GetMarker();
            string dirToZip = Path.Combine(TopLevelDir, String.Format("zipthis-{0}", marker));
            string dirPathInArchive = "dir-path-in-archive";

            Directory.CreateDirectory(dirToZip);

            _output.WriteLine("\n------------\nCreating files ...");

            int entries = 0;
            int subdirCount = _rnd.Next(17) + 14;
            //int subdirCount = _rnd.Next(3) + 2;
            var FileCount = new Dictionary<string, int>();
            var checksums = new Dictionary<string, byte[]>();

            for (i = 0; i < subdirCount; i++)
            {
                string subdirShort = String.Format("dir{0:D4}", i);
                string subdir = Path.Combine(dirToZip, subdirShort);
                Directory.CreateDirectory(subdir);

                int filecount = _rnd.Next(8) + 7;
                //int filecount = _rnd.Next(2) + 2;
                FileCount[subdirShort] = filecount;
                for (j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = Path.Combine(subdir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 100);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var key = Path.Combine(dirPathInArchive, subdirShort, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    _output.WriteLine("chk[{0}]= {1}", key,
                                          TestUtilities.CheckSumToString(chk));
                    checksums.Add(key, chk);
                    entries++;
                }
            }

            //Directory.SetCurrentDirectory(TopLevelDir); race conditions

            _output.WriteLine("\n------------\nAdding files into the Zip...");

            // add all the subdirectories into a new zip
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(dirToZip, dirPathInArchive);
                zip1.Save(zipFileToCreate);
            }

            _output.WriteLine("\n");

            Assert.Equal<int>(entries, CountEntries(zipFileToCreate),
                                 "The Zip file has an unexpected number of entries.");

            _output.WriteLine("\n------------\nExtracting and validating checksums...");

            // validate all the checksums
            string unpackDir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "unpack"));

            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    e.Extract(unpackDir);
                    string pathToExtractedFile = Path.Combine(unpackDir, e.FileName);

                    // if it is a file....
                    if (checksums.ContainsKey(e.FileName))
                    {
                        // verify the checksum of the file is correct
                        string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                        string actualCheckString = TestUtilities.GetCheckSumString(pathToExtractedFile);
                        Assert.Equal<String>
                            (expectedCheckString,
                             actualCheckString,
                                String.Format("Unexpected checksum on extracted filesystem file ({0}).",
                                    pathToExtractedFile));
                    }
                }
            }

            _output.WriteLine("\n------------\nCreating some new files ...");

            // now, update SOME of the existing files
            string updatesDir = Path.Combine(TopLevelDir,
                String.Format("updated-{0}", Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
            Directory.CreateDirectory(updatesDir);
            int hadUpdates = 0;
            for (i = 0; i < subdirCount; i++)
            {
                string subdirShort = String.Format("dir{0:D4}", i);
                string subdir = Path.Combine(updatesDir, subdirShort);
                Directory.CreateDirectory(subdir);

                int filecount = FileCount[subdirShort];
                for (j = 0; j < filecount; j++)
                {
                    if (_rnd.Next(2) == 1)
                    {
                        string filename = String.Format("file{0:D4}.x", j);
                        TestUtilities.CreateAndFillFile(Path.Combine(subdir, filename),
                                                        _rnd.Next(1000) + 100);
                        string fqFilename = Path.Combine(subdir, filename);

                        var chk = TestUtilities.ComputeChecksum(fqFilename);
                        //var t1 = Path.GetFileName(dirToZip);
                        var t2 = Path.Combine(dirPathInArchive, subdirShort);
                        var key = Path.Combine(t2, filename);
                        key = TestUtilities.TrimVolumeAndSwapSlashes(key);

                        _output.WriteLine("chk[{0}]= {1}", key,
                                              TestUtilities.CheckSumToString(chk));

                        checksums.Remove(key);
                        checksums.Add(key, chk);
                        hadUpdates++;
                    }
                }
            }

            Assert.True(hadUpdates > 1, "not enough updates!");
            _output.WriteLine("\n------------\nUpdating some of the files in the zip...");
            // add some new content
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                zip3.UpdateDirectory(updatesDir, dirPathInArchive);
                //String[] dirs = Directory.GetDirectories(dirToZip);

                //foreach (String d in dirs)
                //{
                //    string dir = Path.Combine(Path.GetFileName(dirToZip), Path.GetFileName(d));
                //    //string root = Path.Combine("zipthis", Path.GetFileName(d));
                //    zip3.UpdateDirectory(dir, "zipthis");
                //}
                zip3.Save();
            }

            _output.WriteLine("\n------------\nValidating the checksums for all of the files ...");

            // validate all the checksums again
            unpackDir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "unpack2"));
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip4)
                    _output.WriteLine("Found entry: {0}", e.FileName);

                foreach (ZipEntry e in zip4)
                {
                    e.Extract(unpackDir);
                    if (!e.IsDirectory)
                    {
                        string pathToExtractedFile = Path.Combine(unpackDir, e.FileName);

                        // verify the checksum of the file is correct
                        string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                        string actualCheckString = TestUtilities.GetCheckSumString(pathToExtractedFile);
                        Assert.Equal<String>
                            (expectedCheckString,
                             actualCheckString,
                                String.Format("Unexpected checksum on extracted filesystem file ({0}).",
                                    pathToExtractedFile));
                    }
                }
            }
        }


        [Fact]
        public void CreateZip_AddDirectory_OnlyZeroLengthFiles()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("AddDirectory_OnlyZeroLengthFiles-{0}.zip", marker));
            string dirToZip = Path.Combine(TopLevelDir, String.Format("adddir-zerolengthfiles-{0}", marker));
            Directory.CreateDirectory(dirToZip);
            int numEntries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string subdir = Path.Combine(dirToZip, "dir" + i);
                Directory.CreateDirectory(subdir);
                int n = _rnd.Next(6) + 2;
                for (int j=0; j < n; j++)
                {
                    TestUtilities.CreateUniqueFile("bin", subdir);
                    numEntries++;
                }
            }

            using (var zip = new ZipFile())
            {
                zip.AddDirectory(dirToZip,"");
                zip.Save(zipFileToCreate);
            }

            Assert.Equal<int>(numEntries, CountEntries(zipFileToCreate) );
        }


        [Fact]
        public void CreateZip_AddDirectory_OneZeroLengthFile()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneZeroLengthFile.zip");
            string marker = TestUtilities.GetMarker();
            string dirToZip = Path.Combine(TopLevelDir, String.Format("adddir-1zerolength-file-{0}", marker));
            var info = Directory.CreateDirectory(dirToZip);
            _output.WriteLine("created directory {0}", info.FullName);

            // one empty file
            string file = TestUtilities.CreateUniqueFile("ZeroLengthFile.txt", info.FullName);

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(info.FullName,"");
                zip.Save(zipFileToCreate);
            }

            Assert.Equal<int>(1, CountEntries(zipFileToCreate));
        }


        [Fact]
        public void CreateZip_AddDirectory_OnlyEmptyDirectories()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OnlyEmptyDirectories.zip");
            string marker = TestUtilities.GetMarker();
            string dirToZip = Path.Combine(TopLevelDir, String.Format("adddir-onlyemptydirs-{0}", marker));
            Directory.CreateDirectory(dirToZip);

            int subdirCount = _rnd.Next(28) + 18;
            for (int i = 0; i < subdirCount; i++)
            {
                string subdir = Path.Combine(dirToZip, "EmptyDir" + i);
                Directory.CreateDirectory(subdir);
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(dirToZip, "");
                zip.Save(zipFileToCreate);
            }

            int numFound = CountEntries(zipFileToCreate);
            Assert.Equal<int>(0, numFound, String.Format("unexpected number of entries expected({0}) actual({1})", 0, numFound));
        }


        [Fact]
        public void CreateZip_AddDirectory_OneEmptyDirectory()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneEmptyDirectory.zip");
            string dirToZip = Path.Combine(TopLevelDir, String.Format("adddir-1emptydir-{0}", marker));
            Directory.CreateDirectory(dirToZip);

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(dirToZip, "");
                zip.Save(zipFileToCreate);
            }

            Assert.Equal<int>(0, CountEntries(zipFileToCreate));
            BasicVerifyZip(zipFileToCreate);
        }


        [Fact]
        public void CreateZip_WithEmptyDirectory()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "Create_WithEmptyDirectory.zip");
            string subdir = Path.Combine(TopLevelDir, String.Format("withemptydir-{0}",marker));
            Directory.CreateDirectory(subdir);
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(subdir, "");
                zip.Save(zipFileToCreate);
            }
            Assert.Equal<int>(0, CountEntries(zipFileToCreate));
            BasicVerifyZip(zipFileToCreate);
        }



        [Fact]
        public void CreateZip_AddDirectory_CheckStatusTextWriter()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_CheckStatusTextWriter.zip");
            string marker = TestUtilities.GetMarker();
            string dirToZip = Path.Combine(TopLevelDir, String.Format("adddir-chkstattus-{0}",marker));
            Directory.CreateDirectory(dirToZip);

            int entries = 0;
            int subdirCount = _rnd.Next(8) + 8;
            for (int i = 0; i < subdirCount; i++)
            {
                string subdir = Path.Combine(dirToZip, "Dir" + i);
                Directory.CreateDirectory(subdir);
                // a few files per subdir
                int fileCount = _rnd.Next(12) + 4;
                for (int j = 0; j < fileCount; j++)
                {
                    string file = Path.Combine(subdir, "File" + j);
                    TestUtilities.CreateAndFillFile(file, 1020);
                    entries++;
                }
            }

            var sw = new StringWriter();
            using (ZipFile zip = new ZipFile())
            {
                zip.StatusMessageTextWriter = sw;
                zip.AddDirectory(dirToZip,"");
                zip.Save(zipFileToCreate);
            }

            string status = sw.ToString();
            _output.WriteLine("save output: " + status);

            Assert.True(status.Length > 24 * entries,
                String.Format("status messages? ({0}!>{1})",
                    status.Length, 24 * entries));

            int n = CountEntries(zipFileToCreate);
            Assert.Equal<int>(entries, n,
                String.Format("wrong number of entries. ({0}!={1})", n, entries));

            BasicVerifyZip(zipFileToCreate);
        }


        struct TestTrial
        {
            public string arg;
            public string re;
        }


        [Fact]
        public void CreateZip_AddDirectory_XXX()
        {
            _output.WriteLine("\n--------------------------------\nAddDirectory TopLevelDir: {0}", TopLevelDir);
            TestTrial[] trials = {
                new TestTrial { arg=null, re="^file(\\d+).ext$"},
                new TestTrial { arg="", re="^file(\\d+).ext$"},
                new TestTrial { arg=null, re="^file(\\d+).ext$"},
                new TestTrial { arg="Xabf", re="(?s)^Xabf/(file(\\d+).ext)?$"},
                new TestTrial { arg="AAAA/BBB", re="(?s)^AAAA/BBB/(file(\\d+).ext)?$"}
            };

            for (int k = 0; k < trials.Length; k++)
            {
                _output.WriteLine("\n---- trial {0}", k);
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("CreateZip_AddDirectory-{0}.zip", k));

                Assert.False(File.Exists(zipFileToCreate),
                    String.Format("The temporary zip file '{0}' already exists.", zipFileToCreate));

                string dirToZip = Path.Combine(TopLevelDir, String.Format("DirectoryToZip.{0}.test", k));
                Directory.CreateDirectory(dirToZip);

                int fileCount = _rnd.Next(5) + 4;
                for (int i = 0; i < fileCount; i++)
                {
                    String file = Path.Combine(dirToZip, String.Format("file{0:D3}.ext", i));
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(2000) + 500);
                }

                var sw = new StringWriter();
                using (ZipFile zip = new ZipFile())
                {
                    zip.StatusMessageTextWriter = sw;
                    if (k == 0)
                        zip.AddDirectory(dirToZip);
                    else
                        zip.AddDirectory(dirToZip, trials[k].arg);
                    zip.Save(zipFileToCreate);
                }
                _output.WriteLine(sw.ToString());
                int n = CountEntries(zipFileToCreate);
                Assert.Equal<int>(fileCount, n,
                    String.Format("wrong number of entries in cycle {0}: expected({1}) actual({2}).", k, fileCount, n));

                //_output.WriteLine("");
                // verify that the entries in the zip are in the top level directory!!
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    foreach (ZipEntry e in zip2)
                        _output.WriteLine("found entry: {0}", e.FileName);
                    foreach (ZipEntry e in zip2)
                    {
                        //Assert.IsFalse(e.FileName.StartsWith("dir"),
                        //       String.Format("The Zip entry '{0}' is not rooted in top level directory.", e.FileName));

                        // check the filename:
                        //RE.Match m0 = RE.Regex.Match(e.FileName, fnameRegex[k]);
                        // Assert.IsTrue(m0 != null, "No match");
                        // Assert.AreEqual<int>(m0.Groups.Count, 2,
                        //    String.Format("In cycle {0}, Matching {1} against {2}, Wrong number of matches ({3})",
                        //        k, e.FileName, fnameRegex[k], m0.Groups.Count));

                        Assert.True(RE.Regex.IsMatch(e.FileName, trials[k].re),
                                      String.Format("In cycle {0}, Matching {1} against {2}", k, e.FileName, trials[k].re));
                    }
                }
            }
        }


        [Fact]
        public void CreateZip_AddDirectory_Nested()
        {
            // Each trial provides a directory name into which to add
            // files, and a regex, used for verification after the zip
            // is created, to match the names on any added entries.
            TestTrial[] trials = {
                new TestTrial { arg=null, re="^dir(\\d){3}/(file(\\d+).ext)?$"},
                new TestTrial { arg="", re="^dir(\\d){3}/(file(\\d+).ext)?$"},
                new TestTrial { arg=null, re="^dir(\\d){3}/(file(\\d+).ext)?$"},
                new TestTrial { arg="rtdha", re="(?s)^rtdha/(dir(\\d){3}/(file(\\d+).ext)?)?$"},
                new TestTrial { arg="sdfjk/BBB", re="(?s)^sdfjk/BBB/(dir(\\d){3}/(file(\\d+).ext)?)?$"}
            };

            for (int k = 0; k < trials.Length; k++)
            {
                _output.WriteLine("\n--------------------------------\n\n\n");
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("CreateZip_AddDirectory_Nested-{0}.zip", k));

                Assert.False(File.Exists(zipFileToCreate),
                    String.Format("The temporary zip file '{0}' already exists.", zipFileToCreate));

                string dirToZip = Path.Combine(TopLevelDir, String.Format("adddir-nested.{0}.test", k));
                Directory.CreateDirectory(dirToZip);

                int i, j;
                int entries = 0;
                int subdirCount = _rnd.Next(23) + 7;
                for (i = 0; i < subdirCount; i++)
                {
                    string subdir = Path.Combine(dirToZip, String.Format("dir{0:D3}", i));
                    Directory.CreateDirectory(subdir);

                    int fileCount = _rnd.Next(8);  // sometimes zero
                    for (j = 0; j < fileCount; j++)
                    {
                        String file = Path.Combine(subdir, String.Format("file{0:D3}.ext", j));
                        TestUtilities.CreateAndFillFile(file, _rnd.Next(10750) + 50);
                        entries++;
                    }
                }

                var sw = new StringWriter();
                using (ZipFile zip = new ZipFile())
                {
                    zip.StatusMessageTextWriter = sw;
                    if (k == 0)
                        zip.AddDirectory(dirToZip);
                    else
                        zip.AddDirectory(dirToZip, trials[k].arg);
                    zip.Save(zipFileToCreate);
                }
                _output.WriteLine(sw.ToString());

                Assert.Equal<int>(CountEntries(zipFileToCreate), entries,
                    String.Format("The zip file created in cycle {0} has the wrong number of entries.", k));

                // verify that the entries in the zip are in the top level directory!!
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    foreach (ZipEntry e in zip2)
                        _output.WriteLine("found entry: {0}", e.FileName);
                    foreach (ZipEntry e in zip2)
                    {
                        Assert.True(RE.Regex.IsMatch(e.FileName, trials[k].re),
                            String.Format("In cycle {0}, Matching {1} against {2}", k, e.FileName, trials[k].re));
                    }

                }
            }
        }


        [Fact]
        public void Basic_SaveToFileStream()
        {
            // from small numbers of files to larger numbers of files
            for (int k = 0; k < 3; k++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("SaveToFileStream-t{0}.zip", k));
                string dirToZip = Path.Combine(TopLevelDir, Path.GetRandomFileName());
                Directory.CreateDirectory(dirToZip);

                int filesToAdd = _rnd.Next(k * 10 + 3) + k * 10 + 3;
                for (int i = 0; i < filesToAdd; i++)
                {
                    var s = Path.Combine(dirToZip, String.Format("tempfile-{0}.bin", i));
                    int sz = _rnd.Next(10000) + 5000;
                    _output.WriteLine("  Creating file: {0} sz({1})", s, sz);
                    TestUtilities.CreateAndFillFileBinary(s, sz);
                }

                using (var fileStream = File.Create(zipFileToCreate))
                {
                    using (ZipFile zip1 = new ZipFile())
                    {
                        zip1.AddDirectory(dirToZip, "");
                        zip1.Comment = "This is a Comment On the Archive (AM/PM)";
                        zip1.Save(fileStream);
                    }
                }

                // Verify the files are in the zip
                Assert.Equal<int>(CountEntries(zipFileToCreate),
                                     filesToAdd,
                    String.Format("Trial {0}: file {1} wrong number of entries.",
                        k, zipFileToCreate));
            }
        }



        [Fact]
        public void Basic_IsText()
        {
            // from small numbers of files to larger numbers of files
            for (int k = 0; k < 3; k++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Basic_IsText-trial{0}.zip", k));
                string dirToZip = Path.Combine(TopLevelDir, Path.GetRandomFileName());
                Directory.CreateDirectory(dirToZip);

                int filesToAdd = _rnd.Next(33) + 11;
                for (int i = 0; i < filesToAdd; i++)
                {
                    var s = Path.Combine(dirToZip, String.Format("tempfile-{0}.txt", i));
                    int sz = _rnd.Next(10000) + 5000;
                    _output.WriteLine("  Creating file: {0} sz({1})", s, sz);
                    TestUtilities.CreateAndFillFileText(s, sz);
                }

                using (ZipFile zip1 = new ZipFile())
                {
                    int count = 0;
                    var filesToZip = Directory.GetFiles(dirToZip);
                    foreach (var f in filesToZip)
                    {
                        var e = zip1.AddFile(f, "files");
                        switch (k)
                        {
                            case 0: break;
                            case 1: if ((count % 2) == 0) e.IsText = true; break;
                            case 2: if ((count % 2) != 0) e.IsText = true; break;
                            case 3: e.IsText = true; break;
                        }
                        count++;
                    }
                    zip1.Comment = "This is a Comment On the Archive (AM/PM)";
                    zip1.Save(zipFileToCreate);
                }

                // Verify the files are in the zip
                Assert.Equal<int>(filesToAdd, CountEntries(zipFileToCreate),
                    String.Format("trial {0}: file {1} number of entries.", k, zipFileToCreate));

                // verify the isText setting
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    int count = 0;
                    foreach (var e in zip2)
                    {
                        switch (k)
                        {
                            case 0: Assert.False(e.IsText); break;
                            case 1: Assert.Equal<bool>((count % 2) == 0, e.IsText); break;
                            case 2: Assert.Equal<bool>((count % 2) != 0, e.IsText); break;
                            case 3: Assert.True(e.IsText); break;
                        }
                        count++;
                    }
                }
            }
        }


        [Fact]
        public void CreateZip_VerifyThatStreamRemainsOpenAfterSave()
        {
            Ionic.Zlib.CompressionLevel[] compressionLevelOptions = {
                Ionic.Zlib.CompressionLevel.None,
                Ionic.Zlib.CompressionLevel.BestSpeed,
                Ionic.Zlib.CompressionLevel.Default,
                Ionic.Zlib.CompressionLevel.BestCompression,
            };

            string[] Passwords = { null, Path.GetRandomFileName() };

            for (int j = 0; j < Passwords.Length; j++)
            {
                for (int k = 0; k < compressionLevelOptions.Length; k++)
                {
                    _output.WriteLine("\n\n---------------------------------\n" +
                                          "Trial ({0},{1}):  Password='{2}' Compression={3}\n",
                                          j, k, Passwords[j], compressionLevelOptions[k]);
                    string dirToZip = Path.Combine(TopLevelDir, Path.GetRandomFileName());
                    Directory.CreateDirectory(dirToZip);

                    int filesAdded = _rnd.Next(3) + 3;
                    for (int i = 0; i < filesAdded; i++)
                    {
                        var s = Path.Combine(dirToZip, String.Format("tempfile-{0}-{1}-{2}.bin", j, k, i));
                        int sz = _rnd.Next(10000) + 5000;
                        _output.WriteLine("  Creating file: {0} sz({1})", s, sz);
                        TestUtilities.CreateAndFillFileBinary(s, sz);
                    }

                    _output.WriteLine("\n");

                    //string dirToZip = Path.GetFileName(TopLevelDir);
                    var ms = new MemoryStream();
                    Assert.True(ms.CanSeek, String.Format("Trial {0}: The output MemoryStream does not do Seek.", k));
                    using (ZipFile zip1 = new ZipFile())
                    {
                        zip1.CompressionLevel = compressionLevelOptions[k];
                        zip1.Password = Passwords[j];
                        zip1.Comment = String.Format("Trial ({0},{1}):  Password='{2}' Compression={3}\n",
                                                     j, k, Passwords[j], compressionLevelOptions[k]);
                        zip1.AddDirectory(dirToZip, "");
                        zip1.Save(ms);
                    }

                    Assert.True(ms.CanSeek, String.Format("Trial {0}: After writing, the OutputStream does not do Seek.", k));
                    Assert.True(ms.CanRead, String.Format("Trial {0}: The OutputStream cannot be Read.", k));

                    // seek to the beginning
                    ms.Seek(0, SeekOrigin.Begin);
                    int filesFound = 0;
                    using (ZipFile zip2 = ZipFile.Read(ms))
                    {
                        foreach (ZipEntry e in zip2)
                        {
                            _output.WriteLine("  Found entry: {0} isDir({1}) sz_c({2}) sz_unc({3})", e.FileName, e.IsDirectory, e.CompressedSize, e.UncompressedSize);
                            if (!e.IsDirectory)
                                filesFound++;
                        }
                    }
                    Assert.Equal<int>(filesFound, filesAdded,
                        String.Format("Trial {0}", k));
                }
            }
        }


        [Fact]
        public void CreateZip_AddFile_VerifyCrcAndContents()
        {
            string marker = TestUtilities.GetMarker();
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddFile_VerifyCrcAndContents.zip");

            string subdir = Path.Combine(TopLevelDir, $"addfile-verifycrc-{marker}");
            Directory.CreateDirectory(subdir);

            // create the files
            int numFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < numFilesToCreate; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                             Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::CreateZip_AddFile_VerifyCrcAndContents(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            Assert.Equal<int>(CountEntries(zipFileToCreate),entriesAdded);

            string extractDir = Path.Combine(TopLevelDir, $"ex-{marker}");

            // now extract the files and verify their contents
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string s in zip2.EntryFileNames)
                {
                    repeatedLine = String.Format("This line is repeated over and over and over in file {0}", s);
                    zip2[s].Extract(extractDir);

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(extractDir, s));
                    string actualLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, actualLine,
                        String.Format("Updated file ({0}) is incorrect.", s));
                }
            }
        }


        [Fact]
        public void Extract_IntoMemoryStream()
        {
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            string zipFileToCreate = Path.Combine(TopLevelDir, "Extract_IntoMemoryStream.zip");
            string marker = TestUtilities.GetMarker();
            string subdir = Path.Combine(TopLevelDir, String.Format("into-memstream-{0}", marker));
            Directory.CreateDirectory(subdir);

            // create the files
            int numFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < numFilesToCreate; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                             Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Create the zip file
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "BasicTests::Extract_IntoMemoryStream()";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                                 "The Zip file has the wrong number of entries.");

            // now extract the files into memory streams, checking only the length of the file.
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string s in zip2.EntryFileNames)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        zip2[s].Extract(ms);
                        byte[] a = ms.ToArray();
                        string f = Path.Combine(subdir, s);
                        var fi = new FileInfo(f);
                        Assert.Equal<int>((int)(fi.Length), a.Length, "Unequal file lengths.");
                    }
                }
            }
        }


        [Fact]
        public void Retrieve_ViaIndexer2_wi11056()
        {
            string fileName = "wi11056.dwf";
            string expectedEntryName = @"com.autodesk.dwf.ePlot_5VFMLy3OdEetAPFe7uWXYg\descriptor.xml";
            string srcDir = TestUtilities.GetTestSrcDir();
            string filename = Path.Combine(srcDir, "data", fileName);

            _output.WriteLine("Reading zip file: '{0}'", filename);
            using (ZipFile zip = ZipFile.Read(filename))
            {
                var e = zip[expectedEntryName];
                Assert.False(e == null,
                               "Retrieval by stringindex failed.");
            }
        }

        [Fact]
        public void Retrieve_ViaIndexer()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "Retrieve_ViaIndexer.zip");
            string filename = null;
            int entriesAdded = 0;
            string repeatedLine = null;
            int j;

            string subdir = Path.Combine(TopLevelDir, String.Format("retreieve-via-indexerA-{0}", marker));
            Directory.CreateDirectory(subdir);

            // create the files
            int numFilesToCreate = _rnd.Next(10) + 8;
            for (j = 0; j < numFilesToCreate; j++)
            {
                filename = Path.Combine(subdir, String.Format("File{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                                             Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(23000) + 4000);
                entriesAdded++;
            }

            // Create the zip file
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "BasicTests::Retrieve_ViaIndexer()";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(CountEntries(zipFileToCreate), entriesAdded,
                                 "The Zip file has the wrong number of entries.");

            // now extract the files into memory streams, checking only the length of the file.
            // We do 4 combinations:  case-sensitive on or off, and filename conversion on or off.
            for (int m = 0; m < 2; m++)
            {
                for (int n = 0; n < 2; n++)
                {
                    using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                    {
                        if (n == 1) zip2.CaseSensitiveRetrieval = true;
                        foreach (string s in zip2.EntryFileNames)
                        {
                            var s2 = (m == 1) ? s.ToUpper() : s;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                try
                                {
                                    zip2[s2].Extract(ms);
                                    byte[] a = ms.ToArray();
                                    string f = Path.Combine(subdir, s2);
                                    var fi = new FileInfo(f);
                                    Assert.Equal<int>((int)(fi.Length), a.Length, "Unequal file lengths.");
                                }
                                catch
                                {
                                    Assert.Equal<int>(1, n * m, "Indexer retrieval failed unexpectedly.");
                                }
                            }
                        }
                    }
                }
            }
        }



        [Fact]
        public void CreateZip_SetFileComments()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "FileComments.zip");
            string FileCommentFormat = "Comment Added By Test to file '{0}'";
            string commentOnArchive = "Comment added by FileComments() method.";

            int fileCount = _rnd.Next(3) + 3;
            string[] filesToZip = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                filesToZip[i] = Path.Combine(TopLevelDir, String.Format("file{0:D3}.bin", i));
                TestUtilities.CreateAndFillFile(filesToZip[i], _rnd.Next(10000) + 5000);
            }

            using (ZipFile zip = new ZipFile())
            {
                //zip.StatusMessageTextWriter = System.Console.Out;
                for (int i = 0; i < filesToZip.Length; i++)
                {
                    // use the local filename (not fully qualified)
                    ZipEntry e = zip.AddFile(filesToZip[i], "");
                    e.Comment = String.Format(FileCommentFormat, e.FileName);
                }
                zip.Comment = commentOnArchive;
                zip.Save(zipFileToCreate);
            }

            int entries = 0;
            using (ZipFile z2 = ZipFile.Read(zipFileToCreate))
            {
                Assert.Equal<String>(commentOnArchive, z2.Comment, "Unexpected comment on ZipFile.");
                foreach (ZipEntry e in z2)
                {
                    string expectedComment = String.Format(FileCommentFormat, e.FileName);
                    Assert.Equal<string>(expectedComment, e.Comment, "Unexpected comment on ZipEntry.");
                    entries++;
                }
            }
            Assert.Equal<int>(entries, filesToZip.Length,
                String.Format("Unexpected file count. Expected {0}, got {1}.",
                    filesToZip.Length, entries));
        }


        [Fact]
        public void CreateZip_SetFileLastModified()
        {
            string marker = TestUtilities.GetMarker();
            //int fileCount = _rnd.Next(13) + 23;
            int fileCount = _rnd.Next(3) + 2;
            string[] filesToZip = new string[fileCount];
            for (int i = 0; i < fileCount; i++)
            {
                filesToZip[i] = Path.Combine(TopLevelDir, String.Format("file{0:D3}.bin", i));
                TestUtilities.CreateAndFillFileBinary(filesToZip[i], _rnd.Next(10000) + 5000);
            }
            DateTime[] timestamp =
                {
                    new System.DateTime(2017, 9, 1, 15, 0, 0),
                    new System.DateTime(2017, 4, 2, 14, 0, 0),
                    new System.DateTime(2017, 5, 18, 19, 0, 0),
                };

            // try Kind = unspecified, local, and UTC
            DateTimeKind[] timestampKinds =
                {
                    DateTimeKind.Unspecified,
                    DateTimeKind.Local,
                    DateTimeKind.Utc,
                };

            Boolean[] wantWindowsFormat = { false, true };

            for (int m = 0; m < timestamp.Length; m++)
            {
                for (int n = 0; n < timestampKinds.Length; n++)
                {
                    for (int k = 0; k < wantWindowsFormat.Length; k++)
                    {
                        string zipFileToCreate = Path.Combine(TopLevelDir,
                            String.Format("CreateZip-SetFileLastModified-{0}.{1}.{2}.zip", m, n, k));
                        _output.WriteLine("Cycle {0}.{1}.{2}", m, n, k);
                        _output.WriteLine("zipfile {0}", zipFileToCreate);
                        DateTime t = DateTime.SpecifyKind(timestamp[m], timestampKinds[n]);

                        using (ZipFile zip = new ZipFile())
                        {
                            zip.EmitTimesInWindowsFormatWhenSaving = wantWindowsFormat[k];

                            for (int i = 0; i < filesToZip.Length; i++)
                            {
                                ZipEntry e = zip.AddFile(filesToZip[i], "");
                                e.LastModified = t;
                            }
                            zip.Comment = "All files in this archive have the same LastModified value.";
                            zip.Save(zipFileToCreate);
                        }

                        // NB: comparing two DateTime variables will return "not equal" if they
                        // are not of the same "Kind", even if they represent the same point in
                        // time.  To avoid that pitfall, compare using UniversalTime.

                        var x1 = t.ToUniversalTime().ToString("u");

                        string unpackDir = Path.Combine(TopLevelDir,
                            String.Format("unpack-{0}-{1}.{2}.{3}", marker, m, n, k));
                        int entries = 0;
                        using (ZipFile z2 = ZipFile.Read(zipFileToCreate))
                        {
                            foreach (ZipEntry e in z2)
                            {
                                var t2 = e.LastModified;
                                var x2 = t2.ToUniversalTime().ToString("u");
                                Assert.Equal<String>(x1, x2,
                                    String.Format("cycle {0}.{1}.{2}: Unexpected LastModified value on ZipEntry.", m, n, k));
                                entries++;
                                // now verify that the LastMod time on the filesystem file is set correctly
                                e.Extract(unpackDir);
                                DateTime ActualFilesystemLastMod = File.GetLastWriteTime(Path.Combine(unpackDir, e.FileName));
                                ActualFilesystemLastMod = AdjustTime_Win32ToDotNet(ActualFilesystemLastMod);

                                //Assert.AreEqual<DateTime>(t, ActualFilesystemLastMod,
                                x2 = ActualFilesystemLastMod.ToUniversalTime().ToString("u");
                                Assert.Equal<String>(x1, x2,
                                    String.Format("cycle {0}.{1}.{2}: Unexpected LastWriteTime on extracted filesystem file.", m, n, k));
                            }
                        }
                        Assert.Equal<int>(entries, filesToZip.Length,
                            String.Format("Unexpected file count. Expected {0}, got {1}.",
                                filesToZip.Length, entries));
                    }
                }
            }
        }


        [Fact]
        public void CreateAndExtract_VerifyAttributes()
        {
            try
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, "CreateAndExtract_VerifyAttributes.zip");
                string marker = TestUtilities.GetMarker();
                string subdir = Path.Combine(TopLevelDir, String.Format("verify-attrs-{0}", marker));
                Directory.CreateDirectory(subdir);

                //int fileCount = _rnd.Next(13) + 23;
                FileAttributes[] attributeCombos = {
                    FileAttributes.ReadOnly,
                    FileAttributes.ReadOnly | FileAttributes.System,
                    FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Hidden,
                    FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Hidden | FileAttributes.Archive,
                    FileAttributes.ReadOnly | FileAttributes.Hidden,
                    FileAttributes.ReadOnly | FileAttributes.Hidden| FileAttributes.Archive,
                    FileAttributes.ReadOnly | FileAttributes.Archive,
                    FileAttributes.System,
                    FileAttributes.System | FileAttributes.Hidden,
                    FileAttributes.System | FileAttributes.Hidden | FileAttributes.Archive,
                    FileAttributes.System | FileAttributes.Archive,
                    FileAttributes.Hidden,
                    FileAttributes.Hidden | FileAttributes.Archive,
                    FileAttributes.Archive,
                    FileAttributes.Normal,
                    FileAttributes.NotContentIndexed | FileAttributes.ReadOnly,
                    FileAttributes.NotContentIndexed | FileAttributes.System,
                    FileAttributes.NotContentIndexed | FileAttributes.Hidden,
                    FileAttributes.NotContentIndexed | FileAttributes.Archive,
                    FileAttributes.Temporary,
                    FileAttributes.Temporary | FileAttributes.Archive,
                };
                int fileCount = attributeCombos.Length;
                string[] filesToZip = new string[fileCount];
                _output.WriteLine("============\nCreating.");
                for (int i = 0; i < fileCount; i++)
                {
                    filesToZip[i] = Path.Combine(subdir, String.Format("file{0:D3}.bin", i));
                    TestUtilities.CreateAndFillFileBinary(filesToZip[i], _rnd.Next(10000) + 5000);
                    _output.WriteLine("Creating {0}    [{1}]", filesToZip[i], attributeCombos[i].ToString());
                    File.SetAttributes(filesToZip[i], attributeCombos[i]);
                }

                _output.WriteLine("============\nZipping.");
                using (ZipFile zip = new ZipFile())
                {
                    for (int i = 0; i < filesToZip.Length; i++)
                    {
                        // use the local filename (not fully qualified)
                        ZipEntry e = zip.AddFile(filesToZip[i], "");
                    }
                    zip.Save(zipFileToCreate);
                }

                int entries = 0;
                _output.WriteLine("============\nExtracting.");
                string unpackDir = Path.Combine(TopLevelDir, String.Format("unpack-{0}", marker));
                using (ZipFile z2 = ZipFile.Read(zipFileToCreate))
                {
                    foreach (ZipEntry e in z2)
                    {
                        _output.WriteLine("Extracting {0}", e.FileName);
                        Assert.Equal<FileAttributes>
                            (attributeCombos[entries],
                             e.Attributes,
                                String.Format("unexpected attribute {0} 0x{1:X4}",
                             e.FileName,
                                    (int)e.Attributes));
                        entries++;
                        e.Extract(unpackDir);
                        // now verify that the attributes are set
                        // correctly in the filesystem
                        var attrs = File.GetAttributes(Path.Combine(unpackDir, e.FileName));
                        Assert.Equal<FileAttributes>(attrs, e.Attributes,
                            String.Format("Unexpected attributes on the extracted filesystem file {0}.", e.FileName));
                    }
                }
                Assert.Equal<int>(filesToZip.Length,entries,
                    String.Format("Bad file count. Expected {0}, got {1}.",
                        filesToZip.Length, entries));
            }
            catch (Exception ex1)
            {
                _output.WriteLine("Exception: " + ex1);
                throw;
            }
        }



        [Fact]
        public void CreateAndExtract_SetAndVerifyAttributes()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateAndExtract_SetAndVerifyAttributes.zip");

            // Build a list of combinations of FileAttributes to try.  Not feasible to
            // do an exhaustive combination because (a) not all combinations are valid, and (b)
            // if you SetAttributes(file,Compressed) (also with Encrypted, ReparsePoint) it does
            // not "work."  So those attributes must be excluded.
            FileAttributes[] attributeCombos = {
                FileAttributes.ReadOnly,
                FileAttributes.ReadOnly | FileAttributes.System,
                FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Hidden,
                FileAttributes.ReadOnly | FileAttributes.System | FileAttributes.Hidden | FileAttributes.Archive,
                FileAttributes.ReadOnly | FileAttributes.Hidden,
                FileAttributes.ReadOnly | FileAttributes.Hidden| FileAttributes.Archive,
                FileAttributes.ReadOnly | FileAttributes.Archive,
                FileAttributes.System,
                FileAttributes.System | FileAttributes.Hidden,
                FileAttributes.System | FileAttributes.Hidden | FileAttributes.Archive,
                FileAttributes.System | FileAttributes.Archive,
                FileAttributes.Hidden,
                FileAttributes.Hidden | FileAttributes.Archive,
                FileAttributes.Archive,
                FileAttributes.Normal,
                FileAttributes.NotContentIndexed | FileAttributes.ReadOnly,
                FileAttributes.NotContentIndexed | FileAttributes.System,
                FileAttributes.NotContentIndexed | FileAttributes.Hidden,
                FileAttributes.NotContentIndexed | FileAttributes.Archive,
                FileAttributes.Temporary,
                FileAttributes.Temporary | FileAttributes.Archive,
            };
            int fileCount = attributeCombos.Length;

            _output.WriteLine("============\nZipping.");
            using (ZipFile zip = new ZipFile())
            {
                for (int i = 0; i < fileCount; i++)
                {
                    // use the local filename (not fully qualified)
                    ZipEntry e = zip.AddEntry("file" + i.ToString(),
                                              "FileContent: This file has these attributes: " + attributeCombos[i].ToString());
                    _output.WriteLine("Adding {0}    [{1}]", e.FileName, attributeCombos[i].ToString());
                    e.Attributes = attributeCombos[i];
                }
                zip.Save(zipFileToCreate);
            }

            int entries = 0;
            string unpackDir = Path.Combine(TopLevelDir, String.Format("unpack-{0}", marker));
            _output.WriteLine("============\nExtracting.");
            using (ZipFile z2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in z2)
                {
                    _output.WriteLine("Extracting {0}", e.FileName);
                    Assert.Equal<FileAttributes>
                        (attributeCombos[entries], e.Attributes,
                            String.Format("unexpected attributes value in the entry {0} 0x{1:X4}",
                         e.FileName,
                                (int)e.Attributes));
                    entries++;
                    e.Extract(unpackDir);

                    // now verify that the attributes are set correctly in the filesystem
                    var attrs = File.GetAttributes(Path.Combine(unpackDir, e.FileName));
                    Assert.Equal<FileAttributes>
                        (e.Attributes, attrs,
                            String.Format("Unexpected attributes on the extracted filesystem file {0}.",
                                e.FileName));
                }
            }
            Assert.Equal<int>(fileCount, entries, "Unexpected file count.");
        }


        [Fact]
        //[Timeout(1000 * 240)]  // timeout in ms.  240s = 4 mins
        public void CreateZip_VerifyFileLastModified()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_VerifyFileLastModified.zip");
            string envTemp = Environment.GetEnvironmentVariable("TEMP");
            String[] candidateFileNames = Directory.GetFiles(envTemp);
            var checksums = new Dictionary<string, byte[]>();
            var timestamps = new Dictionary<string, DateTime>();
            var actualFilenames = new List<string>();
            var excludedFilenames = new List<string>();
            _output.WriteLine("\n-----------------------------\n: Found {0} files in '{1}'...",
                candidateFileNames.Length, envTemp);

            int maxFiles = _rnd.Next(candidateFileNames.Length / 2) + candidateFileNames.Length / 3;
            maxFiles = Math.Min(maxFiles, 145);
            //maxFiles = Math.Min(maxFiles, 15);
            _output.WriteLine("\n{0}: Selecting {1} files...", DateTime.Now.ToString("HH:mm:ss"), maxFiles);

            do
            {
                string filename = null;
                bool foundOne = false;
                while (!foundOne)
                {
                    filename = candidateFileNames[_rnd.Next(candidateFileNames.Length)];
                    if (excludedFilenames.Contains(filename)) continue;
                    var fi = new FileInfo(filename);

                    if (Path.GetFileName(filename)[0] == '~'
                        || actualFilenames.Contains(filename)
                        || fi.Length > 10000000
                        || Path.GetFileName(filename) == "dd_BITS.log"
                        // There WERE some weird files on my system that cause this
                        // test to fail!  the GetLastWrite() method returns the
                        // "wrong" time - does not agree with what is shown in
                        // Explorer or in a cmd.exe dir output.  So I exclude those
                        // files here.  (This is no longer a problem?)

                        //|| filename.EndsWith(".cer")
                        //|| filename.EndsWith(".msrcincident")
                        //|| filename == "MSCERTS.ini"
                        )
                    {
                        excludedFilenames.Add(filename);
                    }
                    else
                    {
                        foundOne = true;
                    }
                }

                var key = Path.GetFileName(filename);

                // surround this in a try...catch so as to avoid grabbing a file that is open by someone else, or has disappeared
                try
                {
                    var lastWrite = File.GetLastWriteTime(filename);
                    var fi = new FileInfo(filename);

                    // Rounding to nearest even second was necessary when DotNetZip did
                    // not process NTFS times in the NTFS Extra field. Since v1.8.0.5,
                    // this is no longer the case.
                    //
                    // var tm = TestUtilities.RoundToEvenSecond(lastWrite);

                    var tm = lastWrite;
                    // hop out of the try block if the file is from TODAY.  (heuristic
                    // to avoid currently open files)
                    if ((tm.Year == DateTime.Now.Year) && (tm.Month == DateTime.Now.Month) && (tm.Day == DateTime.Now.Day))
                        throw new Exception();
                    var chk = TestUtilities.ComputeChecksum(filename);
                    checksums.Add(key, chk);
                    _output.WriteLine("  {4}:  {1}  {2}  {3,-9}  {0}",
                                          Path.GetFileName(filename),
                                          lastWrite.ToString("yyyy MMM dd HH:mm:ss"),
                                          tm.ToString("yyyy MMM dd HH:mm:ss"),
                                          fi.Length,
                                          DateTime.Now.ToString("HH:mm:ss"));
                    timestamps.Add(key, this.AdjustTime_Win32ToDotNet(tm));
                    actualFilenames.Add(filename);
                }
                catch
                {
                    excludedFilenames.Add(filename);
                }
            } while ((actualFilenames.Count < maxFiles) && (actualFilenames.Count < candidateFileNames.Length) &&
                     actualFilenames.Count + excludedFilenames.Count < candidateFileNames.Length);

            _output.WriteLine("{0}: Creating zip...", DateTime.Now.ToString("HH:mm:ss"));

            // create the zip file
            using (ZipFile zip = new ZipFile())
            {
                foreach (string s in actualFilenames)
                {
                    ZipEntry e = zip.AddFile(s, "");
                    e.Comment = File.GetLastWriteTime(s).ToString("yyyyMMMdd HH:mm:ss");
                }
                zip.Comment = "The files in this archive will be checked for LastMod timestamp and checksum.";
                _output.WriteLine("{0}: Saving zip....", DateTime.Now.ToString("HH:mm:ss"));
                zip.Save(zipFileToCreate);
            }

            _output.WriteLine("{0}: Unpacking zip....", DateTime.Now.ToString("HH:mm:ss"));

            // unpack the zip, and verify contents
            int entries = 0;
            string unpackDir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "unpack"));
            using (ZipFile z2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in z2)
                {
                    _output.WriteLine("{0}: Checking entry {1}....", DateTime.Now.ToString("HH:mm:ss"), e.FileName);
                    entries++;
                    // verify that the LastMod time on the filesystem file is set correctly
                    e.Extract(unpackDir);
                    string pathToExtractedFile = Path.Combine(unpackDir, e.FileName);
                    DateTime actualFilesystemLastMod = AdjustTime_Win32ToDotNet(File.GetLastWriteTime(pathToExtractedFile));
                    TimeSpan delta = timestamps[e.FileName] - actualFilesystemLastMod;

                    // get the delta as an absolute value:
                    if (delta < new TimeSpan(0, 0, 0))
                        delta = new TimeSpan(0, 0, 0) - delta;

                    _output.WriteLine("time delta: {0}", delta.ToString());
                    // The time delta can be at most, 1 second.
                    Assert.True(delta < new TimeSpan(0, 0, 1),
                        String.Format("Unexpected LastMod timestamp on extracted filesystem file ({0}) expected({1}) actual({2})  delta({3}).",
                                  pathToExtractedFile,
                                  timestamps[e.FileName].ToString("F"),
                                  actualFilesystemLastMod.ToString("F"),
                                  delta.ToString()
                        ));

                    // verify the checksum of the file is correct
                    string expectedCheckString = TestUtilities.CheckSumToString(checksums[e.FileName]);
                    string actualCheckString = TestUtilities.GetCheckSumString(pathToExtractedFile);
                    Assert.Equal<String>
                        (expectedCheckString,
                         actualCheckString,
                            String.Format("Unexpected checksum on extracted filesystem file ({0}).",
                                pathToExtractedFile));
                }
            }
            Assert.Equal<int>(entries, actualFilenames.Count, "Unexpected file count.");
        }


        private DateTime AdjustTime_Win32ToDotNet(DateTime time)
        {
            // If I read a time from a file with GetLastWriteTime() (etc), I need
            // to adjust it for display in the .NET environment.
            if (time.Kind == DateTimeKind.Utc) return time;
            DateTime adjusted = time;
            if (DateTime.Now.IsDaylightSavingTime() && !time.IsDaylightSavingTime())
                adjusted = time + new System.TimeSpan(1, 0, 0);

            else if (!DateTime.Now.IsDaylightSavingTime() && time.IsDaylightSavingTime())
                adjusted = time - new System.TimeSpan(1, 0, 0);

            return adjusted;
        }


        [Fact]
        public void CreateZip_AddDirectory_NoFilesInRoot()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_NoFilesInRoot.zip");
            string marker = TestUtilities.GetMarker();
            string zipThis = Path.Combine(TopLevelDir, String.Format("adddir-no-files-in-root-{0}",marker));
            Directory.CreateDirectory(zipThis);

            int i, j;
            int entries = 0;

            int subdirCount = _rnd.Next(4) + 4;
            _output.WriteLine("creating {0} subdirectories", subdirCount);
            for (i = 0; i < subdirCount; i++)
            {
                string subdir = Path.Combine(zipThis, "DirectoryToZip.test." + i);
                Directory.CreateDirectory(subdir);
                _output.WriteLine("  created {0}", subdir);

                int fileCount = _rnd.Next(3) + 3;
                _output.WriteLine("  creating {0} files there", fileCount);
                for (j = 0; j < fileCount; j++)
                {
                    String file = Path.Combine(subdir, "file" + j);
                    TestUtilities.CreateAndFillFile(file, _rnd.Next(1000) + 1500);
                    entries++;
                }
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(zipThis, "");
                zip.Save(zipFileToCreate);
            }
            Assert.Equal<int>(CountEntries(zipFileToCreate), entries,
                                 "The Zip file has the wrong number of entries.");
        }


        [Fact]
        public void CreateZip_AddDirectory_OneCharOverrideName()
        {
            int entries = 0;
            String filename = null;

            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_OneCharOverrideName.zip");
            String commentOnArchive = "BasicTests::CreateZip_AddDirectory_OneCharOverrideName(): This archive overrides the name of a directory with a one-char name.";

            string subdir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "one-char-ovr-name"));
            Directory.CreateDirectory(subdir);

            int numFilesToCreate = _rnd.Next(23) + 14;
            var checksums = new Dictionary<string, string>();
            for (int j = 0; j < numFilesToCreate; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
                var chk = TestUtilities.ComputeChecksum(filename);

                var relativePath = Path.Combine(Path.GetFileName(subdir), Path.GetFileName(filename));
                //var key = Path.Combine("A", filename);
                var key = TestUtilities.TrimVolumeAndSwapSlashes(relativePath);
                checksums.Add(key, TestUtilities.CheckSumToString(chk));

                entries++;
            }

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(subdir, Path.GetFileName(subdir));
                zip1.Comment = commentOnArchive;
                zip1.Save(zipFileToCreate);
            }

            Assert.Equal<int>(entries, CountEntries(zipFileToCreate));

            // validate all the checksums
            string unpackDir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "unpack"));
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip2)
                {
                    e.Extract(unpackDir);
                    if (checksums.ContainsKey(e.FileName))
                    {
                        string pathToExtractedFile = Path.Combine(unpackDir, e.FileName);

                        // verify the checksum of the file is correct
                        string expectedCheckString = checksums[e.FileName];
                        string actualCheckString = TestUtilities.GetCheckSumString(pathToExtractedFile);
                        Assert.Equal<String>
                            (expectedCheckString,
                             actualCheckString,
                                String.Format("Unexpected checksum on extracted filesystem file ({0}).",
                                    pathToExtractedFile));
                    }
                }
            }

        }


        [Fact]
        public void CreateZip_CompressionLevelZero_AllEntries()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CompressionLevelZero.zip");
            String commentOnArchive = "BasicTests::CompressionLevelZero(): This archive override the name of a directory with a one-char name.";
            int entriesAdded = 0;
            String filename = null;
            string subdir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "zero-compression"));
            Directory.CreateDirectory(subdir);

            int fileCount = _rnd.Next(10) + 10;
            for (int j = 0; j < fileCount; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
                zip.AddDirectory(subdir, Path.GetFileName(subdir));
                zip.Comment = commentOnArchive;
                zip.Save(zipFileToCreate);
            }

            int entriesFound = 0;
            using (ZipFile zip = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    if (!e.IsDirectory) entriesFound++;
                    Assert.Equal<short>(0, (short)e.CompressionMethod,
                                           "compression method");
                }
            }
            Assert.Equal<int>(entriesAdded, entriesFound,
                                 "unexpected number of entries.");
            BasicVerifyZip(zipFileToCreate);
        }



        [Fact]
        public void CreateZip_ForceNoCompressionSomeEntries()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "ForceNoCompression.zip");
            String filename = null;
            string subdir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "force-no-compression-some"));
            Directory.CreateDirectory(subdir);
            int fileCount = _rnd.Next(13) + 13;
            for (int j = 0; j < fileCount; j++)
            {
                filename = Path.Combine(subdir, String.Format("{0}-file{1:D3}.txt", (_rnd.Next(2) == 0) ? "C":"U", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
            }

            bool someWithoutCompression = false;
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                    if (e.FileName.StartsWith("U"))
                    {
                        e.CompressionMethod = 0x0;
                        someWithoutCompression = true;
                    }
                }
                zip.Comment = "Some of these files do not use compression.";
                zip.Save(zipFileToCreate);
            }

            Assert.True(someWithoutCompression);
            int entriesFound = 0;
            using (ZipFile zip = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip)
                {
                    if (!e.IsDirectory) entriesFound++;
                    Assert.Equal<Int32>((e.FileName.StartsWith("U")) ? 0x00 : 0x08,
                                           (Int32)e.CompressionMethod,
                        String.Format("Unexpected compression method on text file ({0}).", e.FileName));
                }
            }
            Assert.Equal<int>(fileCount, entriesFound, "The created Zip file has an unexpected number of entries.");

            BasicVerifyZip(zipFileToCreate);
        }


        [Fact]
        public void AddFile_CompressionMethod_None_wi9208()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddFile_CompressionMethod_None_wi9208.zip");
            string subdir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "no-compress"));
            Directory.CreateDirectory(subdir);
            using (ZipFile zip = new ZipFile())
            {
                string filename = Path.Combine(subdir, String.Format("FileToBeAdded-{0:D2}.txt", _rnd.Next(1000)));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                var e = zip.AddFile(filename,"zipped");
                e.CompressionMethod = CompressionMethod.None;
                zip.Save(zipFileToCreate);
            }

            _output.WriteLine("File zipped!...");
            _output.WriteLine("Reading...");

            using (ZipFile zip = ZipFile.Read(zipFileToCreate))
            {
                foreach (var e in  zip)
                {
                    Assert.Equal<String>("None", e.CompressionMethod.ToString());
                }
            }
            BasicVerifyZip(zipFileToCreate);
        }



        [Fact]
        public void GetInfo()
        {
            _output.WriteLine("GetInfo");
            string zipFileToCreate = Path.Combine(TopLevelDir, "GetInfo.zip");
            String filename = null;
            int n;
            string subdir = Path.Combine(TopLevelDir, TestUtilities.GenerateRandomAsciiString(9));
            _output.WriteLine("creating subdir: {0}", subdir);
            Directory.CreateDirectory(subdir);

            int fileCount = _rnd.Next(27) + 23;
            _output.WriteLine("Creating {0} files", fileCount);
            bool someAreEmpty = false;
            for (int j = 0; j < fileCount; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                if (_rnd.Next(7)!=0)
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                else
                {
                    // create an empty file
                    using (var fs = File.Create(filename)) { }
                    someAreEmpty = true;
                }
            }

            Assert.True(someAreEmpty);
            _output.WriteLine("Creating a zip file");
            using (ZipFile zip = new ZipFile())
            {
                zip.Password = TestUtilities.GenerateRandomPassword(11);
                var filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                {
                    ZipEntry e = zip.AddFile(f, "");
                    if (_rnd.Next(3)==0)
                        e.CompressionMethod = 0x0;
                    n = _rnd.Next(crypto.Length);
                    e.Encryption = crypto[n];
                }
                zip.Save(zipFileToCreate);
            }

            _output.WriteLine("Calling ZipFile::Info_get");
            using (ZipFile zip = ZipFile.Read(zipFileToCreate))
            {
                string info = zip.Info;
                _output.WriteLine("Info: (len({0}))", info.Length);
                foreach (var line in info.Split('\n'))
                    _output.WriteLine(line);

                Assert.True(info.Length > 300,
                    String.Format("Suspect info string (length({0}))",
                        info.Length));
            }
        }



        //
        // This is a reasonable test but xUnit runs multiple tests in parallel, so
        // SetCurrentDirectory is not reliable. This needs to run standalone.
        //
        // [Fact]
        // public void Create_WithChangeDirectory()
        // {
        //     string zipFileToCreate = "Create_WithChangeDirectory.zip";
        //     string marker = TestUtilities.GetMarker();
        //     String filename = String.Format("Testfile-{0}.txt", marker);
        //     TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
        //
        //     //string cwd = Directory.GetCurrentDirectory();
        //     //_output.WriteLine("cwd: {0}", cwd);
        //     string nameOfFileToAdd = Path.Combine(TestUtilities.GetTestSrcDir(), "BasicTests.cs");
        //     using (var zip = new ZipFile())
        //     {
        //         zip.AddFile(filename, "");
        //         Directory.SetCurrentDirectory("\\");
        //         // use a relative path, remove the drive letter
        //         zip.AddFile(nameOfFileToAdd.Substring(3), "");
        //         Directory.SetCurrentDirectory(cwd);
        //         zip.Save(zipFileToCreate);
        //     }
        // }


        private void VerifyEntries(string zipFile,
                                   Variance variance,
                                   int[] values,
                                   EncryptionAlgorithm[] a,
                                   int stage,
                                   int compFlavor,
                                   int encryptionFlavor)
        {
            using (var zip = ZipFile.Read(zipFile))
            {
                foreach (ZipEntry e in zip)
                {
                    var compCheck = false;
                    if (variance == Variance.Method)
                    {
                        compCheck = (e.CompressionMethod == (CompressionMethod)values[stage]);
                    }
                    else
                    {
                        // Variance.Level
                        CompressionMethod expectedMethod =
                            ((Ionic.Zlib.CompressionLevel)values[stage] == Ionic.Zlib.CompressionLevel.None)
                            ? CompressionMethod.None
                            : CompressionMethod.Deflate;
                        compCheck = (e.CompressionMethod == expectedMethod);
                    }

                    Assert.True(compCheck,
                        String.Format("Unexpected compression method ({0}) on entry ({1}) variance({2}) flavor({3},{4}) stage({5})",
                                  e.CompressionMethod,
                                  e.FileName,
                                  variance,
                                  compFlavor, encryptionFlavor,
                                  stage
                        ));

                    var cryptoCheck = (e.Encryption == a[stage]);

                    Assert.True(cryptoCheck,
                        String.Format("Unexpected encryption ({0}) on entry ({1}) variance({2}) flavor({3},{4}) stage({5})",
                                  e.Encryption,
                                  e.FileName,
                                  variance,
                                  compFlavor,
                                  encryptionFlavor,
                                  stage
                        ));
                }
            }
        }



        private void QuickCreateZipAndChecksums(string zipFile,
                                                Variance variance,
                                                object compressionMethodOrLevel,
                                                EncryptionAlgorithm encryption,
                                                string password,
                                                out string[] files,
                                                out Dictionary<string, byte[]> checksums
                                                )
        {
            // zip up the source files for this test library
            files = Directory.GetFiles(TestUtilities.GetTestSrcDir(),
                "*.cs", SearchOption.TopDirectoryOnly);
            checksums = new Dictionary<string, byte[]>();
            foreach (string f in  files)
            {
                var chk = TestUtilities.ComputeChecksum(f);
                var key = Path.GetFileName(f);
                checksums.Add(key, chk);
            }

            using (var zip = new ZipFile())
            {
                if (variance == Variance.Level)
                {
                    zip.CompressionLevel= (Ionic.Zlib.CompressionLevel) compressionMethodOrLevel;
                }
                else
                {
                    if ((Ionic.Zip.CompressionMethod)compressionMethodOrLevel == CompressionMethod.None)
                        zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
                }

                if (password != null)
                {
                    zip.Password = password;
                    zip.Encryption = encryption;
                }
                zip.AddFiles(files, "");
                zip.Save(zipFile);
            }

            int count = CountEntries(zipFile);
            Assert.True(count > 4,
                String.Format("Unexpected number of entries ({0}) in the zip file.", count));
        }

        enum Variance
        {
            Method = 0 ,
            Level = 1
        }


        private string GeneratePassword()
        {
            return TestUtilities.GenerateRandomPassword();
            //return TestUtilities.GenerateRandomAsciiString(9);
            //return "Alphabet";
        }

        private void _Internal_Resave(string zipFile,
                                      Variance variance,
                                      int[] values,
                                      EncryptionAlgorithm[] cryptos,
                                      int compFlavor,
                                      int encryptionFlavor
                                      )
        {
            // Create a zip file, then re-save it with changes in compression methods,
            // compression levels, and/or encryption.  The methods/levels, cryptos are
            // for original and re-saved values. This tests whether we can update a zip
            // entry with changes in those properties.
            string[] passwords = new string[2];
            passwords[0]= (cryptos[0]==EncryptionAlgorithm.None) ? null : GeneratePassword();
            passwords[1]= passwords[0] ?? ((cryptos[1]==EncryptionAlgorithm.None) ? null : GeneratePassword());

            //_output.WriteLine("  crypto: '{0}'  '{1}'", crypto[0]??"-NONE-", passwords[1]??"-NONE-");
            _output.WriteLine("  crypto: '{0}'  '{1}'", cryptos[0], cryptos[1]);

            // first, create a zip file
            string[] filesToZip;
            Dictionary<string, byte[]> checksums;
            QuickCreateZipAndChecksums(zipFile, variance, values[0], cryptos[0], passwords[0], out filesToZip, out checksums);

            // check that the zip was constructed as expected
            VerifyEntries(zipFile, variance, values, cryptos, 0, compFlavor, encryptionFlavor);


            // modify some properties (CompressionLevel, CompressionMethod, and/or Encryption) on each entry
            using (var zip = ZipFile.Read(zipFile))
            {
                zip.Password = passwords[1];
                foreach (ZipEntry e in zip)
                {
                    if (variance == Variance.Method)
                        e.CompressionMethod = (CompressionMethod)values[1];
                    else
                        e.CompressionLevel = (Ionic.Zlib.CompressionLevel)values[1];

                    e.Encryption = cryptos[1];
                }
                zip.Save();
            }

            // Check that the zip was modified as expected
            VerifyEntries(zipFile, variance, values, cryptos, 1, compFlavor, encryptionFlavor);

            // now extract the items and verify checksums
            string extractDir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "ex"));
            // extract
            using (var zip = ZipFile.Read(zipFile))
            {
                zip.Password = passwords[1];
                zip.ExtractAll(extractDir);
            }

            VerifyChecksums(extractDir, filesToZip, checksums);
        }


        EncryptionAlgorithm[][] CryptoPairs = {
            new EncryptionAlgorithm[] {EncryptionAlgorithm.None, EncryptionAlgorithm.None},
            new EncryptionAlgorithm[] {EncryptionAlgorithm.None, EncryptionAlgorithm.WinZipAes128},
            new EncryptionAlgorithm[] {EncryptionAlgorithm.WinZipAes128, EncryptionAlgorithm.None},
            new EncryptionAlgorithm[] {EncryptionAlgorithm.WinZipAes128, EncryptionAlgorithm.WinZipAes128},
            new EncryptionAlgorithm[] {EncryptionAlgorithm.None, EncryptionAlgorithm.PkzipWeak},
            new EncryptionAlgorithm[] {EncryptionAlgorithm.PkzipWeak, EncryptionAlgorithm.None},
            new EncryptionAlgorithm[] {EncryptionAlgorithm.WinZipAes128, EncryptionAlgorithm.PkzipWeak},
            new EncryptionAlgorithm[] {EncryptionAlgorithm.PkzipWeak, EncryptionAlgorithm.WinZipAes128}
        };

        int[][][] VariancePairs = {
            new int[][] {
                new int[] {(int)CompressionMethod.Deflate, (int)CompressionMethod.Deflate},
                new int[] {(int)CompressionMethod.Deflate, (int)CompressionMethod.None},
                new int[] {(int)CompressionMethod.None, (int)CompressionMethod.Deflate},
                new int[] {(int)CompressionMethod.None, (int)CompressionMethod.None}
            },
            new int[][] {
                new int[] {(int)Ionic.Zlib.CompressionLevel.Default, (int)Ionic.Zlib.CompressionLevel.Default},
                new int[] {(int)Ionic.Zlib.CompressionLevel.Default, (int)Ionic.Zlib.CompressionLevel.None},
                new int[] {(int)Ionic.Zlib.CompressionLevel.None, (int)Ionic.Zlib.CompressionLevel.Default},
                new int[] {(int)Ionic.Zlib.CompressionLevel.None, (int)Ionic.Zlib.CompressionLevel.None}
            }
        };

        private void _Internal_Resave(Variance variance, int compFlavor, int encryptionFlavor)
        {
            // Check that re-saving a zip, after modifying properties on
            // each entry, actually does what we want.
            if (encryptionFlavor == 0)
                _output.WriteLine("Resave workdir: {0}", TopLevelDir);

            string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Resave_Compression{0}_{1}_Encryption_{2}.zip",
                                            variance, compFlavor, encryptionFlavor));
            int[] values = VariancePairs[(int)variance][compFlavor];

            _output.WriteLine("Resave {0} {1} {2} file({3})", variance, compFlavor, encryptionFlavor, Path.GetFileName(zipFileToCreate));

            _Internal_Resave(zipFileToCreate, variance, values, CryptoPairs[encryptionFlavor], compFlavor, encryptionFlavor);
        }


        [Fact]
        public void Resave_CompressionMethod_0()
        {
            for (int i=0; i<8;  i++)
            {
                _Internal_Resave(Variance.Method, 0, i);
            }
        }


        [Fact]
        public void Resave_CompressionMethod_1()
        {
            for (int i=0; i<8;  i++)
            {
                if (i!=3)
                    _Internal_Resave(Variance.Method, 1, i);
            }
        }

        [Fact]
        public void Resave_CompressionMethod_2()
        {
            for (int i=0; i<8;  i++)
            {
                _Internal_Resave(Variance.Method, 2, i);
            }
        }

        [Fact]
        public void Resave_CompressionMethod_3()
        {
            for (int i=0; i<8;  i++)
            {
                _Internal_Resave(Variance.Method, 3, i);
            }
        }

        [Fact]
        public void Resave_CompressionLevel_0()
        {
            for (int i=0; i<8;  i++)
            {
                _Internal_Resave(Variance.Level, 0, i);
            }
        }

        [Fact]
        public void Resave_CompressionLevel_1()
        {
            for (int i=0; i<8;  i++)
            {
                if (i!=3)
                    _Internal_Resave(Variance.Level, 1, i);
            }
        }

        [Fact]
        public void Resave_CompressionLevel_2()
        {
            for (int i=0; i<8;  i++)
            {
                _Internal_Resave(Variance.Level, 2, i);
            }
        }

        [Fact]
        public void Resave_CompressionLevel_3()
        {
            for (int i=0; i<8;  i++)
            {
                _Internal_Resave(Variance.Level, 3, i);
            }
        }

    }
}
