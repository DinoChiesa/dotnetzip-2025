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
    public class TestRace : IonicTestClass
    {
        public TestRace(ITestOutputHelper output) 
        {
            _output = output;
        }

        [Fact]
        public void Test1()
        {
            string tld = new String(TopLevelDir); // copy to avoid changes
            string zipFileToCreate = Path.Combine(tld, "Test1.zip");
            string marker = TestUtilities.GetMarker();
            string subdir = Path.Combine(tld, String.Format("files-{0}", marker));
            string[] filesToZip = TestUtilities.GenerateFilesFlat(subdir);

            for (int i = 0; i < filesToZip.Length; i++) {
                _output.WriteLine("File[{0}]: {1}", i, filesToZip[i]);
            }

            Array.ForEach(filesToZip, x => {
                File.Move(x, Path.Combine(subdir, Path.GetFileName(x).ToUpper()));
            });
            
            filesToZip = Directory.GetFiles(subdir);

            for (int i = 0; i < filesToZip.Length; i++) {
                _output.WriteLine("File[{0}]: {1}", i, filesToZip[i]);
            }
            
        }
    }

}
