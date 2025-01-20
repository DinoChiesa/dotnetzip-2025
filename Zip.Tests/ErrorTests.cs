// ErrorTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011, 2025 Dino Chiesa 
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// This module defines some "error tests" - tests that the expected
// errors or exceptions occur in DotNetZip under exceptional conditions.
// These conditions include corrupted zip files, bad input, and so on.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Apache 2.0 License.
// See the file LICENSE.txt that accompanies the source code, for the license details.
//
// ------------------------------------------------------------------

using System.Text;
using Ionic.Zip.Tests.Utilities;
using Xunit.Abstractions;
using Assert = XunitAssertMessages.AssertM;

namespace Ionic.Zip.Tests.Error
{
    public class ErrorTests : IonicTestClass
    {
        public ErrorTests(ITestOutputHelper output) 
        {
            _output = output;
        }

        [Fact]
        public void AddFile_NonExistentFile()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddFile_NonExistentFile.zip");
            Assert.Throws<System.IO.FileNotFoundException>(() => {            
            using (Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile(zipFileToCreate))
            {
                zip.AddFile("ThisFileDoesNotExist.txt");
                zip.Comment = "This is a Comment On the Archive";
                zip.Save();
            }
            });
        }

        [Fact]
        public void Read_NullStream()
        {
            System.IO.Stream s = null;
            Assert.Throws<ArgumentNullException>(() => {
            using (var zip = ZipFile.Read(s))
            {
                foreach (var e in zip)
                {
                    Console.WriteLine("entry: {0}", e.FileName);
                }
            }
            });
        }


        [Fact]
        public void CreateZip_AddDirectory_BlankName()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddDirectory_BlankName.zip");
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            using (ZipFile zip = new ZipFile(zipFileToCreate))
            {
                zip.AddDirectoryByName("");
                zip.Save();
            }
            });
        }

        [Fact]
        public void CreateZip_AddEntry_String_BlankName()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CreateZip_AddEntry_String_BlankName.zip");
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            using (ZipFile zip = new ZipFile())
            {
                zip.AddEntry("", "This is the content.");
                zip.Save(zipFileToCreate);
            }
            });
        }

        private void OverwriteDecider(object sender, ExtractProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Extracting_ExtractEntryWouldOverwrite:
                    // Randomly choose whether to overwrite or not.
                    // In either case, no error is thrown.
                    e.CurrentEntry.ExtractExistingFile = (_rnd.Next(2) == 0)
                        ? ExtractExistingFileAction.DoNotOverwrite
                        : ExtractExistingFileAction.OverwriteSilently;
                    break;
            }
        }

        private void _Internal_ExtractExisting(int flavor)
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate =
                Path.Combine(tld,
                    String.Format("Extract-ExistingFile-f{0}-{1}.zip", flavor, marker));

            string dataDir = Path.Combine(TestUtilities.GetTestSrcDir(), "data");
            Assert.True(Directory.Exists(dataDir));
            var filenames = Directory.GetFiles(dataDir);

            using (ZipFile zip = new ZipFile(zipFileToCreate))
            {
                zip.AddFiles(filenames, "");
                zip.Comment = "This is a Comment On the Archive";
                zip.Save();
            }

            Assert.Equal<int>(CountEntries(zipFileToCreate), filenames.Length,
                                 "The zip file created has the wrong number of entries.");

            // Extract twice: the first time should succeed.
            // The second, should sometimes fail, because of a failed file overwrite.
            // 
            // If flavor==3, no fail. we silently overwrite.
            // If flavor==2, no fail. we silently do not overwrite.
            string unpackDir = Path.Combine(tld, String.Format("unpack-{0}", marker));
            for (int k = 0; k < 2; k++)
            {
                using (ZipFile zip = ZipFile.Read(zipFileToCreate))
                {
                    if (flavor > 10)
                    {
                        zip.ExtractProgress += OverwriteDecider;
                    }
                    for (int j = 0; j < filenames.Length; j++)
                    {
                        ZipEntry e = zip[Path.GetFileName(filenames[j])];
                        if (flavor == 4)
                            e.Extract(unpackDir);
                        else
                            e.Extract(unpackDir, (ExtractExistingFileAction)(flavor % 10));
                    }
                }
            }
        }


        [Fact]
        public void Extract_ExistingFileWithoutOverwrite_Throw()
        {
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            _Internal_ExtractExisting((int)ExtractExistingFileAction.Throw);
            });
        }


        [Fact]
        public void Extract_ExistingFileWithoutOverwrite_NoArg()
        {
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            _Internal_ExtractExisting(4);
            });
        }


        [Fact]
        public void Extract_ExistingFileWithOverwrite_OverwriteSilently()
        {
            _Internal_ExtractExisting((int)ExtractExistingFileAction.OverwriteSilently);
        }

        [Fact]
        public void Extract_ExistingFileWithOverwrite_DoNotOverwrite()
        {
            _Internal_ExtractExisting((int)ExtractExistingFileAction.DoNotOverwrite);
        }

        [Fact]
        public void Extract_ExistingFileWithoutOverwrite_InvokeProgress()
        {
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            _Internal_ExtractExisting((int)ExtractExistingFileAction.InvokeExtractProgressEvent);
            });
        }

        [Fact]
        public void Extract_ExistingFileWithoutOverwrite_InvokeProgress_2()
        {
            // The "decider" will either overwrite or not, but it does so silently.
            _Internal_ExtractExisting(10+(int)ExtractExistingFileAction.InvokeExtractProgressEvent);
        }


        [Fact]
        public void Extract_ExistingFileWithoutOverwrite_7()
        {
            // this is a test of the test!
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            _Internal_ExtractExisting(7);
            });
        }


        [Fact]
        public void EmptySplitZip()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(tld, String.Format("zftc-{0}.zip", marker));
            using (var zip = new ZipFile())
            {
                zip.MaxOutputSegmentSize = 1024*1024;
                zip.Save(zipFileToCreate);
            }

            string extractDir = Path.Combine(tld, TestUtilities.UniqueDir("extract"));
            using (var zip = ZipFile.Read(zipFileToCreate))
            {
                zip.ExtractAll(extractDir);
                Assert.True(zip.Entries.Count == 0);
            }
        }


        [Fact]
        public void Read_InvalidZip()
        {
            string filename = gzip;
            // try reading the invalid zipfile - this must fail.
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            using (ZipFile zip = ZipFile.Read(filename))
            {
                foreach (ZipEntry e in zip)
                {
                    System.Console.WriteLine("name: {0}  compressed: {1} has password?: {2}",
                        e.FileName, e.CompressedSize, e.UsesEncryption);
                }
            }
            });
        }


        [Fact]
        public void NonZipFile_wi11743()
        {
            // try reading an empty, extant file as a zip file
            string tld = new String(TopLevelDir); // copy to avoid changes
            string emptyFile = Path.GetTempFileName();
            string zipFileToRead = Path.Combine(tld, TestUtilities.UniqueDir("empty"));
            File.Move(emptyFile, zipFileToRead);
            
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            using (ZipFile zip = new ZipFile(zipFileToRead))
            {
                zip.AddEntry("EntryName1.txt", "This is the content");
                zip.Save();
            }
            });
        }


        private void CreateSmallZip(string zipFileToCreate)
        {
            string sourceDir = TestUtilities.GetTestSrcDir();

            // the list of filenames to add to the zip
            string[] filenames =
            {
                Path.Combine(sourceDir, "..\\Tools\\GZip\\bin\\Debug\\net9.0\\GZip.exe"),
                Path.Combine(sourceDir, "..\\LICENSE.txt"),
                Path.Combine(sourceDir, "..\\CommonTestSrc\\TestUtilities.cs"),
            };

            using (ZipFile zip = new ZipFile())
            {
                for (int j = 0; j < filenames.Length; j++)
                    zip.AddFile(filenames[j], "");
                zip.Save(zipFileToCreate);
            }

            Assert.Equal<int>(filenames.Length, CountEntries(zipFileToCreate),
                                 "Wrong number of entries.");
        }

        [Fact]
        public void MalformedZip()
        {
            string emptyFile = Path.GetTempFileName();
            string tld = new String(TopLevelDir); // copy to avoid changes
            string zipFileToRead = Path.Combine(tld, TestUtilities.UniqueDir("malformed"));
            File.Move(emptyFile, zipFileToRead);
            File.WriteAllText( zipFileToRead , "this-is-malformed-asdfasdf" );
            string extractDirectory = Path.Combine(tld, TestUtilities.UniqueDir("extract-malformed"));
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            using ( ZipFile zipFile = ZipFile.Read( zipFileToRead ) )
            {
                zipFile.ExtractAll( extractDirectory  );
            }
            });
        }


        [Fact]
        public void UseZipEntryExtractWith_ZIS_wi10355()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(tld, String.Format("UseOpenReaderWith_ZIS-{0}.zip", marker));
            CreateSmallZip(zipFileToCreate);

            // mixing ZipEntry.Extract and ZipInputStream is a no-no!!

            string extractDir = Path.Combine(tld, TestUtilities.UniqueDir("extract"));

            // Use ZipEntry.Extract with ZipInputStream.
            // This must fail.
            _output.WriteLine("Reading with ZipInputStream");

            Assert.Throws<InvalidOperationException>(() => {            
            using (var zip = new ZipInputStream(zipFileToCreate))
            {
                ZipEntry entry;
                while ((entry = zip.GetNextEntry()) != null)
                {
                    entry.Extract(extractDir, Ionic.Zip.ExtractExistingFileAction.OverwriteSilently);
                }
            }
            });
        }


        [Fact]
        public void UseOpenReaderWith_ZIS_wi10923()
        {
            string marker = TestUtilities.GetMarker();
            string tld = new String(TopLevelDir); // copy to avoid changes
            string zipFileToCreate = Path.Combine(tld, String.Format("UseOpenReaderWith_ZIS-{0}.zip", marker));
            CreateSmallZip(zipFileToCreate);

            // mixing OpenReader and ZipInputStream is a no-no!!
            int n;
            var buffer = new byte[2048];

            // Use OpenReader with ZipInputStream.
            // This must fail.
            _output.WriteLine("Reading with ZipInputStream");
            Assert.Throws<InvalidOperationException>(() => {            
            using (var zip = new ZipInputStream(zipFileToCreate))
            {
                ZipEntry entry;
                while ((entry = zip.GetNextEntry()) != null)
                {
                    _output.WriteLine("  Entry: {0}", entry.FileName);
                    using (Stream file = entry.OpenReader())
                    {
                        while((n= file.Read(buffer,0,buffer.Length)) > 0) ;
                    }
                    _output.WriteLine("  -- OpenReader() is done. ");
                }
            }
            });
        }


        [Fact]
        public void Save_InvalidLocation()
        {
            string not_a_file_name = "c:\\Windows\\";
            Assert.True(Directory.Exists(not_a_file_name));

            // Add an entry to the zipfile, then try saving to a directory.
            // This must fail.
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            using (ZipFile zip = new ZipFile())
            {
                zip.AddEntry("This is a file.txt", "Content for the file goes here.");
                zip.Save(not_a_file_name);  // fail
            }
            });
        }


        [Fact]
        public void Save_NonExistentFile()
        {
            int j;
            string repeatedLine;
            string filename;
            string marker = TestUtilities.GetMarker();
            string tld = new String(TopLevelDir); // copy to avoid changes
            string zipFileToCreate = Path.Combine(tld, "Save_NonExistentFile.zip");

            // create the subdirectory
            string subdir = Path.Combine(tld, "DirToZip");
            Directory.CreateDirectory(subdir);

            int entriesAdded = 0;
            // create the files
            int numFilesToCreate = _rnd.Next(20) + 18;
            for (j = 0; j < numFilesToCreate; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                    Path.GetFileName(filename));
                TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(1800) + 1500);
                entriesAdded++;
            }

            string tempFileFolder = Path.Combine(tld, String.Format("Temp-{0}", marker));
            Directory.CreateDirectory(tempFileFolder);
            _output.WriteLine("Using {0} as the temp file folder....", tempFileFolder);
            String[] tfiles = Directory.GetFiles(tempFileFolder);
            int nTemp = tfiles.Length;
            _output.WriteLine("There are {0} files in the temp file folder.", nTemp);
            String[] filenames = Directory.GetFiles(subdir);

            var a1 = System.Reflection.Assembly.GetExecutingAssembly();
            String myName = a1.GetName().ToString();
            string toDay = System.DateTime.Now.ToString("yyyy-MMM-dd");

            try
            {
                using (ZipFile zip = new ZipFile(zipFileToCreate))
                {
                    zip.TempFileFolder = tempFileFolder;
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;

                    _output.WriteLine("Zipping {0} files...", filenames.Length);

                    int count = 0;
                    foreach (string fn in filenames)
                    {
                        count++;
                        _output.WriteLine("  {0}", fn);

                        string file = fn;

                        if (count == filenames.Length - 2)
                        {
                            file += "xx";
                            _output.WriteLine("(Injecting a failure...)");
                        }

                        zip.UpdateFile(file, myName + '-' + toDay + "_done");
                    }
                    _output.WriteLine("\n");
                    zip.Save();
                    _output.WriteLine("Zip Completed '{0}'", zipFileToCreate);
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine("Zip Failed (EXPECTED): {0}", ex.Message);
            }

            tfiles = Directory.GetFiles(tempFileFolder);

            Assert.Equal<int>(nTemp, tfiles.Length,
                    "There are unexpected files remaining in the TempFileFolder.");
        }


        [Fact]
        public void Save_NoFilename()
        {
            string dataDir = Path.Combine(TestUtilities.GetTestSrcDir(), "data");
            string filename = Path.Combine(dataDir, "TestStrings.txt");
            Assert.True(File.Exists(filename), String.Format("The file '{0}' doesnot exist.", filename));

            // add an entry to the zipfile, then try saving, never having specified a filename. This should fail.
            Assert.Throws<Ionic.Zip.BadStateException>(() => {            
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(filename, "");
                zip.Save(); // FAIL: don't know where to save!
            }
            });
        }


        [Fact]
        public void Extract_WithoutSave()
        {
            string dataDir = Path.Combine(TestUtilities.GetTestSrcDir(), "data");
            // add a directory to the zipfile, then try
            // extracting, without a Save. This should fail.
            Assert.Throws<Ionic.Zip.BadStateException>(() => {            
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(dataDir, "");
                Assert.True(zip.Entries.Count > 0);
                zip[0].Extract();  // FAIL; the zip has not been saved
                }
            });
        }

        [Fact]
        public void Read_WithoutSave()
        {
            string dataDir = Path.Combine(TestUtilities.GetTestSrcDir(), "data");

            // add a directory to the zipfile, then try
            // extracting, without a Save. This should fail.
            Assert.Throws<Ionic.Zip.BadStateException>(() => {            
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(dataDir, "");
                Assert.True(zip.Entries.Count > 0);

                using (var s = zip[0].OpenReader()) // FAIL: has not been saved
                {
                    byte[] buffer= new byte[1024];
                    int n;
                    while ((n= s.Read(buffer,0,buffer.Length)) > 0) ;
                }
            }
            });
        }

        [Fact]
        public void AddDirectory_SpecifyingFile()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string zipFileToCreate = Path.Combine(tld, "AddDirectory_SpecifyingFile.zip");
            string filename = Path.Combine(tld, String.Format("file-to-zip-{0}", Path.GetRandomFileName()));
            Assert.False(File.Exists(filename));
            Assert.NotNull(gzip);
            File.Copy(gzip, filename);
            Assert.True(File.Exists(filename));
            Assert.Throws<System.IO.IOException>(() => {            
            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(filename); // FAIL - it is a file
                zip.Save(zipFileToCreate);
            }
            });
        }


        [Fact]
        public void AddFile_SpecifyingDirectory()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string zipFileToCreate = Path.Combine(tld, "AddFile_SpecifyingDirectory.zip");
            string dirname = Path.Combine(tld, String.Format("ThisIsADirectory-{0}", Path.GetRandomFileName()));
            Directory.CreateDirectory(dirname);
            Assert.True(Directory.Exists(dirname));
            Assert.Throws<FileNotFoundException>(() => {            
            using (ZipFile zip = new ZipFile())
            {
                zip.AddFile(dirname); // should fail - it is a directory
                zip.Save(zipFileToCreate);
            }
            });
        }

        private void IntroduceCorruption(string filename)
        {
            // now corrupt the zip archive
            using (FileStream fs = File.OpenWrite(filename))
            {
                byte[] corruption = new byte[_rnd.Next(100) + 12];
                int min = 5;
                int max = (int)fs.Length - 20;
                int offsetForCorruption, lengthOfCorruption;

                int numCorruptions = _rnd.Next(2) + 2;
                for (int i = 0; i < numCorruptions; i++)
                {
                    _rnd.NextBytes(corruption);
                    offsetForCorruption = _rnd.Next(min, max);
                    lengthOfCorruption = _rnd.Next(2) + 3;
                    fs.Seek(offsetForCorruption, SeekOrigin.Begin);
                    fs.Write(corruption, 0, lengthOfCorruption);
                }
            }
        }


        [Fact]
        public void ReadCorruptedZipFile_Passwords()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Read_CorruptedZipFile_Passwords.zip");
            string sourceDir = TestUtilities.GetTestSrcDir();

            // the list of filenames to add to the zip
            string[] filenames =
            {
                Path.Combine(sourceDir, "..\\Tools\\GZip\\bin\\Debug\\net9.0\\GZip.exe"),
                Path.Combine(sourceDir, "..\\LICENSE.txt"),
                Path.Combine(sourceDir, "..\\CommonTestSrc\\TestUtilities.cs"),
            };

            // passwords to use for those entries
            string[] passwords = { "12345678", "0987654321", };

            // create the zipfile, adding the files
            int j = 0;
            using (ZipFile zip = new ZipFile())
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip.Password = passwords[j % passwords.Length];
                    zip.AddFile(filenames[j], "");
                }
                zip.Save(zipFileToCreate);
            }

            IntroduceCorruption(zipFileToCreate);

            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            try
            {
                string unpackDir = TestUtilities.UniqueDir("unpack");
                // read the corrupted zip - this should fail in some way
                using (ZipFile zip = ZipFile.Read(zipFileToCreate))
                {
                    for (j = 0; j < filenames.Length; j++)
                    {
                        ZipEntry e = zip[Path.GetFileName(filenames[j])];

                        System.Console.WriteLine("name: {0}  compressed: {1} has password?: {2}",
                            e.FileName, e.CompressedSize, e.UsesEncryption);
                        Assert.True(e.UsesEncryption, "The entry does not use encryption");
                        e.ExtractWithPassword(unpackDir, passwords[j % passwords.Length]);
                    }
                }
            }
            catch (Exception exc1)
            {
                throw new ZipException("expected", exc1);
            }
            });
        }


        [Fact]
        public void ReadCorruptedZipFile()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Read_CorruptedZipFile.zip");
            string sourceDir = TestUtilities.GetTestSrcDir();

            // the list of filenames to add to the zip
            string[] filenames =
            {
                Path.Combine(sourceDir, "..\\Tools\\GZip\\bin\\Debug\\net9.0\\GZip.exe"),
                Path.Combine(sourceDir, "data\\wi8647.tif"),
                Path.Combine(sourceDir, "..\\Zip\\bin\\Debug\\net9.0\\Zip.dll"),
                Path.Combine(sourceDir, "..\\CommonTestSrc\\TestUtilities.cs"),
            };

            for (int i = 0; i < filenames.Length; i++) {
                Assert.True(File.Exists(filenames[i]),
                    String.Format("The file '{0}' doesnot exist.", filenames[i]));
            }

            // create the zipfile, adding the files
            using (ZipFile zip = new ZipFile())
            {
                for (int i = 0; i < filenames.Length; i++)
                    zip.AddFile(filenames[i], "");
                zip.Save(zipFileToCreate);
            }

            // now corrupt the zip archive
            IntroduceCorruption(zipFileToCreate);

            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            try
            {
                string extractDir = TestUtilities.UniqueDir("extract");
                // read the corrupted zip - this should fail in some way
                using (ZipFile zip = new ZipFile(zipFileToCreate))
                {
                    foreach (var e in zip)
                    {
                        System.Console.WriteLine("name: {0}  compressed: {1} has password?: {2}",
                            e.FileName, e.CompressedSize, e.UsesEncryption);
                        e.Extract(extractDir);
                    }
                }
            }
            catch (Exception exc1)
            {
                // not sure which exception - could be one of several.
                throw new ZipException("expected", exc1);
            }
            });
        }


        [Fact]
        public void LockedFile_wi13903()
        {
            _output.WriteLine("==LockedFile_wi13903()");
            string tld = new String(TopLevelDir); // copy to avoid changes
            string fname = Path.Combine(tld, Path.GetRandomFileName());
            _output.WriteLine("create file {0}", fname);
            TestUtilities.CreateAndFillFileText(fname, _rnd.Next(10000) + 5000);
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(tld, String.Format("wi13903-{0}.zip", marker));

            var zipErrorHandler = new EventHandler<ZipErrorEventArgs>( (sender, e)  =>
                {
                    _output.WriteLine("Error reading entry {0}", e.CurrentEntry);
                    _output.WriteLine("  (this was expected)");
                    e.CurrentEntry.ZipErrorAction = ZipErrorAction.Skip;
                });

            // lock the file
            _output.WriteLine("lock file {0}", fname);
            using (var s = System.IO.File.Open(fname,
                                               FileMode.Open,
                                               FileAccess.Read,
                                               FileShare.None))
            {
                using (var rawOut = File.Create(zipFileToCreate))
                {
                    using (var nonSeekableOut = new Ionic.Zip.Tests.NonSeekableOutputStream(rawOut))
                    {
                        _output.WriteLine("create zip file {0}", zipFileToCreate);
                        using (var zip = new ZipFile())
                        {
                            zip.ZipError += zipErrorHandler;
                            zip.AddFile(fname, "");
                            // should trigger a read error,
                            // which should be skipped. Result will be
                            // a zero-entry zip file.
                            zip.Save(nonSeekableOut);
                        }
                    }
                }
            }
            _output.WriteLine("all done, A-OK");
        }

        [Fact]
        public void Read_EmptyZipFile()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string marker = TestUtilities.GetMarker();
            string zipFileToRead = Path.Combine(tld, "Read_BadFile.zip");
            File.Move(Path.GetTempFileName(), zipFileToRead);
            
            string fileToAdd = Path.Combine(tld, $"EmptyFile-{marker}.txt");
            File.Move(Path.GetTempFileName(), fileToAdd);

            Assert.Throws<Ionic.Zip.ZipException>(() => {            
                try
            {
                using (ZipFile zip = ZipFile.Read(zipFileToRead))
                {
                    zip.AddFile(fileToAdd, "");
                    zip.Save();
                }
            }
            catch (System.Exception exc1)
            {
                throw new ZipException("expected", exc1);
            }
            });
        }


        [Fact]
        public void AddFile_Twice()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            int i;
            // select the name of the zip file
            string zipFileToCreate = Path.Combine(tld, "AddFile_Twice.zip");

            string marker = TestUtilities.GetMarker();
            string subdir = Path.Combine(tld, $"addfile-twice-{marker}");
            Directory.CreateDirectory(subdir);

            // create a bunch of files
            int numFilesToCreate = _rnd.Next(23) + 14;
            for (i = 0; i < numFilesToCreate; i++)
                TestUtilities.CreateUniqueFile("bin", subdir, _rnd.Next(10000) + 5000);

            // Create the zip archive
            using (ZipFile zip1 = new ZipFile(zipFileToCreate))
            {
                zip1.StatusMessageTextWriter = System.Console.Out;
                string[] files = Directory.GetFiles(subdir);
                zip1.AddFiles(files, "files");
                zip1.Save();
            }

            Assert.Throws<ArgumentException>(() => {            
            // this should fail - adding the same file twice
            using (ZipFile zip2 = new ZipFile(zipFileToCreate))
            {
                zip2.StatusMessageTextWriter = System.Console.Out;
                string[] files = Directory.GetFiles(subdir);
                for (i = 0; i < files.Length; i++)
                    zip2.AddFile(files[i], "files");
                zip2.Save();
            }
            });
        }


        [Fact]
        public void FileNotAvailableFails()
        {
            // verify the correct exception is being thrown
            string zipFileToCreate = Path.Combine(TopLevelDir, "FileNotAvailableFails.zip");

            // create a zip file with no entries
            using (var zipfile = new ZipFile(zipFileToCreate)) { zipfile.Save(); }

            Assert.Throws<Ionic.Zip.ZipException>(() => {            
                // open and lock
                using (new FileStream(zipFileToCreate, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                using (new ZipFile(zipFileToCreate)) { }
            }
            });
        }

        [Fact]
        public void IncorrectZipContentTest1_wi10459()
        {
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
            byte[] content = Encoding.UTF8.GetBytes("wrong zipfile content");
            using (var ms = new MemoryStream(content))
            {
                using (var zipFile = ZipFile.Read(ms)) { }
            }
            });
        }

        [Fact]
        public void IncorrectZipContentTest2_wi10459()
        {
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
                using (var ms = new MemoryStream())
            {
                using (var zipFile = ZipFile.Read(ms)) { }
            }
            });
        }

        [Fact]
        public void IncorrectZipContentTest3_wi10459()
        {
            Assert.Throws<Ionic.Zip.ZipException>(() => {            
                byte[] content = new byte[8192];
                using (var ms = new MemoryStream(content))
            {
                using (var zipFile = ZipFile.Read(ms)) { }
            }
            });
        }

    }
}
