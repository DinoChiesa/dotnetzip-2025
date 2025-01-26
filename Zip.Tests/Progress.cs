// Progress.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2010, 2025 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
// It defines the tests for progress events in DotNetZip.
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
    public class Progress : IonicTestClass
    {
        public Progress(ITestOutputHelper output)
        {
            _output = output;
        }

        private System.Reflection.Assembly _myself;
        private System.Reflection.Assembly myself
        {
            get
            {
                if (_myself == null)
                {
                    _myself = System.Reflection.Assembly.GetExecutingAssembly();
                }
                return _myself;
            }
        }


        void ReadProgress1(object sender, ReadProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Reading_Started:
                    _output.WriteLine("Reading_Started");
                    break;
                case ZipProgressEventType.Reading_Completed:
                    _output.WriteLine("Reading_Completed");
                    break;
                case ZipProgressEventType.Reading_BeforeReadEntry:
                    _output.WriteLine("Reading_BeforeReadEntry");
                    break;
                case ZipProgressEventType.Reading_AfterReadEntry:
                    _output.WriteLine("Reading_AfterReadEntry: {0}", e.CurrentEntry.FileName);
                    break;
                case ZipProgressEventType.Reading_ArchiveBytesRead:
                    break;
            }
        }


        [Fact]
        public void Progress_ReadFile()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string  zipFileToCreate = Path.Combine(tld, "Progress_ReadFile.zip");
            string dirToZip = Path.Combine(tld, TestUtilities.GetMarker());
            var files = TestUtilities.GenerateFilesFlat(dirToZip);

            using (ZipFile zip = new ZipFile())
            {
                zip.AddFiles(files);
                zip.Save(zipFileToCreate);
            }

            int count = CountEntries(zipFileToCreate);
            Assert.True(count>0);

            var options = new ReadOptions {
                    StatusMessageWriter = new StringWriter(),
                    ReadProgress = ReadProgress1
            };
            using (ZipFile zip = ZipFile.Read(zipFileToCreate, options))
            {
                // this should be fine
                zip.RemoveEntry(zip[1]);
                zip.Save();
            }
            _output.WriteLine(options.StatusMessageWriter.ToString());
            Assert.Equal<Int32>(count, CountEntries(zipFileToCreate)+1);
        }


        void AddProgress1(object sender, AddProgressEventArgs e)
        {
            switch (e.EventType)
            {
                case ZipProgressEventType.Adding_Started:
                    _output.WriteLine("Adding_Started");
                    break;
                case ZipProgressEventType.Adding_Completed:
                    _output.WriteLine("Adding_Completed");
                    break;
                case ZipProgressEventType.Adding_AfterAddEntry:
                    _output.WriteLine("Adding_AfterAddEntry: {0}", e.CurrentEntry.FileName);
                    break;
            }
        }


        [Fact]
        public void Progress_AddFiles()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string zipFileToCreate = Path.Combine(tld, "Progress_AddFiles.zip");
            string dirToZip = Path.Combine(tld, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
            var files = TestUtilities.GenerateFilesFlat(dirToZip);

            var sw = new StringWriter();
            using (ZipFile zip = new ZipFile(zipFileToCreate, sw))
            {
                zip.AddProgress += AddProgress1;
                zip.AddFiles(files);
                zip.Save();
            }
            _output.WriteLine(sw.ToString());

            Assert.Equal<Int32>(files.Length, CountEntries(zipFileToCreate));
        }

    }


}
