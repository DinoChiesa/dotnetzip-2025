// Selector.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2010 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// This module defines tests for the File and Entry Selection stuff in
// DotNetZip.
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
    public class SelectorTests : IonicTestClass
    {
        private static DateTime twentyDaysAgo;
        private static DateTime todayAtMidnight;
        private static DateTime tomorrow;
        private static DateTime threeDaysAgo;
        private static DateTime threeYearsAgo;
        private static DateTime twoDaysAgo;
        private static DateTime yesterdayAtMidnight;
        private static TimeSpan oneDay;

        public SelectorTests(ITestOutputHelper output)
        {
            _output = output;
            twentyDaysAgo = DateTime.Now - new TimeSpan(20, 0, 0, 0);
            todayAtMidnight = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            tomorrow = todayAtMidnight + new TimeSpan(1, 0, 0, 0);
            threeDaysAgo = todayAtMidnight - new TimeSpan(3, 0, 0, 0);
            twoDaysAgo = todayAtMidnight - new TimeSpan(2, 0, 0, 0);
            threeYearsAgo = new DateTime(
                DateTime.Now.Year - 3,
                DateTime.Now.Month,
                DateTime.Now.Day
            );

            oneDay = new TimeSpan(1, 0, 0, 0);
            yesterdayAtMidnight = todayAtMidnight - oneDay;
        }

        [Fact]
        public void Selector_EdgeCases()
        {
            string subdir = Path.Combine(TopLevelDir, "A");
            Ionic.FileSelector ff = new Ionic.FileSelector("name = *.txt");
            var list = ff.SelectFiles(subdir);
            Assert.NotNull(list);
            Assert.True(list.Count == 0);

            ff.SelectionCriteria = "name = *.bin";
            list = ff.SelectFiles(subdir);
            Assert.NotNull(list);
            Assert.True(list.Count == 0);
        }

        // /// <summary>
        // ///   Checks a fodder directory to see if suitable.
        // /// </summary>
        // /// <param name='dir'>the directory to check</param>
        // ///
        // /// <returns>
        // ///   true if the directory contains a goodly number of fodder files.
        // /// </returns>
        // private bool TryOneFodderDir(string dir)
        // {
        //     if (!Directory.Exists(dir))
        //         return false;
        //
        //     var ctime = File.GetCreationTime(dir).ToUniversalTime();
        //     if ((todayAtMidnight - ctime) > oneDay || (ctime - todayAtMidnight) > oneDay)
        //         return false;
        //
        //
        //     var fodderFiles = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
        //
        //     numFodderFiles = fodderFiles.Length;
        //     if (numFodderFiles <= 2)
        //     {
        //         numFodderFiles = 0;
        //         return false;
        //     }
        //
        //     var fodderDirs = Directory.GetDirectories(dir, "*.*",
        //                                               SearchOption.AllDirectories);
        //     numFodderDirs = fodderDirs.Length;
        //     if (numFodderDirs <= 2)
        //     {
        //         numFodderDirs = numFodderFiles = 0;
        //         return false;
        //     }
        //     return true;
        // }
        //
        //
        // private string SetupFiles()
        // {
        //     lock (LOCK)
        //     {
        //         if (fodderDirectory != null && numFodderFiles > 5)
        //             return fodderDirectory;
        //
        //         string homeDir = System.Environment.GetEnvironmentVariable("TEMP");
        //         var oldDirs = Directory.GetDirectories(homeDir, "*.SelectorTests");
        //
        //         foreach (var dir in oldDirs)
        //         {
        //             if (TryOneFodderDir(dir))
        //             {
        //                 fodderDirectory = dir;
        //                 return dir;
        //             }
        //
        //             if (Directory.Exists(dir))
        //                 Directory.Delete(dir, true);
        //         }
        //
        //         // Arriving here means no good fodder directories exist.
        //         // Create one.
        //         ActuallyCreateFodderFiles();
        //         Assert.True(TryOneFodderDir(fodderDirectory));
        //         return fodderDirectory;
        //     }
        // }


        // private static void DeleteOldFodderDirectories( Ionic.CopyData.Transceiver txrx )
        // {
        //     // Before creating the directory for the current run, Remove old directories.
        //     // For some reason the test cleanup code tends to leave these directories??
        //     string tempDir = System.Environment.GetEnvironmentVariable("TEMP");
        //     var oldDirs = Directory.GetDirectories(tempDir, "*.SelectorTests");
        //     if (oldDirs.Length > 0)
        //     {
        //         if (txrx != null)
        //         {
        //             txrx.Send("status deleting old directories...");
        //             txrx.Send(String.Format("pb 0 max {0}", oldDirs.Length));
        //         }
        //
        //         foreach (var dir in oldDirs)
        //         {
        //             CleanDirectory(dir, txrx);
        //             if (txrx != null) txrx.Send("pb 0 step");
        //         }
        //     }
        // }

        private string CreateFodderFiles(out int totalFilesCreated, out int totalDirsCreated)
        {
            int fileCount = _rnd.Next(95) + 55;
            _output.WriteLine("TopLevelDir {0}", TopLevelDir);
            string fodderDir = TestUtilities.UniqueDir(Path.Combine(TopLevelDir, "SelectorTests"));
            int entriesAdded = 0;
            _output.WriteLine("creating fodder dir {0}", fodderDir);
            Directory.CreateDirectory(fodderDir);

            string[] nameFormats =
            {
                "file{0:D3}",
                "{0:D3}",
                "PrettyLongFileName-{0:D3}",
                "Very-Long-Filename-{0:D3}-with-a-repeated-segment-{0:D3}-{0:D3}-{0:D3}-{0:D3}",
            };

            string[] dirs = { "dir1", "dir1\\dirA", "dir1\\dirB", "dir2" };

            totalDirsCreated = 0;
            foreach (string s in dirs)
            {
                Directory.CreateDirectory(Path.Combine(fodderDir, s));
                totalDirsCreated++;
            }

            for (int j = 0; j < fileCount; j++)
            {
                // select the size
                int sz = 0;
                if (j % 5 == 0)
                    sz = _rnd.Next(15000) + 150000;
                else if (j % 17 == 1)
                    sz = _rnd.Next(50 * 1024) + 1024 * 1024;
                else if (_rnd.Next(13) == 0)
                    sz = 8080; // exactly
                else
                    sz = _rnd.Next(5000) + 5000;

                // randomly select the format of the file name
                int n = _rnd.Next(4);

                // binary or text
                string filename = null;
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(fodderDir, String.Format(nameFormats[n], j) + ".txt");
                    TestUtilities.CreateAndFillFileText(filename, sz);
                }
                else
                {
                    filename = Path.Combine(fodderDir, String.Format(nameFormats[n], j) + ".bin");
                    TestUtilities.CreateAndFillFileBinary(filename, sz);
                }

                // maybe backdate ctime
                if (_rnd.Next(2) == 0)
                {
                    var span = new TimeSpan(
                        _rnd.Next(12),
                        _rnd.Next(24),
                        _rnd.Next(59),
                        _rnd.Next(59)
                    );
                    TouchFile(filename, WhichTime.ctime, twentyDaysAgo + span);
                }

                // maybe backdate mtime
                if (_rnd.Next(2) == 0)
                {
                    var span = new TimeSpan(
                        _rnd.Next(1),
                        _rnd.Next(24),
                        _rnd.Next(59),
                        _rnd.Next(59)
                    );
                    TouchFile(filename, WhichTime.mtime, threeDaysAgo + span);
                }

                // maybe backdate atime
                if (_rnd.Next(2) == 0)
                {
                    var span = new TimeSpan(_rnd.Next(24), _rnd.Next(59), _rnd.Next(59));
                    TouchFile(filename, WhichTime.atime, yesterdayAtMidnight + span);
                }

                // set the creation time to "a long time ago" on 1/14th of the files
                if (j % 14 == 0)
                {
                    DateTime x = new DateTime(1998, 4, 29); // julianna
                    var span = new TimeSpan(
                        _rnd.Next(22),
                        _rnd.Next(24),
                        _rnd.Next(59),
                        _rnd.Next(59)
                    );
                    File.SetCreationTime(filename, x + span);
                }

                // maybe move to a subdir
                n = _rnd.Next(6);
                if (n < 4)
                {
                    string newFilename = Path.Combine(
                        fodderDir,
                        dirs[n],
                        Path.GetFileName(filename)
                    );
                    File.Move(filename, newFilename);
                    filename = newFilename;
                }

                // mark some of the files as hidden, system, readonly, etc
                if (j % 9 == 0)
                    File.SetAttributes(filename, FileAttributes.Hidden);
                if (j % 14 == 0)
                    File.SetAttributes(filename, FileAttributes.ReadOnly);
                if (j % 13 == 0)
                    File.SetAttributes(filename, FileAttributes.System);
                if (j % 11 == 0)
                    File.SetAttributes(filename, FileAttributes.Archive);

                entriesAdded++;

                if (entriesAdded % 8 == 0)
                    _output.WriteLine("creating files ({0}/{1})", entriesAdded, fileCount);
            }

            totalFilesCreated = entriesAdded;
            return fodderDir;
        }

        class Trial
        {
            public string Label;
            public string C1;
            public string C2;
        }

        [Fact]
        public void Selector_SelectFiles()
        {
            Trial[] trials = new Trial[]
            {
                new Trial
                {
                    Label = "name",
                    C1 = "name = *.txt",
                    C2 = "name = *.bin",
                },
                new Trial
                {
                    Label = "name (shorthand)",
                    C1 = "*.txt",
                    C2 = "*.bin",
                },
                new Trial
                {
                    Label = "size",
                    C1 = "size < 7500",
                    C2 = "size >= 7500",
                },
                new Trial
                {
                    Label = "size",
                    C1 = "size = 8080",
                    C2 = "size != 8080",
                },
                new Trial
                {
                    Label = "name & size",
                    C1 = "name = *.bin AND size > 7500",
                    C2 = "name != *.bin  OR  size <= 7500",
                },
                new Trial
                {
                    Label = "name XOR name",
                    C1 = "name = *.bin XOR name = *4.*",
                    C2 = "(name != *.bin OR name = *4.*) AND (name = *.bin OR name != *4.*)",
                },
                new Trial
                {
                    Label = "name XOR size",
                    C1 = "name = *.bin XOR size > 100k",
                    C2 = "(name != *.bin OR size > 100k) AND (name = *.bin OR size <= 100k)",
                },
                new Trial
                {
                    Label = "mtime",
                    C1 = String.Format("mtime < {0}", twentyDaysAgo.ToString("yyyy-MM-dd")),
                    C2 = String.Format("mtime >= {0}", twentyDaysAgo.ToString("yyyy-MM-dd")),
                },
                new Trial
                {
                    Label = "ctime",
                    C1 = String.Format("mtime < {0}", threeDaysAgo.ToString("yyyy-MM-dd")),
                    C2 = String.Format("mtime >= {0}", threeDaysAgo.ToString("yyyy-MM-dd")),
                },
                new Trial
                {
                    Label = "atime",
                    C1 = String.Format("mtime < {0}", yesterdayAtMidnight.ToString("yyyy-MM-dd")),
                    C2 = String.Format("mtime >= {0}", yesterdayAtMidnight.ToString("yyyy-MM-dd")),
                },
                new Trial
                {
                    Label = "size (100k)",
                    C1 = "size > 100k",
                    C2 = "size <= 100kb",
                },
                new Trial
                {
                    Label = "size (1mb)",
                    C1 = "size > 1m",
                    C2 = "size <= 1mb",
                },
                new Trial
                {
                    Label = "size (1gb)",
                    C1 = "size > 1g",
                    C2 = "size <= 1gb",
                },
                new Trial
                {
                    Label = "attributes (Hidden)",
                    C1 = "attributes = H",
                    C2 = "attributes != H",
                },
                new Trial
                {
                    Label = "attributes (ReadOnly)",
                    C1 = "attributes = R",
                    C2 = "attributes != R",
                },
                new Trial
                {
                    Label = "attributes (System)",
                    C1 = "attributes = S",
                    C2 = "attributes != S",
                },
                new Trial
                {
                    Label = "attributes (Archive)",
                    C1 = "attributes = A",
                    C2 = "attributes != A",
                },
            };

            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectFiles.zip");
            Assert.False(
                File.Exists(zipFileToCreate),
                "The zip file '{zipFileToCreate}' already exists."
            );

            int count1,
                count2;

            string fodderDir = CreateFodderFiles(out count1, out count2);
            var topLevelFiles = Directory.GetFiles(fodderDir, "*.*", SearchOption.TopDirectoryOnly);

            for (int m = 0; m < trials.Length; m++)
            {
                Ionic.FileSelector ff = new Ionic.FileSelector(trials[m].C1);
                var list = ff.SelectFiles(fodderDir);
                _output.WriteLine("=======================================================");
                _output.WriteLine("Selector: " + ff.ToString());
                _output.WriteLine("Criteria({0})", ff.SelectionCriteria);
                _output.WriteLine("Count({0})", list.Count);
                count1 = 0;
                foreach (string s in list)
                {
                    switch (m)
                    {
                        case 0:
                        case 1:
                            Assert.True(s.EndsWith(".txt"));
                            break;
                        case 2:
                            {
                                FileInfo fi = new FileInfo(s);
                                Assert.True(fi.Length < 7500);
                            }
                            break;
                        case 4:
                            {
                                FileInfo fi = new FileInfo(s);
                                bool x = s.EndsWith(".bin") && fi.Length > 7500;
                                Assert.True(x);
                            }
                            break;
                    }
                    count1++;
                }

                ff = new Ionic.FileSelector(trials[m].C2);
                list = ff.SelectFiles(fodderDir);
                _output.WriteLine("- - - - - - - - - - - - - - - - - - - - - - - - - - - -");
                _output.WriteLine("Criteria({0})", ff.SelectionCriteria);
                _output.WriteLine("Count({0})", list.Count);
                count2 = 0;
                foreach (string s in list)
                {
                    switch (m)
                    {
                        case 0:
                        case 1:
                            Assert.True(s.EndsWith(".bin"));
                            break;
                        case 2:
                            {
                                FileInfo fi = new FileInfo(s);
                                Assert.True(fi.Length >= 7500);
                            }
                            break;
                        case 4:
                            {
                                FileInfo fi = new FileInfo(s);
                                bool x = !s.EndsWith(".bin") || fi.Length <= 7500;
                                Assert.True(x);
                            }
                            break;
                    }
                    count2++;
                }
                Assert.Equal<Int32>(topLevelFiles.Length, count1 + count2);
            }
        }

        [Fact] /* , Timeout(7200000) */
        public void Selector_AddSelectedFiles()
        {
            Trial[] trials = new Trial[]
            {
                new Trial
                {
                    Label = "name",
                    C1 = "name = *.txt",
                    C2 = "name = *.bin",
                },
                new Trial
                {
                    Label = "name (shorthand)",
                    C1 = "*.txt",
                    C2 = "*.bin",
                },
                new Trial
                {
                    Label = "attributes (Hidden)",
                    C1 = "attributes = H",
                    C2 = "attributes != H",
                },
                new Trial
                {
                    Label = "attributes (ReadOnly)",
                    C1 = "attributes = R",
                    C2 = "attributes != R",
                },
                new Trial
                {
                    Label = "mtime",
                    C1 = "mtime < 2007-01-01",
                    C2 = "mtime > 2007-01-01",
                },
                new Trial
                {
                    Label = "atime",
                    C1 = "atime < 2007-01-01",
                    C2 = "atime > 2007-01-01",
                },
                new Trial
                {
                    Label = "ctime",
                    C1 = "ctime < 2007-01-01",
                    C2 = "ctime > 2007-01-01",
                },
                new Trial
                {
                    Label = "size",
                    C1 = "size < 7500",
                    C2 = "size >= 7500",
                },
                new Trial
                {
                    Label = "name & size",
                    C1 = "name = *.bin AND size > 7500",
                    C2 = "name != *.bin  OR  size <= 7500",
                },
                new Trial
                {
                    Label = "name, size & attributes",
                    C1 = "name = *.bin AND size > 8kb and attributes = H",
                    C2 = "name != *.bin  OR  size <= 8kb or attributes != H",
                },
                new Trial
                {
                    Label = "name, size, time & attributes.",
                    C1 = "name = *.bin AND size > 7k and mtime < 2007-01-01 and attributes = H",
                    C2 = "name != *.bin  OR  size <= 7k or mtime > 2007-01-01 or attributes != H",
                },
            };

            string[] zipFileToCreate =
            {
                Path.Combine(TopLevelDir, "Selector_AddSelectedFiles-1.zip"),
                Path.Combine(TopLevelDir, "Selector_AddSelectedFiles-2.zip"),
            };

            Assert.False(
                File.Exists(zipFileToCreate[0]),
                "The zip file '{zipFileToCreate[0]}' already exists."
            );
            Assert.False(
                File.Exists(zipFileToCreate[1]),
                "The zip file '{zipFileToCreate[1]}' already exists."
            );

            int count1,
                count2;
            string fodderDir = CreateFodderFiles(out count1, out count2);
            var topLevelFiles = Directory.GetFiles(fodderDir, "*.*", SearchOption.TopDirectoryOnly);
            string currentDir = Directory.GetCurrentDirectory();

            for (int m = 0; m < trials.Length; m++)
            {
                _output.WriteLine("===============================================");
                _output.WriteLine("AddSelectedFiles() [{0}]", trials[m].Label);
                _output.WriteLine("test {0}/{1}: creating zip #1/2", m + 1, trials.Length);
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.AddSelectedFiles(trials[m].C1, fodderDir, "");
                    zip1.Save(zipFileToCreate[0]);
                }
                count1 = CountEntries(zipFileToCreate[0]);
                _output.WriteLine("C1({0}) Count({1})", trials[m].C1, count1);

                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.AddSelectedFiles(trials[m].C2, fodderDir, "");
                    zip1.Save(zipFileToCreate[1]);
                }
                count2 = CountEntries(zipFileToCreate[1]);
                _output.WriteLine("C2({0}) Count({1})", trials[m].C2, count2);
                Assert.Equal<Int32>(topLevelFiles.Length, count1 + count2);

                /// =======================================================
                /// Now, select entries from that ZIP
                _output.WriteLine("status test {0}/{1}: selecting zip #1/2", m + 1, trials.Length);
                using (ZipFile zip1 = ZipFile.Read(zipFileToCreate[0]))
                {
                    var selected1 = zip1.SelectEntries(trials[m].C1);
                    Assert.Equal<Int32>(selected1.Count, count1);
                }
                _output.WriteLine("status test {0}/{1}: selecting zip #2/2", m + 1, trials.Length);
                using (ZipFile zip1 = ZipFile.Read(zipFileToCreate[1]))
                {
                    var selected2 = zip1.SelectEntries(trials[m].C2);
                    Assert.Equal<Int32>(selected2.Count, count2);
                }
            }
        }

        [Fact]
        public void Selector_AddSelectedFiles_2()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_AddSelectedFiles_2.zip");
            string dirToZip = Path.Combine(TopLevelDir, "parent");
            Directory.CreateDirectory(dirToZip); // there will be no files at this level
            string childDir = Path.Combine(dirToZip, "child");
            var files = TestUtilities.GenerateFilesFlat(childDir);
            var txtFiles = Directory.GetFiles(dirToZip, "*.txt", SearchOption.AllDirectories);

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("*.txt", dirToZip); // no recurse
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal(0, CountEntries(zipFileToCreate));

            // now try again, this time selecting with recurse
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("*.txt", dirToZip, true);
                zip1.Save(zipFileToCreate);
            }
            int countedEntries = CountEntries(zipFileToCreate);
            Assert.Equal(
                txtFiles.Length,
                countedEntries,
                $"expected:{txtFiles.Length}!= actual:{countedEntries}"
            );

            // now recurse from child dir
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("*.txt", childDir, true);
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal(
                txtFiles.Length,
                countedEntries,
                $"expected:{txtFiles.Length}!= actual:{countedEntries}"
            );
        }

        [Fact]
        public void Selector_AddSelectedFiles_Checkcase_file()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddSelectedFiles_Checkcase.zip");
            string dirToZip = Path.Combine(
                TopLevelDir,
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName())
            );
            var files = TestUtilities.GenerateFilesFlat(dirToZip);

            var f2 = Directory.GetFiles(dirToZip, "*.*");
            Array.ForEach(
                f2,
                x =>
                {
                    File.Move(x, Path.Combine(dirToZip, Path.GetFileName(x).ToUpper()));
                }
            );

            var txtFiles = Directory.GetFiles(dirToZip, "*.txt", SearchOption.AllDirectories);
            Assert.True(txtFiles.Length > 3, $"not enough entries (n={txtFiles.Length})");

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("*.txt", dirToZip, Path.GetFileName(dirToZip));
                zip1.Save(zipFileToCreate);
            }

            int nEntries = 0;
            // now, verify that we have not downcased the filenames
            using (ZipFile zip2 = new ZipFile(zipFileToCreate))
            {
                foreach (var entry in zip2.Entries)
                {
                    Assert.False(entry.FileName.Equals(entry.FileName.ToLower()));
                    nEntries++;
                }
            }
            Assert.False(nEntries < 2, $"not enough entries (n={nEntries})");
        }

        [Fact]
        public void Selector_AddSelectedFiles_Checkcase_directory()
        {
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "AddSelectedFiles_Checkcase_dir.zip"
            );
            string shortDir = Path.GetFileNameWithoutExtension(Path.GetRandomFileName()).ToUpper();
            string dirToZip = Path.Combine(TopLevelDir, shortDir);
            var files = TestUtilities.GenerateFilesFlat(dirToZip);
            var txtFiles = Directory.GetFiles(dirToZip, "*.txt", SearchOption.AllDirectories);

            Assert.False(txtFiles.Length < 3, $"not enough entries (n={txtFiles.Length})");

            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles("*.txt", dirToZip, Path.GetFileName(dirToZip));
                zip1.Save(zipFileToCreate);
            }

            int nEntries = 0;
            using (ZipFile zip2 = new ZipFile(zipFileToCreate))
            {
                foreach (var entry in zip2.Entries)
                {
                    Assert.False(entry.FileName.Equals(entry.FileName.ToLower()));
                    nEntries++;
                }
            }
            Assert.False(nEntries < 3, "not enough entries (n={nEntries})");
        }

        [Fact]
        public void Selector_AddSelectedFiles_Checkcase_directory_2()
        {
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "AddSelectedFiles_Checkcase_dir2.zip"
            );
            string shortDirToZip = Path.GetFileNameWithoutExtension(Path.GetRandomFileName())
                .ToUpper();
            string dirToZip = Path.Combine(TopLevelDir, shortDirToZip); // fully qualified
            var files = TestUtilities.GenerateFilesFlat(dirToZip);
            string keyword = "Ammon";
            int n = _rnd.Next(3) + 2;
            for (int i = 0; i < n; i++)
            {
                string subdir = Path.Combine(dirToZip, $"{keyword}-{i}");
                TestUtilities.GenerateFilesFlat(subdir);
                var f2 = Directory.GetFiles(subdir, "*.*");
                int k = 2;
                Array.ForEach(
                    f2,
                    x =>
                    {
                        File.Move(
                            x,
                            Path.Combine(
                                subdir,
                                String.Format("{0}.{1:D5}.txt", keyword.ToUpper(), k++)
                            )
                        );
                    }
                );
            }

            _output.WriteLine("Create zip file");
            using (ZipFile zip1 = new ZipFile())
            {
                var criterion = $"name = *\\{keyword}-?\\*.txt"; // must match subdir used aboce
                zip1.AddSelectedFiles(criterion, dirToZip, "files", true);
                zip1.Save(zipFileToCreate);
            }

            _output.WriteLine("Verify case of entry FileNames");
            int nEntries = 0;
            // now, verify that DotNetZip has not downcased entry.FileName
            using (ZipFile zip2 = new ZipFile(zipFileToCreate))
            {
                foreach (var entry in zip2.Entries)
                {
                    _output.WriteLine("Check {0}", entry.FileName);
                    Assert.NotEqual<String>(
                        entry.FileName,
                        entry.FileName.ToLower(),
                        entry.FileName
                    );
                    nEntries++;
                }
            }
            Assert.False(nEntries < 3, $"not enough entries (n={nEntries})");
        }

        [Fact]
        public void SelectEntries_FwdSlash_wi13350()
        {
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "SelectEntries_FwdSlash_wi13350.zip"
            );
            string shortDir = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            string dirToZip = Path.Combine(TopLevelDir, shortDir);
            var files = TestUtilities.GenerateFilesFlat(dirToZip);

            _output.WriteLine("Create zip file");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(dirToZip, Path.GetFileName(dirToZip));
                zip1.Save(zipFileToCreate);
            }

            // Using forward slash and backward slash should be equivalent
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                var selection1 = zip2.SelectEntries($"name = {shortDir}\\*.txt");
                //var selection1 = zip2.SelectEntries("name = *.txt");
                Assert.True(selection1.Count > 2, $"{selection1.Count} is not enough entries");
                var selection2 = zip2.SelectEntries($"name = {shortDir}/*.txt");
                Assert.Equal<int>(
                    selection1.Count,
                    selection2.Count,
                    String.Format("{0} != {1}", selection1.Count, selection2.Count)
                );
            }
        }

        [Fact]
        public void CheckRemove_wi10499()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "CheckRemove_wi10499.zip");
            string dirToZip = Path.Combine(
                TopLevelDir,
                Path.GetFileNameWithoutExtension(Path.GetRandomFileName())
            );
            var files = TestUtilities.GenerateFilesFlat(dirToZip);

            _output.WriteLine("Create zip file");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(dirToZip, Path.GetFileName(dirToZip));
                zip1.Save(zipFileToCreate);
            }

            int nBefore = 0,
                nAfter = 0,
                nRemoved = 0;
            using (ZipFile zip2 = ZipFile.Read(zipFileToCreate))
            {
                ICollection<ZipEntry> entries = zip2.SelectEntries("*.txt");
                Assert.False(entries.Count < 3, "not enough entries");
                nBefore = entries.Count;

                foreach (ZipEntry entry in entries)
                {
                    _output.WriteLine("Removing {0}", entry.FileName);
                    zip2.RemoveEntry(entry);
                    nRemoved++;
                }
                var remainingEntries = zip2.SelectEntries("*.txt");
                nAfter = remainingEntries.Count;
                _output.WriteLine("Remaining:");
                foreach (ZipEntry entry in remainingEntries)
                {
                    _output.WriteLine("  {0}", entry.FileName);
                }
            }
            Assert.True(nBefore > nAfter, "Removal appeared to have no effect.");
            Assert.True(
                nBefore - nRemoved == nAfter,
                String.Format("Wrong number of entries {0}-{1}!={2}", nBefore, nRemoved, nAfter)
            );
        }

        private enum WhichTime
        {
            atime,
            mtime,
            ctime,
        }

        private static void TouchFile(string strFile, WhichTime which, DateTime stamp)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(strFile);
            if (which == WhichTime.atime)
                fi.LastAccessTime = stamp;
            else if (which == WhichTime.ctime)
                fi.CreationTime = stamp;
            else if (which == WhichTime.mtime)
                fi.LastWriteTime = stamp;
            else
                throw new System.ArgumentException("which");
        }

        [Fact]
        public void SelectEntries_ByTime()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectEntries.zip");
            Assert.False(
                File.Exists(zipFileToCreate),
                $"The zip file '{zipFileToCreate}' already exists."
            );

            int totalFilesCreated = 0;
            int totalDirsCreated = 0;
            string fodderDir = CreateFodderFiles(out totalFilesCreated, out totalDirsCreated);

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(fodderDir, "");
                zip1.Save(zipFileToCreate);
            }

            int countedFiles = CountEntries(zipFileToCreate);
            Assert.True(countedFiles > 30);
            Assert.Equal(countedFiles, totalFilesCreated);
            int totalEntries = totalFilesCreated + totalDirsCreated;

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, SelectEntries() by date...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                // all of the files should have been modified either
                // after midnight today, or before.
                string crit = String.Format("mtime >= {0}", todayAtMidnight.ToString("yyyy-MM-dd"));
                var selected1 = zip1.SelectEntries(crit);
                _output.WriteLine("Case A({0}) count({1})", crit, selected1.Count);
                crit = String.Format("mtime < {0}", todayAtMidnight.ToString("yyyy-MM-dd"));
                var selected2 = zip1.SelectEntries(crit);

                _output.WriteLine("Case B({0})  count({1})", crit, selected2.Count);
                Assert.Equal<Int32>(
                    totalEntries,
                    selected1.Count + selected2.Count,
                    $"total({totalEntries})!={selected1.Count + selected2.Count}"
                );

                // some nonzero (high) number of files should have been
                // created in the past twenty days.
                crit = String.Format("ctime >= {0}", twentyDaysAgo.ToString("yyyy-MM-dd"));
                var selected3 = zip1.SelectEntries(crit);
                _output.WriteLine("Case C({0}) count({1})", crit, selected3.Count);
                Assert.True(selected3.Count > 0, "C");

                // a nonzero number should be marked as having been
                // created more than 3 years ago.
                crit = String.Format("ctime < {0}", threeYearsAgo.ToString("yyyy-MM-dd"));
                var selected4 = zip1.SelectEntries(crit);
                _output.WriteLine("Case D({0})  count({1})", crit, selected4.Count);
                Assert.True(selected4.Count > 0, "D");

                // None of the files should have been created before 1997.
                var manyYearsAgo = new DateTime(1997, DateTime.Now.Month, DateTime.Now.Day);
                crit = String.Format("ctime < {0}", manyYearsAgo.ToString("yyyy-MM-dd"));
                var selected5 = zip1.SelectEntries(crit);
                _output.WriteLine("Case E({0})  count({1})", crit, selected5.Count);
                Assert.True(selected5.Count == 0, "E");

                // Some number of the files should have been created
                // more than three days ago
                crit = String.Format("ctime < {0}", threeDaysAgo.ToString("yyyy-MM-dd"));
                var selected6 = zip1.SelectEntries(crit);
                _output.WriteLine("Case F({0})  count({1})", crit, selected6.Count);
                Assert.True(selected6.Count > 0, "F");

                // summing all those created more than three days ago,
                // with those created in the last three days, should be all entries.
                crit = String.Format("ctime >= {0}", threeDaysAgo.ToString("yyyy-MM-dd"));
                var selected7 = zip1.SelectEntries(crit);
                _output.WriteLine("Case G({0})  count({1})", crit, selected6.Count);
                Assert.True(selected7.Count > 0, "G");
                Assert.Equal<Int32>(totalEntries, selected6.Count + selected7.Count, "G");

                // some number should have been accessed in the past 2 days
                crit = String.Format(
                    "atime >= {0}  and  atime < {1}",
                    twoDaysAgo.ToString("yyyy-MM-dd"),
                    todayAtMidnight.ToString("yyyy-MM-dd")
                );
                selected5 = zip1.SelectEntries(crit);
                _output.WriteLine("Case H({0})  count({1})", crit, selected5.Count);
                Assert.True(selected5.Count > 0, "H");

                // those accessed *exactly* at midnight yesterday, plus
                // those NOT = all entries
                crit = String.Format("atime = {0}", yesterdayAtMidnight.ToString("yyyy-MM-dd"));
                selected5 = zip1.SelectEntries(crit);
                _output.WriteLine("Case I({0})  count({1})", crit, selected5.Count);

                crit = String.Format("atime != {0}", yesterdayAtMidnight.ToString("yyyy-MM-dd"));
                selected6 = zip1.SelectEntries(crit);
                _output.WriteLine("Case J({0})  count({1})", crit, selected6.Count);
                Assert.Equal<Int32>(totalEntries, selected5.Count + selected6.Count, "J");

                // those marked as last accessed more than 20 days ago == empty set
                crit = String.Format("atime <= {0}", twentyDaysAgo.ToString("yyyy-MM-dd"));
                selected5 = zip1.SelectEntries(crit);
                _output.WriteLine("Case K({0})  count({1})", crit, selected5.Count);
                Assert.Equal<Int32>(0, selected5.Count, "K");
            }
        }

        [Fact]
        public void Selector_ExtractSelectedEntries()
        {
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "Selector_ExtractSelectedEntries.zip"
            );
            int filesCreated,
                dirsCreated;
            string fodderDir = CreateFodderFiles(out filesCreated, out dirsCreated);
            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(fodderDir, "");
                zip1.Save(zipFileToCreate);
            }
            int numEntries = CountEntries(zipFileToCreate);
            Assert.True(numEntries > 10);

            string extractDir = Path.Combine(TopLevelDir, "extract");

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, ExtractSelectedEntries() by date...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                string crit = String.Format("mtime >= {0}", todayAtMidnight.ToString("yyyy-MM-dd"));
                _output.WriteLine("Criteria({0})", crit);
                zip1.ExtractSelectedEntries(crit, null, extractDir);
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, ExtractSelectedEntries() by date, with overwrite...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                string crit = String.Format("mtime >= {0}", todayAtMidnight.ToString("yyyy-MM-dd"));
                _output.WriteLine("Criteria({0})", crit);
                zip1.ExtractSelectedEntries(
                    crit,
                    null,
                    extractDir,
                    ExtractExistingFileAction.OverwriteSilently
                );
            }

            // workitem 9174: test ExtractSelectedEntries using a directoryPathInArchive
            List<String> dirs = new List<String>();
            // first, get the list of directories used by all entries
            _output.WriteLine("Reading zip, ...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                foreach (var e in zip1)
                {
                    _output.WriteLine("entry {0}", e.FileName);
                    string p = Path.GetDirectoryName(e.FileName.Replace("/", "\\"));
                    if (!dirs.Contains(p))
                        dirs.Add(p);
                }
            }

            // with or without trailing slash
            for (int i = 0; i < 2; i++)
            {
                int grandTotal = 0;
                extractDir = Path.Combine(TopLevelDir, $"extract.{i}");
                for (int j = 0; j < dirs.Count; j++)
                {
                    string d = dirs[j];
                    if (i == 1)
                        d += "\\";
                    _output.WriteLine("====================================================");
                    _output.WriteLine(
                        "Reading zip, ExtractSelectedEntries() by name, with directoryInArchive({0})...",
                        d
                    );
                    using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
                    {
                        string crit = "name = *.bin";
                        _output.WriteLine("Criteria({0})", crit);
                        var s = zip1.SelectEntries(crit, d);
                        _output.WriteLine("  {0} entries", s.Count);
                        grandTotal += s.Count;
                        zip1.ExtractSelectedEntries(
                            crit,
                            d,
                            extractDir,
                            ExtractExistingFileAction.OverwriteSilently
                        );
                    }
                }
                _output.WriteLine("====================================================");
                _output.WriteLine("Total for all dirs: {0} entries", grandTotal);

                var extracted = Directory.GetFiles(
                    extractDir,
                    "*.bin",
                    SearchOption.AllDirectories
                );
                Assert.Equal<Int32>(grandTotal, extracted.Length);
            }
        }

        [Fact]
        public void Selector_SelectEntries_ByName()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectEntries.zip");
            Assert.False(
                File.Exists(zipFileToCreate),
                $"The zip file '{zipFileToCreate}' already exists."
            );

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string subDir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(subDir);

            int fileCount = _rnd.Next(33) + 33;
            _output.WriteLine("====================================================");
            _output.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                // select binary or text
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(subDir, String.Format("file{0:D3}.txt", j));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(subDir, String.Format("file{0:D3}.bin", j));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                _output.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(subDir, "");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<Int32>(entriesAdded, CountEntries(zipFileToCreate));

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, SelectEntries() by name...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                var selected1 = zip1.SelectEntries("name = *.txt");
                var selected2 = zip1.SelectEntries("name = *.bin");
                var selected3 = zip1.SelectEntries("name = *.bin OR name = *.txt");
                _output.WriteLine(
                    "Found {0} text files, {0} bin files.",
                    selected1.Count,
                    selected2.Count
                );
                _output.WriteLine("Text files:");
                foreach (ZipEntry e in selected1)
                {
                    _output.WriteLine(e.FileName);
                }
                Assert.Equal<Int32>(entriesAdded, selected1.Count + selected2.Count);
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, SelectEntries() using shorthand filters...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                var selected1 = zip1.SelectEntries("*.txt");
                var selected2 = zip1.SelectEntries("*.bin");
                _output.WriteLine("Text files:");
                foreach (ZipEntry e in selected1)
                {
                    _output.WriteLine(e.FileName);
                }
                Assert.Equal<Int32>(entriesAdded, selected1.Count + selected2.Count);
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, SelectEntries() again ...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                string crit = "name = *.txt AND name = *.bin";
                // none of the entries should match this:
                var selected1 = zip1.SelectEntries(crit);
                _output.WriteLine("Criteria({0})  count({1})", crit, selected1.Count);
                Assert.Equal<Int32>(0, selected1.Count);

                // all of the entries should match this:
                crit = "name = *.txt XOR name = *.bin";
                var selected2 = zip1.SelectEntries(crit);
                _output.WriteLine("Criteria({0})  count({1})", crit, selected2.Count);
                Assert.Equal<Int32>(entriesAdded, selected2.Count);

                // try an compound criterion with XOR
                crit = "name = *.bin XOR name = *2.*";
                var selected3 = zip1.SelectEntries(crit);
                Assert.True(selected3.Count > 0);
                _output.WriteLine("Criteria({0})  count({1})", crit, selected3.Count);

                // factor out the XOR
                crit = "(name = *.bin AND name != *2.*) OR (name != *.bin AND name = *2.*)";
                var selected4 = zip1.SelectEntries(crit);
                _output.WriteLine("Criteria({0})  count({1})", crit, selected4.Count);
                Assert.Equal<Int32>(selected3.Count, selected4.Count);

                // take the negation of the XOR criterion
                crit = "(name != *.bin OR name = *2.*) AND (name = *.bin OR name != *2.*)";
                var selected5 = zip1.SelectEntries(crit);
                _output.WriteLine("Criteria({0})  count({1})", crit, selected4.Count);
                Assert.True(selected5.Count > 0);
                Assert.Equal<Int32>(entriesAdded, selected3.Count + selected5.Count);
            }
        }

        [Fact]
        public void Selector_SelectEntries_ByName_NamesWithSpaces()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectEntries_Spaces.zip");
            Assert.False(
                File.Exists(zipFileToCreate),
                $"The zip file '{zipFileToCreate}' already exists."
            );

            //int count1, count2;
            int entriesAdded = 0;
            String filename = null;

            string subDir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(subDir);

            int fileCount = _rnd.Next(44) + 44;
            _output.WriteLine("====================================================");
            _output.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(subDir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(subDir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                _output.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(subDir, "");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<Int32>(entriesAdded, CountEntries(zipFileToCreate));

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                var selected1 = zip1.SelectEntries("name = *.txt");
                var selected2 = zip1.SelectEntries("name = *.bin");
                _output.WriteLine("Text files:");
                foreach (ZipEntry e in selected1)
                {
                    _output.WriteLine(e.FileName);
                }
                Assert.Equal<Int32>(entriesAdded, selected1.Count + selected2.Count);
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, using name patterns that contain spaces...");
            string[] selectionStrings =
            {
                "name = '* *.txt'",
                "name = '* *.bin'",
                "name = *.txt and name != '* *.txt'",
                "name = *.bin and name != '* *.bin'",
            };
            int count = 0;
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                foreach (string selectionCriteria in selectionStrings)
                {
                    var selected1 = zip1.SelectEntries(selectionCriteria);
                    count += selected1.Count;
                    _output.WriteLine(
                        "  For criteria ({0}), found {1} files.",
                        selectionCriteria,
                        selected1.Count
                    );
                }
            }
            Assert.Equal<Int32>(entriesAdded, count);
        }

        [Fact]
        public void Selector_RemoveSelectedEntries_Spaces()
        {
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "Selector_RemoveSelectedEntries_Spaces.zip"
            );

            Assert.False(
                File.Exists(zipFileToCreate),
                $"The zip file '{zipFileToCreate}' already exists."
            );

            int entriesAdded = 0;
            String filename = null;

            string subDir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(subDir);

            int fileCount = _rnd.Next(44) + 44;
            _output.WriteLine("====================================================");
            _output.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(subDir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(subDir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                _output.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(subDir, "");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<Int32>(entriesAdded, CountEntries(zipFileToCreate));

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, using name patterns that contain spaces...");
            string[] selectionStrings =
            {
                "name = '* *.txt'",
                "name = '* *.bin'",
                "name = *.txt and name != '* *.txt'",
                "name = *.bin and name != '* *.bin'",
            };
            foreach (string selectionCriteria in selectionStrings)
            {
                using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
                {
                    var selected1 = zip1.SelectEntries(selectionCriteria);
                    zip1.RemoveEntries(selected1);
                    _output.WriteLine(
                        "for pattern {0}, Removed {1} entries",
                        selectionCriteria,
                        selected1.Count
                    );
                    zip1.Save();
                }
            }
            Assert.Equal<Int32>(0, CountEntries(zipFileToCreate));
        }

        [Fact]
        public void Selector_RemoveSelectedEntries2()
        {
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "Selector_RemoveSelectedEntries2.zip"
            );

            Assert.False(
                File.Exists(zipFileToCreate),
                $"The zip file '{zipFileToCreate}' already exists."
            );

            int entriesAdded = 0;
            String filename = null;

            string subDir = Path.Combine(TopLevelDir, "A");
            Directory.CreateDirectory(subDir);

            int fileCount = _rnd.Next(44) + 44;
            _output.WriteLine("====================================================");
            _output.WriteLine("Files being added to the zip:");
            for (int j = 0; j < fileCount; j++)
            {
                string space = (_rnd.Next(2) == 0) ? " " : "";
                if (_rnd.Next(2) == 0)
                {
                    filename = Path.Combine(subDir, String.Format("file{1}{0:D3}.txt", j, space));
                    TestUtilities.CreateAndFillFileText(filename, _rnd.Next(5000) + 5000);
                }
                else
                {
                    filename = Path.Combine(subDir, String.Format("file{1}{0:D3}.bin", j, space));
                    TestUtilities.CreateAndFillFileBinary(filename, _rnd.Next(5000) + 5000);
                }
                _output.WriteLine(Path.GetFileName(filename));
                entriesAdded++;
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip...");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddDirectory(subDir, "");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<Int32>(entriesAdded, CountEntries(zipFileToCreate));

            _output.WriteLine("====================================================");
            _output.WriteLine("Reading zip, using name patterns that contain spaces...");
            string[] selectionStrings =
            {
                "name = '* *.txt'",
                "name = '* *.bin'",
                "name = *.txt and name != '* *.txt'",
                "name = *.bin and name != '* *.bin'",
            };
            foreach (string selectionCriteria in selectionStrings)
            {
                using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
                {
                    var selected1 = zip1.SelectEntries(selectionCriteria);
                    ZipEntry[] entries = new ZipEntry[selected1.Count];
                    selected1.CopyTo(entries, 0);
                    string[] names = Array.ConvertAll(entries, x => x.FileName);
                    zip1.RemoveEntries(names);
                    _output.WriteLine(
                        "for pattern {0}, Removed {1} entries",
                        selectionCriteria,
                        selected1.Count
                    );
                    zip1.Save();
                }
            }

            Assert.Equal<Int32>(0, CountEntries(zipFileToCreate));
        }

        [Fact]
        public void Selector_SelectEntries_subDirs()
        {
            //Directory.SetCurrentDirectory(TopLevelDir);

            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectFiles_subDirs.zip");

            Assert.False(
                File.Exists(zipFileToCreate),
                $"The zip file '{zipFileToCreate}' already exists."
            );

            int count1,
                count2;

            string fodder = Path.Combine(TopLevelDir, "fodder");
            Directory.CreateDirectory(fodder);

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating files...");
            int entries = 0;
            int i = 0;
            int subdirCount = _rnd.Next(17) + 9;
            //int subdirCount = _rnd.Next(3) + 2;
            var FileCount = new Dictionary<string, int>();

            var checksums = new Dictionary<string, string>();
            // I don't actually verify the checksums in this method...

            for (i = 0; i < subdirCount; i++)
            {
                string subDirShort = new System.String(new char[] { (char)(i + 65) });
                string subDir = Path.Combine(fodder, subDirShort);
                Directory.CreateDirectory(subDir);

                int filecount = _rnd.Next(8) + 8;
                //int filecount = _rnd.Next(2) + 2;
                FileCount[subDirShort] = filecount;
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = Path.Combine(subDir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 1000);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var s = TestUtilities.CheckSumToString(chk);
                    var t1 = Path.GetFileName(fodder);
                    var t2 = Path.Combine(t1, subDirShort);
                    var key = Path.Combine(t2, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    _output.WriteLine("chk[{0}]= {1}", key, s);
                    checksums.Add(key, s);
                    entries++;
                }
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip ({0} entries in {1} subdirs)...", entries, subdirCount);
            // add all the subdirectories into a new zip
            using (ZipFile zip1 = new ZipFile())
            {
                // add all of those subdirectories (A, B, C...) into the root in the zip archive
                zip1.AddDirectory(fodder, "");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<Int32>(entries, CountEntries(zipFileToCreate));

            _output.WriteLine("====================================================");
            _output.WriteLine("Selecting entries by directory...");

            for (int j = 0; j < 2; j++)
            {
                count1 = 0;
                using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
                {
                    for (i = 0; i < subdirCount; i++)
                    {
                        string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                        if (j == 1)
                            dirInArchive += "\\";
                        var selected1 = zip1.SelectEntries("*.*", dirInArchive);
                        count1 += selected1.Count;
                        _output.WriteLine(
                            "--------------\nfiles in dir {0} ({1}):",
                            dirInArchive,
                            selected1.Count
                        );
                        foreach (ZipEntry e in selected1)
                            _output.WriteLine(e.FileName);
                    }
                    Assert.Equal<Int32>(entries, count1);
                }
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Selecting entries by directory and size...");
            count1 = 0;
            count2 = 0;
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                for (i = 0; i < subdirCount; i++)
                {
                    string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                    var selected1 = zip1.SelectEntries("size > 1500", dirInArchive);
                    count1 += selected1.Count;
                    _output.WriteLine(
                        "--------------\nfiles in dir {0} ({1}):",
                        dirInArchive,
                        selected1.Count
                    );
                    foreach (ZipEntry e in selected1)
                        _output.WriteLine(e.FileName);
                }

                var selected2 = zip1.SelectEntries("size <= 1500");
                count2 = selected2.Count;
                Assert.Equal<Int32>(entries, count1 + count2 - subdirCount);
            }
        }

        [Fact]
        public void Selector_SelectEntries_Fullpath()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_SelectFiles_Fullpath.zip");
            Assert.False(
                File.Exists(zipFileToCreate),
                $"The zip file '{zipFileToCreate}' already exists."
            );

            int count1,
                count2;

            string fodder = Path.Combine(TopLevelDir, "fodder");
            Directory.CreateDirectory(fodder);

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating files...");
            int entries = 0;
            int i = 0;
            int subdirCount = _rnd.Next(17) + 9;
            //int subdirCount = _rnd.Next(3) + 2;
            var FileCount = new Dictionary<string, int>();

            var checksums = new Dictionary<string, string>();
            // I don't actually verify the checksums in this method...

            for (i = 0; i < subdirCount; i++)
            {
                string subDirShort = new System.String(new char[] { (char)(i + 65) });
                string subDir = Path.Combine(fodder, subDirShort);
                Directory.CreateDirectory(subDir);

                int filecount = _rnd.Next(8) + 8;
                //int filecount = _rnd.Next(2) + 2;
                FileCount[subDirShort] = filecount;
                for (int j = 0; j < filecount; j++)
                {
                    string filename = String.Format("file{0:D4}.x", j);
                    string fqFilename = Path.Combine(subDir, filename);
                    TestUtilities.CreateAndFillFile(fqFilename, _rnd.Next(1000) + 1000);

                    var chk = TestUtilities.ComputeChecksum(fqFilename);
                    var s = TestUtilities.CheckSumToString(chk);
                    var t1 = Path.GetFileName(fodder);
                    var t2 = Path.Combine(t1, subDirShort);
                    var key = Path.Combine(t2, filename);
                    key = TestUtilities.TrimVolumeAndSwapSlashes(key);
                    _output.WriteLine("chk[{0}]= {1}", key, s);
                    checksums.Add(key, s);
                    entries++;
                }
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip ({0} entries in {1} subdirs)...", entries, subdirCount);
            // add all the subdirectories into a new zip
            using (ZipFile zip1 = new ZipFile())
            {
                // add all of those subdirectories (A, B, C...) into the root in the zip archive
                zip1.AddDirectory(fodder, "");
                zip1.Save(zipFileToCreate);
            }
            Assert.Equal<Int32>(entries, CountEntries(zipFileToCreate));

            _output.WriteLine("====================================================");
            _output.WriteLine("Selecting entries by full path...");
            count1 = 0;
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                for (i = 0; i < subdirCount; i++)
                {
                    string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                    var selected1 = zip1.SelectEntries(Path.Combine(dirInArchive, "*.*"));
                    count1 += selected1.Count;
                    _output.WriteLine(
                        "--------------\nfiles in dir {0} ({1}):",
                        dirInArchive,
                        selected1.Count
                    );
                    foreach (ZipEntry e in selected1)
                        _output.WriteLine(e.FileName);
                }
                Assert.Equal<Int32>(entries, count1);
            }

            _output.WriteLine("====================================================");
            _output.WriteLine("Selecting entries by directory and size...");
            count1 = 0;
            count2 = 0;
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                for (i = 0; i < subdirCount; i++)
                {
                    string dirInArchive = new System.String(new char[] { (char)(i + 65) });
                    string pathCriterion = String.Format(
                        "name = {0}",
                        Path.Combine(dirInArchive, "*.*")
                    );
                    string combinedCriterion = String.Format("size > 1500  AND {0}", pathCriterion);

                    var selected1 = zip1.SelectEntries(combinedCriterion, dirInArchive);
                    count1 += selected1.Count;
                    _output.WriteLine(
                        "--------------\nfiles in ({0}) ({1} entries):",
                        combinedCriterion,
                        selected1.Count
                    );
                    foreach (ZipEntry e in selected1)
                        _output.WriteLine(e.FileName);
                }

                var selected2 = zip1.SelectEntries("size <= 1500");
                count2 = selected2.Count;
                Assert.Equal<Int32>(entries, count1 + count2 - subdirCount);
            }
        }

        [Fact]
        public void Selector_SelectEntries_NestedDirectories_wi8559()
        {
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "Selector_SelectFiles_NestedDirectories.zip"
            );

            _output.WriteLine("====================================================");
            _output.WriteLine("Creating zip file...");

            int dirCount = _rnd.Next(4) + 3;
            using (var zip = new ZipFile())
            {
                for (int i = 0; i < dirCount; i++)
                {
                    String dir = new String((char)(65 + i), i + 1);
                    zip.AddEntry(
                        Path.Combine(dir, "Readme.txt"),
                        "This is the content for the Readme.txt in directory " + dir
                    );
                    int subDirCount = _rnd.Next(3) + 2;
                    for (int j = 0; j < subDirCount; j++)
                    {
                        String subdir = Path.Combine(dir, new String((char)(90 - j), 3));
                        zip.AddEntry(
                            Path.Combine(subdir, "Readme.txt"),
                            "This is the content for the Readme.txt in directory " + subdir
                        );
                    }
                }
                zip.Save(zipFileToCreate);
            }

            // This test method does not extract files, or verify checksums ...
            // Just want to verify that selection of entries works in nested directories.
            _output.WriteLine("====================================================");
            _output.WriteLine("Selecting entries by path...");
            using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
            {
                for (int i = 0; i < dirCount; i++)
                {
                    String dir = new String((char)(65 + i), i + 1);
                    var selected1 = zip1.SelectEntries("*.txt", dir);
                    Assert.Equal<Int32>(1, selected1.Count);

                    selected1 = zip1.SelectEntries("*.txt", dir + "/ZZZ");
                    var selected2 = zip1.SelectEntries("*.txt", dir + "\\ZZZ");
                    Assert.Equal<Int32>(selected1.Count, selected2.Count);

                    selected1 = zip1.SelectEntries("*.txt", dir + "/YYY");
                    selected2 = zip1.SelectEntries("*.txt", dir + "\\YYY");
                    Assert.Equal<Int32>(selected1.Count, selected2.Count);
                }
            }
        }

        [Fact]
        public void Selector_SelectFiles_DirName_wi8245()
        {
            // workitem 8245
            //Directory.SetCurrentDirectory(TopLevelDir);
            int filesCreated,
                dirsCreated;
            string fodderDir = CreateFodderFiles(out filesCreated, out dirsCreated);
            var ff = new Ionic.FileSelector("*.*");
            var result = ff.SelectFiles(fodderDir);
            Assert.True(result.Count > 1);
        }

        [Fact]
        public void Selector_SelectFiles_DirName_wi8245_2()
        {
            // workitem 8245
            string zipFileToCreate = Path.Combine(
                TopLevelDir,
                "Selector_SelectFiles_DirName_wi8245_2.zip"
            );
            int filesCreated,
                dirsCreated;
            string fodderDir = CreateFodderFiles(out filesCreated, out dirsCreated);
            var fodderFiles = Directory.GetFiles(fodderDir, "*.*", SearchOption.AllDirectories);

            _output.WriteLine("===============================================");
            _output.WriteLine("AddSelectedFiles()");
            using (ZipFile zip1 = new ZipFile())
            {
                zip1.AddSelectedFiles(fodderDir, null, "fodder", true);
                zip1.Save(zipFileToCreate);
            }

            Assert.Equal<Int32>(
                CountEntries(zipFileToCreate),
                fodderFiles.Length,
                "The Zip file has the wrong number of entries."
            );
        }

        [Fact]
        public void Selector_SelectFiles_DirName_wi9176()
        {
            _output.WriteLine("** Selector_SelectFiles_DirName_wi9176()");
            // workitem 9176
            int filesCreated,
                dirsCreated;
            string fodderDir = CreateFodderFiles(out filesCreated, out dirsCreated);
            _output.WriteLine("** fodderDir: {0}", fodderDir);
            var binFiles = Directory.GetFiles(fodderDir, "*.bin", SearchOption.AllDirectories);
            int[] eCount = new int[2];
            for (int i = 0; i < 2; i++)
            {
                string zipFileToCreate = Path.Combine(
                    TopLevelDir,
                    $"Selector_SelectFiles_DirName_wi9176-{i}.zip"
                );
                string d = fodderDir;
                if (i == 1)
                    d += "\\";
                _output.WriteLine("===============================================");
                _output.WriteLine("AddSelectedFiles(cycle={0})", i);
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.AddSelectedFiles("name = *.bin", d, "", true);
                    zip1.Save(zipFileToCreate);
                }

                Assert.Equal<Int32>(
                    binFiles.Length,
                    CountEntries(zipFileToCreate),
                    "The Zip file has the wrong number of entries."
                );

                using (ZipFile zip1 = ZipFile.Read(zipFileToCreate))
                {
                    foreach (var e in zip1)
                    {
                        if (e.FileName.Contains("/"))
                            eCount[i]++;
                    }
                }
                if (i == 1)
                    Assert.Equal<Int32>(
                        eCount[0],
                        eCount[1],
                        "Inconsistent results when the directory includes a path."
                    );
            }
        }

        [Fact]
        public void Selector_SelectFiles_GoodSyntax01()
        {
            string[] criteria =
            {
                "type = D",
                "type = F",
                "attrs = HRS",
                "attrs = L",
                "name = *.txt  OR (size > 7800)",
                "name = *.harvey  OR  (size > 7800  and attributes = H)",
                "(name = *.harvey)  OR  (size > 7800  and attributes = H)",
                "(name = *.xls)  and (name != *.xls)  OR  (size > 7800  and attributes = H)",
                "(name = '*.xls')",
                "(name = Ionic.Zip.dll) or ((size > 1mb) and (name != *.zip))",
                "(name = Ionic.Zip.dll) or ((size > 1mb) and (name != *.zip)) or (name = Joe.txt)",
                "(name=Ionic.Zip.dll) or ((size>1mb) and (name!=*.zip)) or (name=Joe.txt)",
                "(name=Ionic.Zip.dll)or((size>1mb)and(name!=*.zip))or(name=Joe.txt)",
            };

            foreach (string s in criteria)
            {
                _output.WriteLine("Selector: " + s);
                var ff = new Ionic.FileSelector(s);
            }
        }

        [Fact]
        public void Twiddle_wi10153()
        {
            // workitem 10153:
            //
            // When calling AddSelectedFiles(String,String,String,bool), and when the
            // actual filesystem path uses mixed case, but the specified directoryOnDisk
            // argument is downcased, AND when the filename contains a ~ (weird, I
            // know), verify that the path replacement works as advertised, and entries
            // are rooted in the directoryInArchive specified path.

            string zipFileToCreate = Path.Combine(TopLevelDir, "Selector_Twiddle.zip");
            string dirToZip = Path.Combine(TopLevelDir, "dirToZip");
            var keyword = "Gamma";
            var files = TestUtilities.GenerateFilesFlat(dirToZip);
            int k = 0;

            Array.ForEach(
                files,
                x =>
                {
                    File.Move(
                        x,
                        Path.Combine(dirToZip, String.Format("~{0}.{1:D5}.txt", keyword, k++))
                    );
                }
            );

            using (ZipFile zip = new ZipFile())
            {
                // must use ToLower to force case mismatch
                zip.AddSelectedFiles("name != *.zip*", dirToZip.ToLower(), "", true);
                zip.Save(zipFileToCreate);
            }

            int nEntries = 0;
            using (ZipFile zip = ZipFile.Read(zipFileToCreate))
            {
                foreach (var e in zip)
                    _output.WriteLine("entry {0}", e.FileName);

                _output.WriteLine("");

                foreach (var e in zip)
                {
                    _output.WriteLine("check {0}", e.FileName);
                    Assert.False(
                        e.FileName.Contains("/"),
                        "The filename contains a path, but should not"
                    );
                    nEntries++;
                }
            }
            Assert.True(nEntries > 2, "Not enough entries");
        }

        [Fact]
        public void Selector_SelectFiles_BadNoun()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("fame = *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax01()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("size = ");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax02()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name = *.txt and");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax03()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name = *.txt  URF ");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax04()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name = *.txt  OR (");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax05()
        {
            Assert.Throws<FormatException>(() =>
            {
                new Ionic.FileSelector("name = *.txt  OR (size = G)");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax06()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name = *.txt  OR (size > )");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax07()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name = *.txt  OR (size > 7800");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax08()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name = *.txt  OR )size > 7800");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax09()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name = *.txt and  name =");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax10()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name == *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax10a()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name >= *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax11()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name ~= *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax12()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name @ = *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax13()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name LIKE  *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax14()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name AND  *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax15()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("name (AND  *.txt");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax16()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("mtime 2007-01-01");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax17()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("size 1kb");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax18()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Ionic.FileSelector ff = new Ionic.FileSelector("");
                var list = ff.SelectFiles(".");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax19()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Ionic.FileSelector ff = new Ionic.FileSelector(null);
                var list = ff.SelectFiles(".");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax20()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes > HRTS");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax21()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes HRTS");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax22a()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes = HHHA");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax22b()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes = SHSA");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax22c()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes = AHA");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax22d()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes = RRA");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax22e()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes = IRIA");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax23()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("attributes = INVALID");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax24a()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("type = I");
            });
        }

        [Fact]
        public void Selector_SelectFiles_BadSyntax24b()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Ionic.FileSelector("type > D");
            });
        }

        [Fact]
        public void Selector_Normalize()
        {
            string[][] sPairs =
            {
                new string[] { "name = '.\\Selector (this is a Test)\\this.txt'", null },
                new string[]
                {
                    "(size > 100)AND(name='Name (with Parens).txt')",
                    "(size > 100 AND name = 'Name (with Parens).txt')",
                },
                new string[]
                {
                    "(size > 100) AND ((name='Name (with Parens).txt')OR(name=*.jpg))",
                    "(size > 100 AND (name = 'Name (with Parens).txt' OR name = '*.jpg'))",
                },
                new string[]
                {
                    "name='*.txt' and name!='* *.txt'",
                    "(name = '*.txt' AND name != '* *.txt')",
                },
                new string[]
                {
                    "name = *.txt AND name != '* *.txt'",
                    "(name = '*.txt' AND name != '* *.txt')",
                },
            };

            for (int i = 0; i < sPairs.Length; i++)
            {
                var pair = sPairs[i];
                var selector = pair[0];
                var expectedResult = pair[1];
                var fsel = new FileSelector(selector);
                var stringVer = fsel.ToString().Replace("\u00006", " ");
                Assert.Equal<string>(
                    $"FileSelector({(expectedResult ?? selector)})",
                    stringVer,
                    "entry {i}"
                );
            }
        }

        [Fact]
        public void SingleQuotesAndSlashes_wi14033()
        {
            var zipFileToCreate = Path.Combine(TopLevelDir, "SingleQuotesAndSlashes.zip");
            var parentDir = Path.Combine(TopLevelDir, "DexMik");

            int nFolders = this._rnd.Next(4) + 3;
            _output.WriteLine("Creating {0} folders:", nFolders);
            Directory.CreateDirectory(parentDir);
            string[] childFolders = new string[nFolders + 1];
            childFolders[0] = parentDir;
            for (int i = 0; i < nFolders; i++)
            {
                var b1 = "folder" + (i + 1);
                int k = (i > 0) ? this._rnd.Next(i + 1) : 0;
                var d1 = Path.Combine(childFolders[k], b1);
                _output.WriteLine("  {0}", d1);
                Directory.CreateDirectory(d1);
                childFolders[i + 1] = d1;

                int nFiles = this._rnd.Next(3) + 2;
                _output.WriteLine("  Creating {0} files:", nFiles);
                for (int j = 0; j < nFiles; j++)
                {
                    var fn1 = Path.GetRandomFileName();
                    var fname = Path.Combine(d1, fn1);
                    _output.WriteLine("    {0}", fn1);
                    TestUtilities.CreateAndFillFileText(fname, this._rnd.Next(10000) + 1000);
                }
                _output.WriteLine("");
            }

            // create a zip file using those files
            _output.WriteLine("");
            _output.WriteLine("Zipping:");
            using (var zip = new ZipFile())
            {
                zip.AddDirectory(parentDir, Path.GetFileName(childFolders[0]));
                zip.Save(zipFileToCreate);
            }

            // list all the entries
            _output.WriteLine("");
            _output.WriteLine("List of entries:");
            using (var zip = new ZipFile(zipFileToCreate))
            {
                foreach (var e in zip)
                {
                    _output.WriteLine("  {0}", e.FileName);
                }
            }
            _output.WriteLine("");

            // now select some of the entries
            int m = this._rnd.Next(nFolders) + 1;
            _output.WriteLine("");
            _output.WriteLine("Selecting entries from folder {0}:", m);
            using (var zip = new ZipFile(zipFileToCreate))
            {
                // TODO: maybe replace with file.Separator
                string selectCriteria = String.Format(
                    "name = '{0}/*.*'",
                    childFolders[m].Replace(TopLevelDir + "\\", "")
                );

                _output.WriteLine("select:  {0}", selectCriteria);
                var selection1 = zip.SelectEntries(selectCriteria);
                Assert.True(selection1.Count > 0, "first selection failed.");

                foreach (var item in selection1)
                {
                    _output.WriteLine("  {0}", item);
                }

                // Try different formats of the selection string - with
                // and without quotes, with fwd slashes and back
                // slashes.
                string[][] replacementPairs =
                {
                    new string[] { "\\", "/" }, // backslash to fwdslash
                    new string[] { "'", "" }, // remove single quotes
                    new string[] { "/", "\\" }, // fwdslash to backslash
                };

                for (int k = 0; k < 3; k++)
                {
                    selectCriteria = selectCriteria.Replace(
                        replacementPairs[k][0],
                        replacementPairs[k][1]
                    );

                    _output.WriteLine("");
                    _output.WriteLine("Try #{0}: {1}", k + 2, selectCriteria);
                    var selection2 = zip.SelectEntries(selectCriteria);
                    foreach (var item in selection2)
                    {
                        _output.WriteLine("  {0}", item);
                    }
                    Assert.Equal<int>(
                        selection1.Count,
                        selection2.Count,
                        $"selection verification trial {k} failed."
                    );
                }
            }
        }
    }
}
