// PasswordTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2008-2011, 2025 Dino Chiesa .
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// This module provides tests for password features.
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
    public class PasswordTests : IonicTestClass
    {
        public PasswordTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BasicAddAndExtract()
        {
            string marker = TestUtilities.GetMarker();
            string[] Passwords = { null, "Password!", TestUtilities.GenerateRandomPassword(), "A" };

            Ionic.Zlib.CompressionLevel[] compressionLevelOptions = {
                Ionic.Zlib.CompressionLevel.None,
                Ionic.Zlib.CompressionLevel.BestSpeed,
                Ionic.Zlib.CompressionLevel.Default,
                Ionic.Zlib.CompressionLevel.BestCompression,
            };

            _output.WriteLine("\n---------------------\nBasicAddAndExtract. creating files and computing checksums...");
            string dirToZip = Path.Combine(TopLevelDir, $"zipthis-{marker}");
            Directory.CreateDirectory(dirToZip);

            int numFilesToCreate = _rnd.Next(10) + 10;
            string[] filenames = new string[numFilesToCreate];
            var checksums = new Dictionary<string, string>();
            for (int i = 0; i < numFilesToCreate; i++)
            {
                filenames[i] = Path.Combine(dirToZip, String.Format("file{0:D3}.txt", i));
                int sz = _rnd.Next(22000) + 3000;
                var repeatedLine = String.Format("Line to Repeat... {0} filename: {1}", i, filenames[i]);
                TestUtilities.CreateAndFillFileText(filenames[i], repeatedLine, sz);
                string key = Path.GetFileName(filenames[i]);
                checksums.Add(key, TestUtilities.GetCheckSumString(filenames[i]));
                _output.WriteLine("  chk[{0}]={1}", key, checksums[key]);
            }

            for (int k = 0; k < compressionLevelOptions.Length; k++)
            {
                for (int j = 0; j < Passwords.Length; j++)
                {
                    _output.WriteLine("\n\n===================\nTrial ({0}) pw({1})", j, Passwords[j]);
                    string zipFileToCreate = Path.Combine(TopLevelDir, String.Format("Password_BasicAddAndExtract-{0}-{1}.zip", k, j));
                    Assert.False(File.Exists(zipFileToCreate), $"The temporary zip file '{zipFileToCreate}' already exists.");

                    _output.WriteLine("\n---------------------adding files to the archive {0}...", zipFileToCreate);

                    var sw = new StringWriter();
                    using (ZipFile zip = new ZipFile(zipFileToCreate, sw))
                    {
                        zip.CompressionLevel = compressionLevelOptions[k];
                        zip.Password = Passwords[j];
                        zip.AddDirectory(dirToZip, "");
                        zip.Save();
                    }
                    _output.WriteLine(sw.ToString());

                    Assert.Equal(numFilesToCreate, CountEntries(zipFileToCreate),
                            "The Zip file has an unexpected number of entries.");

                    _output.WriteLine("\n---------------------verifying checksums...");

                    using (ZipFile zip = ZipFile.Read(zipFileToCreate))
                    {
                        foreach (ZipEntry e in zip)
                            _output.WriteLine("found entry: {0}", e.FileName);

                        var extractDir = Path.Combine(TopLevelDir, String.Format("extract-{0}-{1}", k, j));
                        _output.WriteLine("  Extract with pw({0})", Passwords[j]);
                        foreach (ZipEntry e in zip)
                        {
                            e.ExtractWithPassword(extractDir, ExtractExistingFileAction.OverwriteSilently, Passwords[j]);
                            if (!e.IsDirectory)
                            {
                                byte[] c2 = TestUtilities.ComputeChecksum(Path.Combine(extractDir, e.FileName));
                                Assert.Equal<string>(checksums[e.FileName],
                                        TestUtilities.CheckSumToString(c2), "The checksum of the extracted file is incorrect.");
                            }
                        }
                    }
                    _output.WriteLine("\n");
                }
            }
        }



        [Fact]
        public void CheckZipPassword_wi13664()
        {
            string[] passwords = { null,
                                   "Password!",
                                   TestUtilities.GenerateRandomPassword(),
                                   "_" };

            string dirToZip = Path.Combine(TopLevelDir, "zipthis");
            int subdirCount;
            int entries = TestUtilities.GenerateFilesOneLevelDeep
                (_output, "wi13664", dirToZip, null, out subdirCount);
            string[] filesToZip = Directory.GetFiles(Path.Combine(TopLevelDir, "zipthis"), "*.*", SearchOption.AllDirectories);

            Assert.Equal<int>(filesToZip.Length, entries, "Incorrect number of entries in the directory.");

            for (int j = 0; j < passwords.Length; j++)
            {
                string zipFileToCreate = Path.Combine(TopLevelDir, $"Password_CheckZipPassword_wi13664-{j}.zip");

                // Create the zip archive
                using (ZipFile zip1 = new ZipFile())
                {
                    zip1.Password = passwords[j];
                    zip1.AddFiles(filesToZip, true, "");
                    zip1.Save(zipFileToCreate);
                }

                var r = ZipFile.CheckZipPassword(zipFileToCreate, passwords[j]);
                Assert.True(r, $"Bad password in round {j}");
            }
        }


        [Fact]
        public void UnsetEncryptionAfterSetPassword_wi13909_ZOS()
        {
            // Verify that unsetting the Encryption property after
            // setting a Password results in no encryption being used.
            // This method tests ZipOutputStream.
            string unusedPassword = TestUtilities.GenerateRandomPassword();
            int numTotalEntries = _rnd.Next(46)+653;
            string zipFileToCreate = Path.Combine(TopLevelDir, "UnsetEncryption.zip");
            string entryName = null;
            byte[] buffer = null;

            using (FileStream fs = File.Create(zipFileToCreate))
            {
                using (var zos = new ZipOutputStream(fs))
                {
                    entryName = "UnsetEncryptionAfterSetPassword_wi13909_ZOS.README.txt";
                    zos.PutNextEntry(entryName);
                    buffer = System.Text.Encoding.ASCII.GetBytes("marker text");
                    zos.Write(buffer, 0, buffer.Length);

                    zos.Password = unusedPassword;
                    zos.Encryption = EncryptionAlgorithm.None;

                    for (int i=0; i < numTotalEntries; i++)
                    {
                        if (_rnd.Next(7)==0)
                        {
                            entryName = String.Format("{0:D5}/", i);
                            zos.PutNextEntry(entryName);
                        }
                        else
                        {
                            entryName = String.Format("{0:D5}.txt", i);
                            zos.PutNextEntry(entryName);
                            if (_rnd.Next(12)==0)
                            {
                                var block = TestUtilities.GenerateRandomAsciiString() + " ";
                                string contentBuffer = String.Format("This is the content for entry {0}", i);
                                int n = _rnd.Next(6) + 2;
                                for (int j=0; j < n; j++)
                                    contentBuffer += block;
                                 buffer = System.Text.Encoding.ASCII.GetBytes(contentBuffer);
                                zos.Write(buffer, 0, buffer.Length);
                            }
                        }
                    }
                }
            }

            BasicVerifyZip(zipFileToCreate);
        }



        [Fact]
        public void UnsetEncryptionAfterSetPassword_wi13909_ZF()
        {
            // Verify that unsetting the Encryption property after
            // setting a Password results in no encryption being used.
            // This method tests ZipFile.
            string unusedPassword = TestUtilities.GenerateRandomPassword();
            int numTotalEntries = _rnd.Next(46)+653;
            string zipFileToCreate = Path.Combine(TopLevelDir, "UnsetEncryption.zip");

            using (var zip = new ZipFile())
            {
                zip.Password = unusedPassword;
                zip.Encryption = EncryptionAlgorithm.None;

                for (int i=0; i < numTotalEntries; i++)
                {
                    if (_rnd.Next(7)==0)
                    {
                        string entryName = String.Format("{0:D5}", i);
                        zip.AddDirectoryByName(entryName);
                    }
                    else
                    {
                        string entryName = String.Format("{0:D5}.txt", i);
                        if (_rnd.Next(12)==0)
                        {
                            var block = TestUtilities.GenerateRandomAsciiString() + " ";
                            string contentBuffer = String.Format("This is the content for entry {0}", i);
                                int n = _rnd.Next(6) + 2;
                                for (int j=0; j < n; j++)
                                    contentBuffer += block;
                            byte[] buffer = System.Text.Encoding.ASCII.GetBytes(contentBuffer);
                            zip.AddEntry(entryName, contentBuffer);
                        }
                        else
                            zip.AddEntry(entryName, Stream.Null);
                    }
                }
                zip.Save(zipFileToCreate);
            }

            BasicVerifyZip(zipFileToCreate);
        }


        [Fact]
        public void CheckBadPassword_wi13668()
        {
            _output.WriteLine("Password_CheckBadPassword_wi13668()");
            // In this case, the password is "correct" but the decrypted
            // header does not match the CRC. Therefore the library
            // should fail this password.  I don't know how the zip was
            // constructed but I suspect a broken library.
            string fileName = _GetNameForZipContentFile("wi13668-bad-pwd-472713.zip");
            string password = "472713";
            _output.WriteLine("Reading zip file: '{0}'", fileName);
            Assert.Throws<Ionic.Zip.BadPasswordException>(() => {
            using (ZipFile zip = ZipFile.Read(fileName))
            {
                foreach (ZipEntry e in zip)
                {
                    // will throw if wrong password
                    e.ExtractWithPassword(Stream.Null, password);
                }
            }
            });
        }

        private string _GetNameForZipContentFile(string shortFileName)
        {
            return Path.Combine(TestUtilities.GetTestSrcDir(), "zips", shortFileName);
        }


        [Fact]
        public void MultipleEntriesDifferentPasswords()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "Password_MultipleEntriesDifferentPasswords.zip");
            Assert.False(File.Exists(ZipFileToCreate), $"The temporary zip file '{ZipFileToCreate}' already exists.");
            string dnzSolutionDir =
                Path.GetFullPath(Path.Combine(TestUtilities.GetTestSrcDir(), ".."));

            string[] filenames =
            {
                Path.Combine(dnzSolutionDir, "Tools\\Zipit\\bin\\Debug\\net9.0\\Zipit.exe"),
                Path.Combine(dnzSolutionDir, "Zip\\ZipFile.cs"),
            };

            foreach (string f in filenames) {
                Assert.True(File.Exists(f), $"file {f} does not exist");
            }

            string[] checksums =
            {
                TestUtilities.GetCheckSumString(filenames[0]),
                TestUtilities.GetCheckSumString(filenames[1]),
            };

            string[] passwords =
            {
                    "12345678",
                    "0987654321",
            };

            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip.Password = passwords[j];
                    zip.AddFile(filenames[j], "");
                }
                zip.Save();
            }

            Assert.Equal<int>(filenames.Length, CountEntries(ZipFileToCreate),
                    "The zip file created has the wrong number of entries.");

            string unpackDir = Path.Combine(TopLevelDir, "unpack");
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip[Path.GetFileName(filenames[j])].ExtractWithPassword(unpackDir, ExtractExistingFileAction.OverwriteSilently, passwords[j]);
                    string newpath = Path.Combine(unpackDir, filenames[j]);
                    Assert.True(File.Exists(newpath), $"extracted file {newpath}");
                    string chk = TestUtilities.GetCheckSumString(newpath);
                    Assert.Equal(checksums[j], chk, "File checksums do not match.");
                }
            }
        }


        [Fact]
        public void Extract_WrongPassword()
        {
            string ZipFileToCreate = Path.Combine(TopLevelDir, "MultipleEntriesDifferentPasswords.zip");
            Assert.False(File.Exists(ZipFileToCreate), $"The temporary zip file '{ZipFileToCreate}' already exists.");

            string dnzSolutionDir =
                Path.GetFullPath(Path.Combine(TestUtilities.GetTestSrcDir(), ".."));

            string[] filenames =
            {
                Path.Combine(dnzSolutionDir, "Tools\\Zipit\\bin\\Debug\\net9.0\\Zipit.exe"),
                Path.Combine(dnzSolutionDir, "Zip\\Zipfile.cs"),
            };

            foreach (string f in filenames) {
                Assert.True(File.Exists(f), $"file {f} does not exist");
            }

            string[] passwords =
            {
                    "12345678",
                    "0987654321",
            };

            int j = 0;
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    zip.Password = passwords[j];
                    zip.AddFile(filenames[j], "");
                }
                zip.Save();
            }

            // now try to extract
            string unpackDir = Path.Combine(TopLevelDir, "unpack");
            Assert.Throws<Ionic.Zip.BadPasswordException>(() => {
            using (ZipFile zip = new ZipFile(ZipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++) {
                    zip[Path.GetFileName(filenames[j])]
                      .ExtractWithPassword(unpackDir, ExtractExistingFileAction.OverwriteSilently, "WrongPassword");
                }
            }
            });
        }


        [Fact]
        public void AddEntryWithPasswordToExistingZip()
        {
            string marker = TestUtilities.GetMarker();
            string zipFileToCreate = Path.Combine(TopLevelDir, "AddEntryWithPasswordToExistingZip.zip");
            string dnzSolutionDir =
                Path.GetFullPath(Path.Combine(TestUtilities.GetTestSrcDir(), ".."));

            string[] filenames =
            {
                Path.Combine(dnzSolutionDir, "Tools\\GZip\\GZip.cs"),
                Path.Combine(dnzSolutionDir, "Zip\\bin\\Debug\\net9.0\\Ionic.Zip.pdb"),
            };

            foreach (string f in filenames) {
                Assert.True(File.Exists(f), $"file {f} does not exist");
            }

            string[] checksums =
            {
                TestUtilities.GetCheckSumString(filenames[0]),
                TestUtilities.GetCheckSumString(filenames[1]),
            };

            int j = 0;
            using (ZipFile zip = new ZipFile(zipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                    zip.AddFile(filenames[j], "");
                zip.Save();
            }

            Assert.Equal<int>(2, CountEntries(zipFileToCreate), "wrong number of entries.");

            string additionalFile = Path.Combine(dnzSolutionDir, "Tools\\UnZip\\bin\\Debug\\net9.0\\unzip.exe");
            Assert.True(File.Exists(additionalFile), $"additional file {additionalFile} does not exist");
            string checksumX = TestUtilities.GetCheckSumString(additionalFile);
            string password = TestUtilities.GenerateRandomPassword() + "!";
            using (ZipFile zip = new ZipFile(zipFileToCreate))
            {
                zip.Password = password;
                zip.AddFile(additionalFile, "");
                zip.Save();
            }

            Assert.Equal<int>(3, CountEntries(zipFileToCreate), "wrong number of entries.");

            string unpackDir = Path.Combine(TopLevelDir, $"unpack-{marker}");
            string newpath, chk, baseName;
            using (ZipFile zip = new ZipFile(zipFileToCreate))
            {
                for (j = 0; j < filenames.Length; j++)
                {
                    baseName = Path.GetFileName(filenames[j]);
                    zip[baseName].Extract(unpackDir, ExtractExistingFileAction.OverwriteSilently);
                    newpath = Path.Combine(unpackDir, filenames[j]);
                    chk = TestUtilities.GetCheckSumString(newpath);
                    Assert.Equal<string>(checksums[j], chk, "Checksums do not match.");
                }

                baseName = Path.GetFileName(additionalFile);

                zip[baseName].ExtractWithPassword(unpackDir,
                                                  ExtractExistingFileAction.OverwriteSilently,
                                                  password);

                newpath = Path.Combine(unpackDir, baseName);
                chk = TestUtilities.GetCheckSumString(newpath);
                Assert.Equal<string>(checksumX, chk, "Checksums do not match.");
            }
        }

        [Fact]
        public void SilentDeletion_wi10639()
        {
            string zipFileToCreate = Path.Combine(TopLevelDir, "SilentDeletion.zip");
            string dirToZip = Path.Combine(TopLevelDir, "dirToZip");
            string extractDir = Path.Combine(TopLevelDir, "extracted");
            string password = TestUtilities.GenerateRandomPassword();
            string wrongPassword = "passworD";
            var files = TestUtilities.GenerateFilesFlat(dirToZip);

            _output.WriteLine("Creating the zip.");
            using (var zip = new ZipFile())
            {
                zip.Password = password;
                zip.AddFiles(files, Path.GetFileName(dirToZip));
                zip.Save(zipFileToCreate);
            }

            _output.WriteLine("Extract one file with wrong password.");

             // pick a random entry to extract
            int ix = -1;
            string extractedFile = null;

            // Perform two passes: first with correct password to extract the file.
            // 2nd with incorrect password to see if the file is deleted.

            Directory.CreateDirectory(extractDir);
            for (int i=0; i < 2; i++)
            {
                try
                {
                    using (var zip = ZipFile.Read(zipFileToCreate))
                    {
                        if (i==0)
                        {
                            do
                            {
                                ix = this._rnd.Next(zip.Entries.Count);
                            }
                            while (zip[ix].IsDirectory);
                            _output.WriteLine("Selected entry: {0}", zip[ix].FileName);
                            extractedFile = Path.Combine(extractDir, zip[ix].FileName.Replace("/","\\"));
                            _output.WriteLine("name for extracted file: {0}", extractedFile);
                            Assert.False(File.Exists(extractedFile), "The file exists.");
                        }
                        _output.WriteLine("Cycle {0}: ExtractWithPassword()", i);
                        zip[ix].ExtractWithPassword(extractDir,
                                                    ExtractExistingFileAction.OverwriteSilently,
                                                    (i==0)? password : wrongPassword);
                    }
                }
                catch (Ionic.Zip.BadPasswordException)
                {
                    // do not swallow exceptions on the first go-round
                    if (i==0) throw;
                }
                Assert.True(File.Exists(extractedFile), $"Cycle {i}: The extracted file does not exist.");
            }
        }

    }
}
