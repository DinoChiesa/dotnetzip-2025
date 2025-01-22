// RandomTextGenTests.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2011, 2025 Dino Chiesa
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// This module defines tests the RandomTextGenerator,
// which is used only by DotNetZip test code. It's like a meta-test.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Apache 2.0 License.
// See the file LICENSE.txt that accompanies the source code, for the license details.
//
// ------------------------------------------------------------------
//

using Ionic.Zip.Tests.Utilities;
using Xunit.Abstractions;
using Assert = XunitAssertMessages.AssertM;

namespace Ionic.Zip.Tests.Streams
{
    public class RandomTextGenTests : IonicTestClass
    {
        public RandomTextGenTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Retrieve_Uris()
        {
            var unretrievable = new List<String[]>();
            _output.WriteLine("Retrieve_Uris()");
            foreach (string uri in RandomTextGenerator.URIS)
            {
                _output.WriteLine("  {0}", uri);
                try
                {
                    RandomTextGenerator.GetPageMarkup(uri);
                }
                catch (Exception exc1)
                {
                    _output.WriteLine("error {0}", exc1);
                    unretrievable.Add(new String[]{ uri, exc1.Message});
                }
            }

            if (unretrievable.Count != 0) {
                _output.WriteLine("\nunretrievable:\n  {0}",
                    string.Join("\n  ",
                        unretrievable.Select(item => string.Join(", REASON: ", item))));

            }

            Assert.True(unretrievable.Count == 0);
        }
    }
}
