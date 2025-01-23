// UpdateTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011, 2025 Dino Chiesa
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// This module defines tests for updating zip files via DotNetZip.
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

namespace Ionic.Zip.Tests.Update
{
    public class UpdateTests : IonicTestClass
    {
        public UpdateTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private int CreateDirAndSomeFiles(string marker, out String subdir)
        {
            int entriesAdded = 0;
            // create the subdirectory
            subdir = Path.Combine(TopLevelDir, $"A-{marker}");
            Directory.CreateDirectory(subdir);

            // create a bunch of files
            int numFilesToCreate = _rnd.Next(14) + 9;
            for (int j = 0; j < numFilesToCreate; j++)
            {
                String filename = String.Format("file{0:D3}.txt", j);
                String repeatedLine = $"This line is repeated over and over and over in file {filename}";
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }
            return entriesAdded;
        }


        [Fact]
        public void AddNewDirectory()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddNewDirectory.zip");
            String CommentOnArchive = "UpdateTests::AddNewDirectory(): This archive will be overwritten.";
            string newComment = "This comment has been OVERWRITTEN." + DateTime.Now.ToString("G");
            string dirToZip = Path.Combine(TopLevelDir, $"zipup-{marker}");

            int i, j;
            int numEntries = 0;
            string subdir = null;
            String filename = null;
            int subdirCount = _rnd.Next(4) + 4;
            for (i = 0; i < subdirCount; i++)
            {
                subdir = Path.Combine(dirToZip, "Directory." + i);
                Directory.CreateDirectory(subdir);

                int fileCount = _rnd.Next(3) + 3;
                for (j = 0; j < fileCount; j++)
                {
                    filename = Path.Combine(subdir, $"file-{j}.txt");
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
                    numEntries++;
                }
            }

            using (ZipFile zip = new ZipFile())
            {
                zip.AddDirectory(dirToZip);
                zip.Comment = CommentOnArchive;
                zip.Save(zipFileToCreate);
            }

            Assert.Equal<int>(numEntries, CountEntries(zipFileToCreate),
                    "The created Zip file has an unexpected number of entries.");

            BasicVerifyZip(zipFileToCreate);

            // Now create a new subdirectory and add that one
            subdir = Path.Combine(TopLevelDir, "NewSubDirectory");
            Directory.CreateDirectory(subdir);

            filename = Path.Combine(subdir, "newfile.txt");
            TestUtilities.CreateAndFillFileText(filename, _rnd.Next(12000) + 5000);
            numEntries++;

            using (ZipFile zip = new ZipFile(zipFileToCreate))
            {
                zip.AddDirectory(subdir);
                zip.Comment = newComment;
                // this will add entries into the existing zip file
                zip.Save();
            }

            Assert.Equal<int>(numEntries, CountEntries(zipFileToCreate),
                    "The overwritten Zip file has the wrong number of entries.");

            using (ZipFile readzip = new ZipFile(zipFileToCreate))
            {
                Assert.Equal<string>(newComment,
                                        readzip.Comment,
                                        "The zip comment is incorrect.");
            }
        }


        [Fact]
        public void ChangeMetadata_AES()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "ChangeMetadata_AES.zip");
            string subdir = Path.Combine(TopLevelDir, $"A-{marker}");
            Directory.CreateDirectory(subdir);

            // create the files
            int numFilesToCreate = _rnd.Next(13) + 24;
            //int numFilesToCreate = 2;
            string filename = null;
            for (int j = 0; j < numFilesToCreate; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                //TestUtilities.CreateAndFillFileText(filename, 500);
            }

            string password = Path.GetRandomFileName() + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

            using (var zip = new ZipFile())
            {
                zip.Password = password;
                zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip.AddFiles(Directory.GetFiles(subdir), "");
                zip.Save(zipFileToCreate);
            }

            // Verify the correct number of files are in the zip
            Assert.Equal<int>(numFilesToCreate, CountEntries(zipFileToCreate),
                                 "The updated Zip file has the wrong number of entries.");

            // test extract (and implicitly check CRCs, passwords, etc)
            VerifyZip(zipFileToCreate, password);

            byte[] buffer = new byte[_rnd.Next(10000) + 10000];
            _rnd.NextBytes(buffer);
            using (var zip = ZipFile.Read(zipFileToCreate))
            {
                // modify the metadata for an entry
                zip[0].LastModified = DateTime.Now - new TimeSpan(7 * 31, 0, 0);
                zip.Password = password;
                zip.Encryption = EncryptionAlgorithm.WinZipAes256;
                zip.AddEntry(Path.GetRandomFileName(), buffer);
                zip.Save();
            }

            // Verify the correct number of files are in the zip
            Assert.Equal<int>(numFilesToCreate + 1, CountEntries(zipFileToCreate),
                                 "The updated Zip file has the wrong number of entries.");

            // test extract (and implicitly check CRCs, passwords, etc)
            VerifyZip(zipFileToCreate, password);
        }


        private void VerifyZip(string zipfile, string password)
        {
            Stream bitBucket = Stream.Null;
            _output.WriteLine("Checking file {0}", zipfile);
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                zip.Password = password;
                zip.BufferSize = 65536;
                foreach (var s in zip.EntryFileNames)
                {
                    _output.WriteLine("  Entry: {0}", s);
                    zip[s].Extract(bitBucket);
                }
            }
            System.Threading.Thread.Sleep(0x500);
        }


        [Fact]
        public void RemoveEntry_ByLastModTime()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "RemoveEntry_ByLastModTime.zip");
            string subdir = Path.Combine(TopLevelDir, $"A-{marker}");
            Directory.CreateDirectory(subdir);

            // create the files
            int numFilesToCreate = _rnd.Next(13) + 24;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < numFilesToCreate; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            int ix = 0;
            System.DateTime origDate = new System.DateTime(2023, 1, 15, 12, 1, 0);
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                {
                    ZipEntry e = zip1.AddFile(f, "");
                    e.LastModified = origDate + new TimeSpan(24 * 31 * ix, 0, 0);  // 31 days * number of entries
                    ix++;
                }
                zip1.Comment = "UpdateTests::RemoveEntry_ByLastModTime(): This archive will soon be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            var threshold = new TimeSpan(24 * 31 * (2 + _rnd.Next(ix - 12)), 0, 0);
            int numRemoved = 0;
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                // We cannot remove the entry from the list, within the context of
                // an enumeration of said list.
                // So we add the doomed entry to a list to be removed
                // later.
                // pass 1: mark the entries for removal
                var entriesToRemove = new List<ZipEntry>();
                foreach (ZipEntry e in zip2)
                {
                    if (e.LastModified < origDate + threshold)
                    {
                        entriesToRemove.Add(e);
                        numRemoved++;
                    }
                }

                // pass 2: actually remove the entry.
                foreach (ZipEntry zombie in entriesToRemove)
                    zip2.RemoveEntry(zombie);

                zip2.Comment = "UpdateTests::RemoveEntry_ByLastModTime(): This archive has been updated.";
                zip2.Save();
            }

            // Verify the correct number of files are in the zip
            Assert.Equal<int>(entriesAdded - numRemoved, CountEntries(zipFileToCreate),
                "The updated Zip file has the wrong number of entries.");

            // verify that all entries in the archive are within the threshold
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                foreach (ZipEntry e in zip3)
                    Assert.True((e.LastModified >= origDate + threshold),
                        "The updated Zip file has entries that lie outside the threshold.");
            }
        }

        [Fact]
        public void RemoveEntry_ByFilename_WithPassword()
        {
            string marker = TestUtilities.GetMarker();
            string password = "*!ookahoo";
            string filename = null;
            string repeatedLine = null;
            int j;

            string zipFileToCreate = Path.Combine(TopLevelDir, "ByFilename_WithPassword.zip");
            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Add the files to the zip, save the zip
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                zip1.Password = password;
                zip1.AddFiles(filenames, "");

                zip1.Comment = "UpdateTests::RemoveEntry_ByFilename_WithPassword(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            var filesToRemove = new List<string>();
            int numToRemove = _rnd.Next(entriesAdded - 4) + 1;
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                for (j = 0; j < numToRemove; j++)
                {
                    // select a new, uniquely named file to create
                    do
                    {
                        filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                    } while (filesToRemove.Contains(filename));
                    // add this file to the list
                    filesToRemove.Add(filename);
                    zip2.RemoveEntry(filename);
                }
                zip2.Comment = "This archive has been modified. Some files have been removed.";
                zip2.Save();
            }


            // extract all files, verify the contents of the files that remain.
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s1 in zip3.EntryFileNames)
                {
                    Assert.False(filesToRemove.Contains(s1), $"File ({s1}) was not expected.");

                    zip3[s1].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), password);
                    repeatedLine = $"This line is repeated over and over and over in file {s1}";

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                }
            }

            // Verify the number of files remaining in the zip
            Assert.Equal<int>(entriesAdded - filesToRemove.Count, CountEntries(zipFileToCreate),
                "The updated Zip file has the wrong number of entries.");
        }


        [Fact]
        public void RenameEntry()
        {
            string marker = TestUtilities.GetMarker();
            string dirToZip = Path.Combine(TopLevelDir, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            var files = TestUtilities.GenerateFilesFlat(dirToZip,
                                                        _rnd.Next(13) + 24,
                                                        42 * 1024 + _rnd.Next(20000));

            // Two passes:  in pass 1, simply rename the file;
            // in pass 2, rename it so that it has a directory.
            // This shouldn't matter, but we test it anyway.
            for (int k = 0; k < 2; k++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, $"RenameEntry-{k}.zip");
                _output.WriteLine("-----------------------------");
                _output.WriteLine("{0}: Trial {1}, adding {2} files into '{3}'...",
                                      DateTime.Now.ToString("HH:mm:ss"),
                                      k,
                                      files.Length,
                                      zipFileToCreate);

                // Add the files to the zip, save the zip
                using (ZipFile zip1 = new ZipFile())
                {
                    foreach (String f in files)
                        zip1.AddFile(f, "");
                    zip1.Comment = "This archive will be updated.";
                    zip1.Save(zipFileToCreate);
                }

                // Verify the number of files in the zip
                Assert.Equal<int>(files.Length, CountEntries(zipFileToCreate),
                                     "the Zip file has the wrong number of entries.");

                // selectively rename a few files in the zip archive
                int renameCount = 0;
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    var toRename = new List<ZipEntry>();
                    while (toRename.Count < 2)
                    {
                        foreach (ZipEntry e in zip2)
                        {
                            if (_rnd.Next(2) == 1)
                                toRename.Add(e);
                        }
                    }

                    foreach (ZipEntry e in toRename)
                    {
                        var newname = (k == 0)
                            ? e.FileName + "-renamed"
                            : "renamed_files\\" + e.FileName;

                        _output.WriteLine("  renaming {0} to {1}", e.FileName, newname);
                        e.FileName = newname;
                        e.Comment = "renamed";
                        renameCount++;
                    }

                    zip2.Comment = String.Format("This archive has been modified. {0} files have been renamed.", renameCount);
                    zip2.Save();
                }

                // Extract all the files, verify that none have been removed,
                // and verify the names of the entries.
                int renameCount2 = 0;
                using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
                {
                    string extractDir = $"ex-{marker}-{k}";
                    foreach (string s1 in zip3.EntryFileNames)
                    {
                        zip3[s1].Extract(Path.Combine(TopLevelDir, extractDir));
                        string origFilename = Path.GetFileName((s1.Contains("renamed"))
                            ? s1.Replace("-renamed", "")
                            : s1);

                        if (zip3[s1].Comment == "renamed") renameCount2++;
                    }
                }

                Assert.Equal<int>(renameCount, renameCount2,
                    "The updated Zip file has the wrong number of renamed entries.");

                Assert.Equal<int>(files.Length, CountEntries(zipFileToCreate),
                    "Wrong number of entries.");
            }
        }


        [Fact]
        public void UpdateEntryComment()
        {
            string marker = TestUtilities.GetMarker();
            for (int k = 0; k < 4; k++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, $"UpdateEntryComment-{k}.zip");
                String subdir = null;
                int entriesAdded = CreateDirAndSomeFiles($"{marker}-{k}", out subdir);
                _output.WriteLine("\n-----------------------------\r\nUpdateEntryComment {0} Trial {1}, adding {2} files into '{3}'...",
                    DateTime.Now.ToString("HH:mm:ss"),
                    k,
                    entriesAdded,
                    zipFileToCreate);

                // Add the files to the zip, and save it
                using (ZipFile zip1 = new ZipFile())
                {
                    String[] filenames = Directory.GetFiles(subdir);
                    foreach (String f in filenames)
                        zip1.AddFile(f, "");

                    zip1.Comment = "UpdateTests::UpdateEntryComment(): This archive will be updated.";
                    zip1.Save(zipFileToCreate);
                }

                // Verify the files are in the zip
                int n = CountEntries(zipFileToCreate);
                Assert.Equal<int>(entriesAdded, n,
                    $"the Zip file has the wrong number of entries. expected({entriesAdded}) actual({n})");

                // update the comments for a few files in the zip archive
                int updateCount = 0;
                int numToUpdate = _rnd.Next(4) + 2;
                using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                {
                    do
                    {
                        foreach (ZipEntry e in zip2)
                        {
                            if (_rnd.Next(2) == 1)
                            {
                                if (String.IsNullOrEmpty(e.Comment))
                                {
                                    e.Comment = $"This is update {updateCount}, a new comment on entry " + e.FileName;
                                    updateCount++;
                                }
                            }
                        }
                    } while (updateCount < numToUpdate);
                    zip2.Comment = $"This archive has been modified.  Comments on {updateCount} entries have been inserted.";
                    zip2.Save();
                }

                // Extract all files, verify that none have been removed,
                // and verify the contents of those that remain.
                int commentCount = 0;
                using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
                {
                    foreach (string s1 in zip3.EntryFileNames)
                    {
                        string dir = Path.Combine(TopLevelDir, $"ex-{marker}-{k}");
                        zip3[s1].Extract(dir);
                        String repeatedLine = $"This line is repeated over and over and over in file {s1}";

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(dir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                    String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));

                        if (!String.IsNullOrEmpty(zip3[s1].Comment))
                        {
                            commentCount++;
                        }
                    }
                }

                Assert.Equal<int>(updateCount, commentCount,
                    "The updated Zip file has the wrong number of entries with comments.");

                n = CountEntries(zipFileToCreate);
                Assert.Equal<int>(entriesAdded, n,
                    $"the Zip file has the wrong number of entries. expected({entriesAdded}) actual({n})");
            }
        }

        [Fact]
        public void RemoveEntry_ByFilename()
        {
            string marker = TestUtilities.GetMarker();
            for (int k = 0; k < 2; k++)
            {
                int j;
                string filename = null;
                string repeatedLine = null;
                string zipFileToCreate = Path.Combine(TopLevelDir, $"RemoveEntry_ByFilename-{k}.zip");
                String subdir = null;
                int entriesAdded = CreateDirAndSomeFiles($"{marker}-{k}", out subdir);

                // Add the files to the zip, save the zip.
                // in pass 2, remove one file, then save again.
                using (ZipFile zip1 = new ZipFile())
                {
                    String[] filenames = Directory.GetFiles(subdir);
                    foreach (String f in filenames)
                        zip1.AddFile(f, "");

                    zip1.Comment = "UpdateTests::RemoveEntry_ByFilename(): This archive will be updated.";
                    zip1.Save(zipFileToCreate);

                    // conditionally remove a single entry, only on the 2nd trial.
                    if (k == 1)
                    {
                        int chosen = _rnd.Next(filenames.Length);
                        zip1.RemoveEntry(zip1[chosen]);
                        zip1.Save();
                    }
                }

                // Verify the files are in the zip
                Assert.Equal<int>(entriesAdded - k, CountEntries(zipFileToCreate),
                    $"Trial {k}: the Zip file has the wrong number of entries.");

                if (k == 0)
                {
                    // selectively remove a few files in the zip archive
                    var filesToRemove = new List<string>();
                    int numToRemove = _rnd.Next(entriesAdded - 4) + 1;
                    using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
                    {
                        for (j = 0; j < numToRemove; j++)
                        {
                            // select a new, uniquely named file to create
                            do
                            {
                                filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                            } while (filesToRemove.Contains(filename));
                            // add this file to the list
                            filesToRemove.Add(filename);
                            zip2.RemoveEntry(filename);

                        }

                        zip2.Comment = "This archive has been modified. Some files have been removed.";
                        zip2.Save();
                    }

                    // extract all files, verify none should have been removed,
                    // and verify the contents of those that remain
                    using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
                    {
                        string extractDir = $"ex-{marker}-{k}";
                        foreach (string s1 in zip3.EntryFileNames)
                        {
                            Assert.False(filesToRemove.Contains(s1),
                                           $"File ({s1}) was not expected.");

                            zip3[s1].Extract(Path.Combine(TopLevelDir, extractDir));
                            repeatedLine = $"This line is repeated over and over and over in file {s1}";

                            // verify the content of the updated file.
                            var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                            string sLine = sr.ReadLine();
                            sr.Close();

                            Assert.Equal<string>(repeatedLine, sLine,
                                        String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));

                        }
                    }

                    // Verify the files are in the zip
                    Assert.Equal<int>(entriesAdded - filesToRemove.Count, CountEntries(zipFileToCreate),
                                         "The updated Zip file has the wrong number of entries.");
                }
            }
        }


        [Fact]
        public void RemoveEntry_ViaIndexer_WithPassword()
        {
            string marker = TestUtilities.GetMarker();
            string password = TestUtilities.GenerateRandomPassword();
            string filename = null;
            string repeatedLine = null;
            int j;

            string zipFileToCreate = Path.Combine(TopLevelDir, "RemoveEntry_ViaIndexer_WithPassword.zip");
            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Add the files to the zip, save the zip
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                zip.Password = password;
                foreach (String f in filenames)
                    zip.AddFile(f, "");

                zip.Comment = "UpdateTests::OpenForUpdate_Password_RemoveViaIndexer(): This archive will be updated.";
                zip.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // selectively remove a few files in the zip archive
            var filesToRemove = new List<string>();
            int numToRemove = _rnd.Next(entriesAdded - 4) + 2;
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                for (j = 0; j < numToRemove; j++)
                {
                    // select a new, uniquely named file to remove
                    do
                    {
                        filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                    } while (filesToRemove.Contains(filename));
                    // add this file to the list
                    filesToRemove.Add(filename);

                    // remove the file from the zip archive
                    zip2.RemoveEntry(filename);
                }

                zip2.Comment = "This archive has been modified. Some files have been removed.";
                zip2.Save();
            }

            // extract all files, verify the contents of those that remain.
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s1 in zip3.EntryFileNames)
                {
                    Assert.False(filesToRemove.Contains(s1), $"File ({s1}) was not expected.");

                    zip3[s1].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), password);
                    repeatedLine = $"This line is repeated over and over and over in file {s1}";

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine,
                                            sLine,
                                            String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));

                }
            }

            Assert.Equal<int>(entriesAdded - filesToRemove.Count, CountEntries(zipFileToCreate),
                                 "The updated Zip file has the wrong number of entries.");
        }


        [Fact]
        public void RemoveAllEntries()
        {
            string marker = TestUtilities.GetMarker();
            string password = "Wheeee!!" + TestUtilities.GenerateRandomLowerString(7);
            string zipFileToCreate = Path.Combine(TopLevelDir, "RemoveAllEntries.zip");
            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Add the files to the zip, save the zip
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                zip.Password = password;
                foreach (String f in filenames)
                    zip.AddFile(f, "");

                zip.Comment = "UpdateTests::RemoveAllEntries(): This archive will be updated.";
                zip.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // remove all the entries from the zip archive
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                zip2.RemoveSelectedEntries("*.*");
                zip2.Comment = "This archive has been modified. All the entries have been removed.";
                zip2.Save();
            }

            // Verify the files are in the zip
            Assert.Equal<int>(0, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");
        }


        [Fact]
        public void AddFile_OldEntriesWithPassword()
        {
            string marker = TestUtilities.GetMarker();
            string password = "Secret!";
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddFile_OldEntriesWithPassword.zip");
            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Create the zip file
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = password;
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::AddFile_OldEntriesWithPassword(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // Create a bunch of new files...
            var addedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 4);
            for (int j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                String filename = String.Format("newfile{0:D3}.txt", j);
                // create a new file, and fill that new file with text data
                String repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                addedFiles.Add(filename);
            }

            // add each one of those new files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string s in addedFiles)
                    zip2.AddFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::AddFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded + addedFiles.Count, CountEntries(zipFileToCreate),
                                 "The Zip file has the wrong number of entries.");

            // now extract the newly-added files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in addedFiles)
                {
                    String repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract(Path.Combine(TopLevelDir,extractDir));

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex2-{marker}";
                foreach (string s1 in zip4.EntryFileNames)
                {
                    bool addedLater = false;
                    foreach (string s2 in addedFiles)
                    {
                        if (s2 == s1) addedLater = true;
                    }
                    if (!addedLater)
                    {
                        zip4[s1].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), password);
                        String repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }



        [Fact]
        public void UpdateItem()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "UpdateItem.zip");
            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Create the zip file
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateItem(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            subdir = Path.Combine(TopLevelDir, $"B-{marker}");
            Directory.CreateDirectory(subdir);

            // create a bunch more files
            int newFileCount = entriesAdded + _rnd.Next(3) + 3;
            for (int j = 0; j < newFileCount; j++)
            {
                String filename = String.Format("file{0:D3}.txt", j);
                String repeatedLine = String.Format("Content for the updated file {0} {1}",
                    filename,
                    System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(1000) + 2000);
                entriesAdded++;
            }

            // Update those files in the zip file
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.UpdateItem(f, "");
                zip1.Comment = "UpdateTests::UpdateItem(): This archive has been updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(newFileCount, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // now extract the files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in zip3.EntryFileNames)
                {
                    String repeatedLine = String.Format("Content for the updated file {0} {1}",
                        s,
                        System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract(Path.Combine(TopLevelDir, extractDir));

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }
        }


        [Fact]
        public void AddFile_NewEntriesWithPassword()
        {
            string marker = TestUtilities.GetMarker();
            string password = "V.Secret!";
            string filename = null;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddFile_NewEntriesWithPassword.zip");

            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Create the zip archive
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                zip.AddFiles(filenames, "");
                zip.Comment = "UpdateTests::AddFile_NewEntriesWithPassword(): This archive will be updated.";
                zip.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // Create a bunch of new files...
            var addedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 4);
            for (j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                filename = String.Format("newfile{0:D3}.txt", j);
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                addedFiles.Add(filename);
            }

            // add each one of those new files in the zip archive using a password
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                zip2.Password = password;
                foreach (string s in addedFiles)
                    zip2.AddFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::AddFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            Assert.Equal<int>(entriesAdded + addedFiles.Count, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // now extract the newly-added files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in addedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), password);

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }


            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex2-{marker}";
                foreach (string s1 in zip4.EntryFileNames)
                {
                    bool addedLater = false;
                    foreach (string s2 in addedFiles)
                    {
                        if (s2 == s1) addedLater = true;
                    }
                    if (!addedLater)
                    {
                        zip4[s1].Extract(Path.Combine(TopLevelDir, extractDir));
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [Fact]
        public void AddFile_DifferentPasswords()
        {
            string marker = TestUtilities.GetMarker();
            string password1 = Path.GetRandomFileName();
            string password2 = "Secret2" + Path.GetRandomFileName();
            string filename = null;
            string repeatedLine = null;
            int j;

            // select the name of the zip file
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddFile_DifferentPasswords.zip");

            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Add the files to the zip, save the zip
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = password1;
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::AddFile_DifferentPasswords(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // Create a bunch of new files...
            var addedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 4);
            //int numToUpdate = 1;
            for (j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                filename = String.Format("newfile{0:D3}.txt", j);
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                addedFiles.Add(filename);
            }

            // add each one of those new files in the zip archive using a password
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                zip2.Password = password2;
                foreach (string s in addedFiles)
                    zip2.AddFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::AddFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded + addedFiles.Count, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // now extract the newly-added files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in addedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), password2);

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }


            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex2-{marker}";
                foreach (string s1 in zip4.EntryFileNames)
                {
                    bool addedLater = false;
                    foreach (string s2 in addedFiles)
                    {
                        if (s2 == s1) addedLater = true;
                    }
                    if (!addedLater)
                    {
                        zip4[s1].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), password1);
                        repeatedLine = $"This line is repeated over and over and over in file {s1}";

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }




        [Fact]
        public void UpdateFile_NoPasswords()
        {
            string marker = TestUtilities.GetMarker();
            string filename = null;
            int j = 0;
            string repeatedLine = null;

            // select the name of the zip file
            string zipFileToCreate = Path.Combine(TopLevelDir, "UpdateFile_NoPasswords.zip");

            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateFile_NoPasswords(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "Zoiks! The Zip file has the wrong number of entries.");

            // create another subdirectory
            subdir = Path.Combine(TopLevelDir, $"updates-{marker}");
            Directory.CreateDirectory(subdir);

            // Create a bunch of new files, in that new subdirectory
            var updatedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 4);
            for (j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                } while (updatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                updatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string s in updatedFiles)
                    zip2.UpdateFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in updatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract(Path.Combine(TopLevelDir, extractDir));

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex2-{marker}";
                foreach (string s1 in zip4.EntryFileNames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in updatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip4[s1].Extract(Path.Combine(TopLevelDir, extractDir));
                        repeatedLine = $"This line is repeated over and over and over in file {s1}";

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [Fact]
        public void UpdateFile_2_NoPasswords()
        {
            string marker = TestUtilities.GetMarker();
            string filename = null;
            int j = 0;
            string repeatedLine = null;

            // select the name of the zip file
            string zipFileToCreate = Path.Combine(TopLevelDir, "UpdateFile_NoPasswords.zip");

            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.UpdateFile(f, "");
                zip1.Comment = "UpdateTests::UpdateFile_NoPasswords(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "Zoiks! The Zip file has the wrong number of entries.");

            // create another subdirectory
            subdir = Path.Combine(TopLevelDir, $"updates-{marker}");
            Directory.CreateDirectory(subdir);

            // Create a bunch of new files, in that new subdirectory
            var updatedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 4);
            for (j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                } while (updatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                updatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string s in updatedFiles)
                    zip2.UpdateFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateFile_NoPasswords(): This archive has been updated.";
                zip2.Save();
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "Zoiks! The Zip file has the wrong number of entries.");

            // update those files AGAIN in the zip archive
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string s in updatedFiles)
                    zip3.UpdateFile(Path.Combine(subdir, s), "");
                zip3.Comment = "UpdateTests::UpdateFile_NoPasswords(): This archive has been re-updated.";
                zip3.Save();
            }

            // extract the updated files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in updatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip4[s].Extract(Path.Combine(TopLevelDir, extractDir));

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip5 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex2-{marker}";
                foreach (string s1 in zip5.EntryFileNames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in updatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip5[s1].Extract(Path.Combine(TopLevelDir, extractDir));
                        repeatedLine = $"This line is repeated over and over and over in file {s1}";

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }



        [Fact]
        public void UpdateFile_OldEntriesWithPassword()
        {
            string marker = TestUtilities.GetMarker();
            string Password = "1234567";
            string filename = null;
            int j = 0;
            string repeatedLine = null;

            // select the name of the zip file
            string zipFileToCreate = Path.Combine(TopLevelDir, "UpdateFile_OldEntriesWithPassword.zip");

            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // Create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = Password;
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateFile_OldEntriesWithPassword(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the number of files in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            subdir = Path.Combine(TopLevelDir, $"updates-{marker}");
            Directory.CreateDirectory(subdir);

            // Create a bunch of new files, in that new subdirectory
            var updatedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 4);
            for (j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                } while (updatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                updatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string s in updatedFiles)
                    zip2.UpdateFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateFile_OldEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in updatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].Extract(Path.Combine(TopLevelDir, extractDir));

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s1 in zip4.EntryFileNames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in updatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip4[s1].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), Password);
                        repeatedLine = $"This line is repeated over and over and over in file {s1}";

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [Fact]
        public void UpdateFile_NewEntriesWithPassword()
        {
            string marker = TestUtilities.GetMarker();
            string Password = " P@ssw$rd";
            string filename = null;
            string repeatedLine = null;
            int j = 0;

            string zipFileToCreate = Path.Combine(TopLevelDir, "UpdateFile_NewEntriesWithPassword.zip");
            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // create the zip archive, add those files to it
            using (ZipFile zip1 = new ZipFile())
            {
                // no password used here.
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateFile_NewEntriesWithPassword(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            subdir = Path.Combine(TopLevelDir, $"updates-{marker}");
            Directory.CreateDirectory(subdir);

            // Create a bunch of new files, in that new subdirectory
            var updatedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 5) + 1;
            Assert.True(numToUpdate > 1);
            for (j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                } while (updatedFiles.Contains(filename));
                // create the new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                updatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                zip2.Password = Password;
                foreach (string s in updatedFiles)
                    zip2.UpdateFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateFile_NewEntriesWithPassword(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in updatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), Password);

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(repeatedLine, sLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex2-{marker}";
                foreach (string s1 in zip4.EntryFileNames)
                {
                    bool NotUpdated = true;
                    foreach (string s2 in updatedFiles)
                    {
                        if (s2 == s1) NotUpdated = false;
                    }
                    if (NotUpdated)
                    {
                        zip4[s1].Extract(Path.Combine(TopLevelDir, extractDir));
                        repeatedLine = $"This line is repeated over and over and over in file {s1}";

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                String.Format("The content of the originally added file ({0}) in the zip archive is incorrect.", s1));
                    }
                }
            }
        }


        [Fact]
        public void UpdateFile_DifferentPasswords()
        {
            string marker = TestUtilities.GetMarker();
            string Password1 = "Whoofy1";
            string Password2 = "Furbakl1";
            string filename = null;
            int j = 0;
            string repeatedLine;

            string zipFileToCreate = Path.Combine(TopLevelDir, "UpdateFile_DifferentPasswords.zip");
            String subdir = null;
            int entriesAdded = CreateDirAndSomeFiles(marker, out subdir);

            // create the zip archive
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.Password = Password1;
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip1.AddFile(f, "");
                zip1.Comment = "UpdateTests::UpdateFile_DifferentPasswords(): This archive will be updated.";
                zip1.Save(zipFileToCreate);
            }

            // Verify the files are in the zip
            Assert.Equal<int>(entriesAdded, CountEntries(zipFileToCreate),
                "The Zip file has the wrong number of entries.");

            // create another subdirectory
            subdir = Path.Combine(TopLevelDir, "updates");
            Directory.CreateDirectory(subdir);

            // Create a bunch of new files, in that new subdirectory
            var updatedFiles = new List<string>();
            int numToUpdate = _rnd.Next(entriesAdded - 4);
            for (j = 0; j < numToUpdate; j++)
            {
                // select a new, uniquely named file to create
                do
                {
                    filename = String.Format("file{0:D3}.txt", _rnd.Next(entriesAdded));
                } while (updatedFiles.Contains(filename));
                // create a new file, and fill that new file with text data
                repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                    filename, System.DateTime.Now.ToString("yyyy-MM-dd"));
                TestUtilities.CreateAndFillFileText(Path.Combine(subdir, filename), repeatedLine, _rnd.Next(34000) + 5000);
                updatedFiles.Add(filename);
            }

            // update those files in the zip archive
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                zip2.Password = Password2;
                foreach (string s in updatedFiles)
                    zip2.UpdateFile(Path.Combine(subdir, s), "");
                zip2.Comment = "UpdateTests::UpdateFile_DifferentPasswords(): This archive has been updated.";
                zip2.Save();
            }

            // extract those files and verify their contents
            using (ZipFile zip3 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex-{marker}";
                foreach (string s in updatedFiles)
                {
                    repeatedLine = String.Format("**UPDATED** This file ({0}) has been updated on {1}.",
                        s, System.DateTime.Now.ToString("yyyy-MM-dd"));
                    zip3[s].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), Password2);

                    // verify the content of the updated file.
                    var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s));
                    string sLine = sr.ReadLine();
                    sr.Close();

                    Assert.Equal<string>(sLine, repeatedLine,
                            String.Format("The content of the Updated file ({0}) in the zip archive is incorrect.", s));
                }
            }

            // extract all the other files and verify their contents
            using (ZipFile zip4 = ZipFile.Read(zipFileToCreate))
            {
                string extractDir = $"ex2-{marker}";
                foreach (string s1 in zip4.EntryFileNames)
                {
                    bool wasUpdated = false;
                    foreach (string s2 in updatedFiles)
                    {
                        if (s2 == s1) wasUpdated = true;
                    }
                    if (!wasUpdated)
                    {
                        // use original password
                        zip4[s1].ExtractWithPassword(Path.Combine(TopLevelDir, extractDir), Password1);
                        repeatedLine = String.Format("This line is repeated over and over and over in file {0}",
                            s1);

                        // verify the content of the updated file.
                        var sr = new StreamReader(Path.Combine(TopLevelDir, extractDir, s1));
                        string sLine = sr.ReadLine();
                        sr.Close();

                        Assert.Equal<string>(repeatedLine, sLine,
                                $"The content of the originally added file ({s1}) in the zip archive is incorrect.");
                    }
                }
            }
        }


        [Fact]
        public void AddFile_ExistingFile_Error()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddFile_ExistingFile_Error.zip");
            string subdir = Path.Combine(TopLevelDir, $"A-{marker}");
            Directory.CreateDirectory(subdir);

            // create the files
            int fileCount = _rnd.Next(3) + 4;
            string filename = null;
            int entriesAdded = 0;
            for (int j = 0; j < fileCount; j++)
            {
                filename = Path.Combine(subdir, String.Format("file{0:D3}.txt", j));
                TestUtilities.CreateAndFillFileText(filename, _rnd.Next(34000) + 5000);
                entriesAdded++;
            }

            // Add the files to the zip, save the zip
            using (ZipFile zip = new ZipFile())
            {
                String[] filenames = Directory.GetFiles(subdir);
                foreach (String f in filenames)
                    zip.AddFile(f, "");
                zip.Comment = "UpdateTests::AddFile_ExistingFile_Error(): This archive will be updated.";
                zip.Save(zipFileToCreate);
            }

            // create and file a new file with text data
            int FileToUpdate = _rnd.Next(fileCount);
            filename = Path.Combine(TopLevelDir, String.Format("file{0:D3}.txt", FileToUpdate));
            string repeatedLine = String.Format("**UPDATED** UpdateTests.AddFile_ExistingFile_Error() This file ({0}) was updated at {1}.",
                        filename,
                        System.DateTime.Now.ToString("G"));
            TestUtilities.CreateAndFillFileText(filename, repeatedLine, _rnd.Next(21567) + 23872);

            // Try to again add that file in the zip archive.
            Assert.Throws<ArgumentException>(() => {
            using (ZipFile z = ZipFile.Read(zipFileToCreate))
            {
                // Try Adding a file again.  THIS SHOULD THROW.
                ZipEntry e = z.AddFile(filename, "");
                z.Comment = "UpdateTests::AddFile_ExistingFile_Error(): This archive has been updated.";
                z.Save();
            }
            });
        }


        [Fact]
        public void MultipleSaves_wi10319()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "MultipleSaves_wi10319.zip");

            using (ZipFile _zipFile = new ZipFile(zipFileToCreate))
            {
                using (MemoryStream data = new MemoryStream())
                {
                    using (StreamWriter writer = new StreamWriter(data))
                    {
                        writer.Write("Dit is een test string.");
                        writer.Flush();

                        data.Seek(0, SeekOrigin.Begin);
                        _zipFile.AddEntry("test.txt", data);
                        _zipFile.Save();
                        _zipFile.AddEntry("test2.txt", "Esta es un string de test");
                        _zipFile.Save();
                        _zipFile.AddEntry("test3.txt", "this is some content for the entry.");
                        _zipFile.Save();
                    }
                }
            }

            using (ZipFile _zipFile = new ZipFile(zipFileToCreate))
            {
                using (MemoryStream data = new MemoryStream())
                {
                    using (StreamWriter writer = new StreamWriter(data))
                    {
                        writer.Write("Dit is een andere test string.");
                        writer.Flush();

                        data.Seek(0, SeekOrigin.Begin);

                        _zipFile.UpdateEntry("test.txt", data);
                        _zipFile.Save();
                        _zipFile.UpdateEntry("test2.txt", "Esta es un otro string de test");
                        _zipFile.Save();
                        _zipFile.UpdateEntry("test3.txt", "This is another string for content.");
                        _zipFile.Save();

                    }
                }
            }
        }


        [Fact]
        public void MultipleSaves_wi10694()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "MultipleSaves_wi10694.zip");
            var shortDir = $"fodder-{marker}";
            string subdir = Path.Combine(TopLevelDir, shortDir);
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddFiles(filesToZip, "Download");
                zip1.AddFiles(filesToZip, "other");
                zip1.Save(zipFileToCreate);
            }

            Assert.Equal<int>(2 * filesToZip.Length, CountEntries(zipFileToCreate),
                                 "Incorrect number of entries in the zip file.");

            using (var zip2 = ZipFile.Read(zipFileToCreate))
            {
                var entries = zip2.Entries.Where(e => e.FileName.Contains("Download")).ToArray();
                //PART1 - Add directory and save
                zip2.AddDirectoryByName($"XX-{marker}");
                zip2.Save();

                //PART2 - Rename paths (not related to XX directory from above) and save
                foreach (var zipEntry in entries)
                {
                    zipEntry.FileName = zipEntry.FileName.Replace("Download", "Download2");
                }
                zip2.Save();
            }

            Assert.Equal<int>(2 * filesToZip.Length, CountEntries(zipFileToCreate),
                                 "Incorrect number of entries in the zip file.");
        }


        [Fact]
        public void MultipleSavesWithRename_wi10544()
        {
            // select the name of the zip file
            string originalZipLocation = Path.Combine(TopLevelDir, "Update_MultipleSaves_wi10319.zip");
            const int numEntries = 10;
            _output.WriteLine("Creating zip file... ");
            using (var zip = new ZipFile())
            {
                for (int i = 0; i < numEntries; i++) {
                    string entryName = $"Entry{i}.txt";
                    _output.WriteLine($"  writing entry {i}: {entryName}");
                    string dataForTheEntry = $"This is the data for Entry {i}.\n" + DateTime.Now.ToString("G");
                    byte[] a = System.Text.Encoding.ASCII.GetBytes(dataForTheEntry.ToCharArray());
                    zip.AddEntry(new String(entryName), a);
                }
                zip.Save(originalZipLocation);
            }

            int N = _rnd.Next(59) + 34;
            _output.WriteLine("Performing {0} cycles... ", N);
            for (int i = 0; i < N; i++)
            {
                string tempZipFile = Path.Combine(TopLevelDir, $"cycle-{i}.zip.tmp");
                _output.WriteLine("==== Update cycle {0}/{1}... ", i+1,N);
                using (var zip1 = ZipFile.Read(originalZipLocation))
                {
                    // create a new zip file
                    using (var zip = new ZipFile())
                    {
                        int selected = _rnd.Next(numEntries);
                        string selectedEntryName = $"Entry{selected}.txt";
                        _output.WriteLine($"  selected entry {selected}: {selectedEntryName}... ");

                        // for all entries in the original
                        foreach (ZipEntry e in zip1) {
                            _output.WriteLine($"  handling entry {e.FileName}...");
                            zip.AddEntry(e.FileName, (name, stream) => {
                                // read the entry from zip1
                                var src = zip1[name].OpenReader();
                                int n;
                                byte[] b = new byte[2048];
                                while ((n = src.Read(b, 0, b.Length)) > 0)
                                stream.Write(b, 0, n);

                                // also write more, if this is the selected entry
                                if (e.FileName == selectedEntryName) {
                                    string update = String.Format("For cycle {0}, this is updated content for entry {1} at {2}\n", N, i, DateTime.Now.ToString("G"));
                                    byte[] a = System.Text.Encoding.ASCII.GetBytes(update.ToCharArray());
                                    stream.Write(a, 0, a.Length);
                                }

                            });
                        }

                        zip.Save(tempZipFile);
                    }
                }
                File.Delete(originalZipLocation);
                System.Threading.Thread.Sleep(40); // 1400
                File.Move(tempZipFile, originalZipLocation);
            }
        }


        [Fact]
        public void FromRoot_wi11988()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "FromRoot.zip");
            string dirToZip = Path.Combine(TopLevelDir, $"Fodder-{marker}");
            var files = TestUtilities.GenerateFilesFlat(dirToZip);
            string windir = System.Environment.GetEnvironmentVariable("Windir");
            string substExe = Path.Combine(windir, "system32", "subst.exe");
            Assert.True(File.Exists(substExe), $"subst.exe does not exist ({substExe})");

            try
            {
                // create a subst drive
                this.Exec(substExe, "G: " + dirToZip);

                using (var zip = new ZipFile())
                {
                    zip.UpdateSelectedFiles("*.*", "G:\\", "", true);
                    zip.Save(zipFileToCreate);
                }

                Assert.Equal<int>(files.Length, CountEntries(zipFileToCreate));
                Assert.True(files.Length > 3);
                BasicVerifyZip(zipFileToCreate);
            }
            finally
            {
                // remove the virt drive
                this.Exec(substExe, "/D G:");
            }
        }

    }
}
