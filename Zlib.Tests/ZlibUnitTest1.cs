﻿// Copyright (c) 2009, 2025 Dino Chiesa.
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
// This module defines the base class for DotNetZip test classes.
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

namespace Ionic.Zlib.Tests
{
    public class UnitTest1 : IDisposable
    {
        private System.Random rnd;
        protected System.Collections.Generic.List<string> _DirsToRemove;
        private string CurrentDir = null;
        private string TopLevelDir = null;
        Dictionary<String, String> TestStrings;

        private ITestOutputHelper _output;

        public UnitTest1(ITestOutputHelper output)
        {
            // I could not get this to work with a BaseClass ctor
            // see https://stackoverflow.com/questions/73685218/get-at-itestoutputhelper-in-xunit-without-injecting-it
            _output = output;

            this.rnd = new System.Random();
            TestStrings = new Dictionary<String, String>()
            {
                { "LetMeDoItNow", LetMeDoItNow },
                { "GoPlacidly", GoPlacidly },
                { "IhaveaDream", IhaveaDream },
                { "LoremIpsum", LoremIpsum },
            };

            _DirsToRemove = new System.Collections.Generic.List<string>();
            CurrentDir = System.IO.Directory.GetCurrentDirectory();
            TestUtilities.Initialize(out TopLevelDir);
            _DirsToRemove.Add(TopLevelDir);
        }

        static UnitTest1()
        {
            LoremIpsumWords = LoremIpsum.Split(
                " ".ToCharArray(),
                StringSplitOptions.RemoveEmptyEntries
            );
        }

        public void Dispose()
        {
            TestUtilities.CleanUp(CurrentDir, _DirsToRemove, _output);
        }

        #region Helpers

        static MemoryStream StringToMemoryStream(string s)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            int byteCount = enc.GetByteCount(s.ToCharArray(), 0, s.Length);
            byte[] ByteArray = new byte[byteCount];
            int bytesEncodedCount = enc.GetBytes(s, 0, s.Length, ByteArray, 0);
            var ms = new System.IO.MemoryStream(ByteArray);
            return ms;
        }

        static String MemoryStreamToString(System.IO.MemoryStream ms)
        {
            byte[] ByteArray = ms.ToArray();
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            var s = enc.GetString(ByteArray);
            return s;
        }

        private static void CopyStream(System.IO.Stream src, System.IO.Stream dest)
        {
            byte[] buffer = new byte[1024];
            int len = 0;
            while ((len = src.Read(buffer, 0, buffer.Length)) > 0)
            {
                dest.Write(buffer, 0, len);
            }
            dest.Flush();
        }

        private static string GetTestDependentDir(string startingPoint, string subdir)
        {
            var location = startingPoint;
            for (int i = 0; i < 3; i++)
                location = Path.GetDirectoryName(location);

            location = Path.Combine(location, subdir);
            return location;
        }

        private string GetContentFile(string fileName)
        {
            string srcDir = TestUtilities.GetTestSrcDir();
            string path = Path.Combine(srcDir, "Resources", fileName);
            Assert.True(File.Exists(path), String.Format("file ({0}) does not exist", path));
            return path;
        }

        internal string Exec(string program, string args)
        {
            return Exec(program, args, true);
        }

        internal string Exec(string program, string args, bool waitForExit)
        {
            return Exec(program, args, waitForExit, true);
        }

        internal string Exec(string program, string args, bool waitForExit, bool emitOutput)
        {
            if (program == null)
                throw new ArgumentException("program");

            if (args == null)
                throw new ArgumentException("args");

            _output.WriteLine("running command: {0} {1}", program, args);

            string output;
            int rc = Exec_NoContext(program, args, waitForExit, out output);

            if (rc != 0)
                throw new Exception(String.Format("Non-zero RC {0}: {1}", program, output));

            if (emitOutput)
                _output.WriteLine("output: {0}", output);
            else
                _output.WriteLine("A-OK. (output suppressed)");

            return output;
        }

        internal static int Exec_NoContext(
            string program,
            string args,
            bool waitForExit,
            out string output
        )
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = program,
                    CreateNoWindow = true,
                    Arguments = args,
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    UseShellExecute = false,
                },
            };

            if (waitForExit)
            {
                StringBuilder sb = new StringBuilder();
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                // must read at least one of the stderr or stdout asynchronously,
                // to avoid deadlock
                Action<Object, System.Diagnostics.DataReceivedEventArgs> stdErrorRead = (o, e) =>
                {
                    if (!String.IsNullOrEmpty(e.Data))
                        sb.Append(e.Data);
                };

                p.ErrorDataReceived += new System.Diagnostics.DataReceivedEventHandler(
                    stdErrorRead
                );
                p.Start();
                p.BeginErrorReadLine();
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                if (sb.Length > 0)
                    output += sb.ToString();
                //output = CleanWzzipOut(output); // just in case
                return p.ExitCode;
            }
            else
            {
                p.Start();
            }
            output = "";
            return 0;
        }

        #endregion


        [Fact]
        public void zlib_Compat_decompress_wi13446()
        {
            var zlibbedFile = GetContentFile("zlibbed.file");
            var streamCopy = new Action<Stream, Stream, int>(
                (source, dest, bufferSize) =>
                {
                    var temp = new byte[bufferSize];
                    while (true)
                    {
                        var read = source.Read(temp, 0, temp.Length);
                        if (read <= 0)
                            break;
                        dest.Write(temp, 0, read);
                    }
                }
            );

            var unpack = new Action<int>(
                (bufferSize) =>
                {
                    using (var output = new MemoryStream())
                    {
                        using (var input = File.OpenRead(zlibbedFile))
                        {
                            using (var zinput = new ZlibStream(input, CompressionMode.Decompress))
                            {
                                streamCopy(zinput, output, bufferSize);
                            }
                        }
                    }
                }
            );

            unpack(1024);
            unpack(16384);
        }

        [Fact]
        public void Zlib_BasicDeflateAndInflate()
        {
            string TextToCompress = LoremIpsum;

            int rc;
            int bufferSize = 40000;
            byte[] compressedBytes = new byte[bufferSize];
            byte[] decompressedBytes = new byte[bufferSize];

            ZlibCodec compressingStream = new ZlibCodec();

            rc = compressingStream.InitializeDeflate(CompressionLevel.Default);
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at InitializeDeflate() [{0}]", compressingStream.Message)
            );

            compressingStream.InputBuffer = System.Text.ASCIIEncoding.ASCII.GetBytes(
                TextToCompress
            );
            compressingStream.NextIn = 0;

            compressingStream.OutputBuffer = compressedBytes;
            compressingStream.NextOut = 0;

            while (
                compressingStream.TotalBytesIn != TextToCompress.Length
                && compressingStream.TotalBytesOut < bufferSize
            )
            {
                compressingStream.AvailableBytesIn = compressingStream.AvailableBytesOut = 1; // force small buffers
                rc = compressingStream.Deflate(FlushType.None);
                Assert.Equal<int>(
                    ZlibConstants.Z_OK,
                    rc,
                    String.Format("at Deflate(1) [{0}]", compressingStream.Message)
                );
            }

            while (true)
            {
                compressingStream.AvailableBytesOut = 1;
                rc = compressingStream.Deflate(FlushType.Finish);
                if (rc == ZlibConstants.Z_STREAM_END)
                    break;
                Assert.Equal<int>(
                    ZlibConstants.Z_OK,
                    rc,
                    String.Format("at Deflate(2) [{0}]", compressingStream.Message)
                );
            }

            rc = compressingStream.EndDeflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at EndDeflate() [{0}]", compressingStream.Message)
            );

            ZlibCodec decompressingStream = new ZlibCodec();

            decompressingStream.InputBuffer = compressedBytes;
            decompressingStream.NextIn = 0;
            decompressingStream.OutputBuffer = decompressedBytes;
            decompressingStream.NextOut = 0;

            rc = decompressingStream.InitializeInflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at InitializeInflate() [{0}]", decompressingStream.Message)
            );
            //CheckForError(decompressingStream, rc, "inflateInit");

            while (
                decompressingStream.TotalBytesOut < decompressedBytes.Length
                && decompressingStream.TotalBytesIn < bufferSize
            )
            {
                decompressingStream.AvailableBytesIn = decompressingStream.AvailableBytesOut = 1; /* force small buffers */
                rc = decompressingStream.Inflate(FlushType.None);
                if (rc == ZlibConstants.Z_STREAM_END)
                    break;
                Assert.Equal<int>(
                    ZlibConstants.Z_OK,
                    rc,
                    String.Format("at Inflate() [{0}]", decompressingStream.Message)
                );
                //CheckForError(decompressingStream, rc, "inflate");
            }

            rc = decompressingStream.EndInflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at EndInflate() [{0}]", decompressingStream.Message)
            );
            //CheckForError(decompressingStream, rc, "inflateEnd");

            int j = 0;
            for (; j < decompressedBytes.Length; j++)
                if (decompressedBytes[j] == 0)
                    break;

            Assert.Equal<int>(TextToCompress.Length, j, String.Format("Unequal lengths"));

            int i = 0;
            for (i = 0; i < j; i++)
                if (TextToCompress[i] != decompressedBytes[i])
                    break;

            Assert.Equal<int>(j, i, String.Format("Non-identical content"));

            var result = System.Text.ASCIIEncoding.ASCII.GetString(decompressedBytes, 0, j);

            _output.WriteLine("orig length: {0}", TextToCompress.Length);
            _output.WriteLine("compressed length: {0}", compressingStream.TotalBytesOut);
            _output.WriteLine("decompressed length: {0}", decompressingStream.TotalBytesOut);
            _output.WriteLine("result length: {0}", result.Length);
            _output.WriteLine("result of inflate:\n{0}", result);
            return;
        }

        [Fact]
        public void GZ_Utility()
        {
            var srcDir = TestUtilities.GetTestSrcDir();
            var dnzGzipexe = Path.Combine(
                srcDir,
                "..",
                "Tools\\GZip\\bin\\Debug",
                "net9.0",
                "GZip.exe"
            );
            Assert.True(
                File.Exists(dnzGzipexe),
                String.Format("GZip.exe is missing {0}", dnzGzipexe)
            );
            var unxGzipexe = Path.Combine(srcDir, "Resources\\gzip.exe");
            Assert.True(
                File.Exists(unxGzipexe),
                String.Format("gzip.exe is missing {0}", unxGzipexe)
            );

            foreach (var key in TestStrings.Keys)
            {
                int count = this.rnd.Next(81) + 40;
                _output.WriteLine("Doing string {0}", key);
                var s = TestStrings[key];
                var fname = String.Format("Pippo-{0}.txt", key);
                using (var sw = new StreamWriter(File.Create(fname)))
                {
                    for (int k = 0; k < count; k++)
                    {
                        sw.WriteLine(s);
                    }
                }

                int crcOriginal = DoCrc(fname);

                string args = fname + " -keep -v";
                _output.WriteLine("Exec: gzip {0}", args);
                string gzout = this.Exec(dnzGzipexe, args);

                var gzfile = fname + ".gz";
                Assert.True(File.Exists(gzfile), String.Format("File is missing. {0}", gzfile));

                File.Delete(fname);
                Assert.True(!File.Exists(fname), String.Format("The delete failed. {0}", fname));

                System.Threading.Thread.Sleep(1200);

                args = "-dfv " + gzfile;
                _output.WriteLine("Exec: gzip {0}", args);
                gzout = this.Exec(unxGzipexe, args);
                Assert.True(File.Exists(fname), String.Format("File is missing. {0}", fname));

                int crcDecompressed = DoCrc(fname);
                Assert.Equal<int>(
                    crcOriginal,
                    crcDecompressed,
                    String.Format("CRC mismatch {0:X8}!={1:X8}", crcOriginal, crcDecompressed)
                );
            }
        }

        [Fact]
        public void Zlib_BasicDictionaryDeflateInflate()
        {
            int rc;
            int comprLen = 40000;
            int uncomprLen = comprLen;
            byte[] uncompr = new byte[uncomprLen];
            byte[] compr = new byte[comprLen];
            //long dictId;

            ZlibCodec compressor = new ZlibCodec();
            rc = compressor.InitializeDeflate(CompressionLevel.BestCompression);
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at InitializeDeflate() [{0}]", compressor.Message)
            );

            string dictionaryWord = "hello ";
            byte[] dictionary = System.Text.ASCIIEncoding.ASCII.GetBytes(dictionaryWord);
            string TextToCompress = "hello, hello!  How are you, Joe? I said hello. ";
            byte[] BytesToCompress = System.Text.ASCIIEncoding.ASCII.GetBytes(TextToCompress);

            rc = compressor.SetDictionary(dictionary);
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at SetDeflateDictionary() [{0}]", compressor.Message)
            );

            int dictId = compressor.Adler32;

            compressor.OutputBuffer = compr;
            compressor.NextOut = 0;
            compressor.AvailableBytesOut = comprLen;

            compressor.InputBuffer = BytesToCompress;
            compressor.NextIn = 0;
            compressor.AvailableBytesIn = BytesToCompress.Length;

            rc = compressor.Deflate(FlushType.Finish);
            Assert.Equal<int>(
                ZlibConstants.Z_STREAM_END,
                rc,
                String.Format("at Deflate() [{0}]", compressor.Message)
            );

            rc = compressor.EndDeflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at EndDeflate() [{0}]", compressor.Message)
            );

            ZlibCodec decompressor = new ZlibCodec();

            decompressor.InputBuffer = compr;
            decompressor.NextIn = 0;
            decompressor.AvailableBytesIn = comprLen;

            rc = decompressor.InitializeInflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at InitializeInflate() [{0}]", decompressor.Message)
            );

            decompressor.OutputBuffer = uncompr;
            decompressor.NextOut = 0;
            decompressor.AvailableBytesOut = uncomprLen;

            while (true)
            {
                rc = decompressor.Inflate(FlushType.None);
                if (rc == ZlibConstants.Z_STREAM_END)
                {
                    break;
                }
                if (rc == ZlibConstants.Z_NEED_DICT)
                {
                    Assert.Equal<long>(dictId, decompressor.Adler32, "Unexpected Dictionary");
                    rc = decompressor.SetDictionary(dictionary);
                }
                Assert.Equal<int>(
                    ZlibConstants.Z_OK,
                    rc,
                    String.Format("at Inflate/SetInflateDictionary() [{0}]", decompressor.Message)
                );
            }

            rc = decompressor.EndInflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at EndInflate() [{0}]", decompressor.Message)
            );

            int j = 0;
            for (; j < uncompr.Length; j++)
                if (uncompr[j] == 0)
                    break;

            Assert.Equal<int>(TextToCompress.Length, j, String.Format("Unequal lengths"));

            int i = 0;
            for (i = 0; i < j; i++)
                if (TextToCompress[i] != uncompr[i])
                    break;

            Assert.Equal<int>(j, i, String.Format("Non-identical content"));

            var result = System.Text.ASCIIEncoding.ASCII.GetString(uncompr, 0, j);

            _output.WriteLine("orig length: {0}", TextToCompress.Length);
            _output.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
            _output.WriteLine("uncompressed length: {0}", decompressor.TotalBytesOut);
            _output.WriteLine("result length: {0}", result.Length);
            _output.WriteLine("result of inflate:\n{0}", result);
        }

        [Fact]
        public void Zlib_TestFlushSync()
        {
            int rc;
            int bufferSize = 40000;
            byte[] CompressedBytes = new byte[bufferSize];
            byte[] DecompressedBytes = new byte[bufferSize];
            string TextToCompress = "This is the text that will be compressed.";
            byte[] BytesToCompress = System.Text.ASCIIEncoding.ASCII.GetBytes(TextToCompress);

            ZlibCodec compressor = new ZlibCodec(CompressionMode.Compress);

            compressor.InputBuffer = BytesToCompress;
            compressor.NextIn = 0;
            compressor.AvailableBytesIn = 3;

            compressor.OutputBuffer = CompressedBytes;
            compressor.NextOut = 0;
            compressor.AvailableBytesOut = CompressedBytes.Length;

            rc = compressor.Deflate(FlushType.Full);

            CompressedBytes[3]++; // force an error in first compressed block // dinoch - ??
            compressor.AvailableBytesIn = TextToCompress.Length - 3;

            rc = compressor.Deflate(FlushType.Finish);
            Assert.Equal<int>(
                ZlibConstants.Z_STREAM_END,
                rc,
                String.Format("at Deflate() [{0}]", compressor.Message)
            );

            rc = compressor.EndDeflate();
            bufferSize = (int)(compressor.TotalBytesOut);

            ZlibCodec decompressor = new ZlibCodec(CompressionMode.Decompress);

            decompressor.InputBuffer = CompressedBytes;
            decompressor.NextIn = 0;
            decompressor.AvailableBytesIn = 2;

            decompressor.OutputBuffer = DecompressedBytes;
            decompressor.NextOut = 0;
            decompressor.AvailableBytesOut = DecompressedBytes.Length;

            rc = decompressor.Inflate(FlushType.None);
            decompressor.AvailableBytesIn = bufferSize - 2;

            rc = decompressor.SyncInflate();

            bool gotException = false;
            try
            {
                rc = decompressor.Inflate(FlushType.Finish);
            }
            catch (ZlibException ex1)
            {
                _output.WriteLine("Got Expected Exception: " + ex1);
                gotException = true;
            }

            Assert.True(gotException, "inflate should report DATA_ERROR");

            rc = decompressor.EndInflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at EndInflate() [{0}]", decompressor.Message)
            );

            int j = 0;
            for (; j < DecompressedBytes.Length; j++)
                if (DecompressedBytes[j] == 0)
                    break;

            var result = System.Text.ASCIIEncoding.ASCII.GetString(DecompressedBytes, 0, j);

            Assert.Equal<int>(
                TextToCompress.Length,
                result.Length + 3,
                "Strings are unequal lengths"
            );

            Console.WriteLine("orig length: {0}", TextToCompress.Length);
            Console.WriteLine("compressed length: {0}", compressor.TotalBytesOut);
            Console.WriteLine("uncompressed length: {0}", decompressor.TotalBytesOut);
            Console.WriteLine("result length: {0}", result.Length);
            Console.WriteLine("result of inflate:\n(Thi){0}", result);
        }

        [Fact]
        public void Zlib_Codec_TestLargeDeflateInflate()
        {
            int rc;
            int j;
            int bufferSize = 80000;
            byte[] compressedBytes = new byte[bufferSize];
            byte[] workBuffer = new byte[bufferSize / 4];

            ZlibCodec compressingStream = new ZlibCodec();

            rc = compressingStream.InitializeDeflate(CompressionLevel.Level1);
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at InitializeDeflate() [{0}]", compressingStream.Message)
            );

            compressingStream.OutputBuffer = compressedBytes;
            compressingStream.AvailableBytesOut = compressedBytes.Length;
            compressingStream.NextOut = 0;
            System.Random rnd = new Random();

            for (int k = 0; k < 4; k++)
            {
                switch (k)
                {
                    case 0:
                        // At this point, workBuffer is all zeroes, so it should compress very well.
                        break;

                    case 1:
                        // switch to no compression, keep same workBuffer (all zeroes):
                        compressingStream.SetDeflateParams(
                            CompressionLevel.None,
                            CompressionStrategy.Default
                        );
                        break;

                    case 2:
                        // Insert data into workBuffer, and switch back to compressing mode.
                        // we'll use lengths of the same random byte:
                        for (int i = 0; i < workBuffer.Length / 1000; i++)
                        {
                            byte b = (byte)rnd.Next();
                            int n = 500 + rnd.Next(500);
                            for (j = 0; j < n; j++)
                                workBuffer[j + i] = b;
                            i += j - 1;
                        }
                        compressingStream.SetDeflateParams(
                            CompressionLevel.BestCompression,
                            CompressionStrategy.Filtered
                        );
                        break;

                    case 3:
                        // insert totally random data into the workBuffer
                        rnd.NextBytes(workBuffer);
                        break;
                }

                compressingStream.InputBuffer = workBuffer;
                compressingStream.NextIn = 0;
                compressingStream.AvailableBytesIn = workBuffer.Length;
                rc = compressingStream.Deflate(FlushType.None);
                Assert.Equal<int>(
                    ZlibConstants.Z_OK,
                    rc,
                    String.Format("at Deflate({0}) [{1}]", k, compressingStream.Message)
                );

                if (k == 0)
                    Assert.Equal<int>(
                        0,
                        compressingStream.AvailableBytesIn,
                        "Deflate should be greedy."
                    );

                _output.WriteLine(
                    "Stage {0}: uncompressed/compresssed bytes so far:  ({1,6}/{2,6})",
                    k,
                    compressingStream.TotalBytesIn,
                    compressingStream.TotalBytesOut
                );
            }

            rc = compressingStream.Deflate(FlushType.Finish);
            Assert.Equal<int>(
                ZlibConstants.Z_STREAM_END,
                rc,
                String.Format("at Deflate() [{0}]", compressingStream.Message)
            );

            rc = compressingStream.EndDeflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at EndDeflate() [{0}]", compressingStream.Message)
            );

            _output.WriteLine(
                "Final: uncompressed/compressed bytes: ({0,6},{1,6})",
                compressingStream.TotalBytesIn,
                compressingStream.TotalBytesOut
            );

            ZlibCodec decompressingStream = new ZlibCodec(CompressionMode.Decompress);

            decompressingStream.InputBuffer = compressedBytes;
            decompressingStream.NextIn = 0;
            decompressingStream.AvailableBytesIn = bufferSize;

            // upon inflating, we overwrite the decompressedBytes buffer repeatedly
            int nCycles = 0;
            while (true)
            {
                decompressingStream.OutputBuffer = workBuffer;
                decompressingStream.NextOut = 0;
                decompressingStream.AvailableBytesOut = workBuffer.Length;
                rc = decompressingStream.Inflate(FlushType.None);

                nCycles++;

                if (rc == ZlibConstants.Z_STREAM_END)
                    break;

                Assert.Equal<int>(
                    ZlibConstants.Z_OK,
                    rc,
                    String.Format(
                        "at Inflate() [{0}] TotalBytesOut={1}",
                        decompressingStream.Message,
                        decompressingStream.TotalBytesOut
                    )
                );
            }

            rc = decompressingStream.EndInflate();
            Assert.Equal<int>(
                ZlibConstants.Z_OK,
                rc,
                String.Format("at EndInflate() [{0}]", decompressingStream.Message)
            );

            Assert.Equal<int>(4 * workBuffer.Length, (int)decompressingStream.TotalBytesOut);

            _output.WriteLine("compressed length: {0}", compressingStream.TotalBytesOut);
            _output.WriteLine("decompressed length (expected): {0}", 4 * workBuffer.Length);
            _output.WriteLine(
                "decompressed length (actual)  : {0}",
                decompressingStream.TotalBytesOut
            );
            _output.WriteLine("decompression cycles: {0}", nCycles);
        }

        [Fact]
        public void Zlib_CompressString()
        {
            _output.WriteLine("Original.Length: {0}", GoPlacidly.Length);
            byte[] compressed = ZlibStream.CompressString(GoPlacidly);
            _output.WriteLine("compressed.Length: {0}", compressed.Length);
            Assert.True(compressed.Length < GoPlacidly.Length);

            string uncompressed = ZlibStream.UncompressString(compressed);
            Assert.Equal<Int32>(GoPlacidly.Length, uncompressed.Length);
        }

        [Fact]
        public void GZip_CompressString()
        {
            _output.WriteLine("Original.Length: {0}", GoPlacidly.Length);
            byte[] compressed = GZipStream.CompressString(GoPlacidly);
            _output.WriteLine("compressed.Length: {0}", compressed.Length);
            Assert.True(compressed.Length < GoPlacidly.Length);

            string uncompressed = GZipStream.UncompressString(compressed);
            Assert.Equal<Int32>(GoPlacidly.Length, uncompressed.Length);
        }

        [Fact]
        public void Deflate_CompressString()
        {
            _output.WriteLine("Original.Length: {0}", GoPlacidly.Length);
            byte[] compressed = DeflateStream.CompressString(GoPlacidly);
            _output.WriteLine("compressed.Length: {0}", compressed.Length);
            Assert.True(compressed.Length < GoPlacidly.Length);

            string uncompressed = DeflateStream.UncompressString(compressed);
            Assert.Equal<Int32>(GoPlacidly.Length, uncompressed.Length);
        }

        [Fact]
        public void Zlib_ZlibStream_CompressWhileWriting()
        {
            System.IO.MemoryStream msSinkCompressed;
            System.IO.MemoryStream msSinkDecompressed;
            ZlibStream zOut;

            // first, compress:
            msSinkCompressed = new System.IO.MemoryStream();
            zOut = new ZlibStream(
                msSinkCompressed,
                CompressionMode.Compress,
                CompressionLevel.BestCompression,
                true
            );
            CopyStream(StringToMemoryStream(IhaveaDream), zOut);
            zOut.Close();

            // at this point, msSinkCompressed contains the compressed bytes

            // now, decompress:
            msSinkDecompressed = new System.IO.MemoryStream();
            zOut = new ZlibStream(msSinkDecompressed, CompressionMode.Decompress);
            msSinkCompressed.Position = 0;
            CopyStream(msSinkCompressed, zOut);

            string result = MemoryStreamToString(msSinkDecompressed);
            _output.WriteLine("decompressed: {0}", result);
            Assert.Equal<String>(IhaveaDream, result);
        }

        [Fact]
        public void Zlib_ZlibStream_CompressWhileReading_wi8557()
        {
            // workitem 8557
            System.IO.MemoryStream msSinkCompressed;
            System.IO.MemoryStream msSinkDecompressed;

            // first, compress:
            msSinkCompressed = new System.IO.MemoryStream();
            ZlibStream zIn = new ZlibStream(
                StringToMemoryStream(WhatWouldThingsHaveBeenLike),
                CompressionMode.Compress,
                CompressionLevel.BestCompression,
                true
            );
            CopyStream(zIn, msSinkCompressed);

            // At this point, msSinkCompressed contains the compressed bytes.
            // Now, decompress:
            msSinkDecompressed = new System.IO.MemoryStream();
            ZlibStream zOut = new ZlibStream(msSinkDecompressed, CompressionMode.Decompress);
            msSinkCompressed.Position = 0;
            CopyStream(msSinkCompressed, zOut);

            string result = MemoryStreamToString(msSinkDecompressed);
            _output.WriteLine("decompressed: {0}", result);
            Assert.Equal<String>(WhatWouldThingsHaveBeenLike, result);
        }

        [Fact]
        public void Zlib_CodecTest()
        {
            int sz = this.rnd.Next(50000) + 50000;
            string fileName = System.IO.Path.Combine(TopLevelDir, "Zlib_CodecTest.txt");
            CreateAndFillFileText(fileName, sz);

            byte[] UncompressedBytes = System.IO.File.ReadAllBytes(fileName);

            foreach (
                Ionic.Zlib.CompressionLevel level in Enum.GetValues(
                    typeof(Ionic.Zlib.CompressionLevel)
                )
            )
            {
                _output.WriteLine("\n\n+++++++++++++++++++++++++++++++++++++++++++++++++++++++");
                _output.WriteLine("trying compression level '{0}'", level.ToString());
                byte[] CompressedBytes = DeflateBuffer(UncompressedBytes, level);
                byte[] DecompressedBytes = InflateBuffer(CompressedBytes, UncompressedBytes.Length);
                CompareBuffers(UncompressedBytes, DecompressedBytes);
            }
            System.Threading.Thread.Sleep(2000);
        }

#if UNNECESSARY
        private byte[] ReadFile(string f)
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(f);
            byte[] buffer = new byte[fi.Length];

            using (var readStream = System.IO.File.OpenRead(f))
            {
                readStream.Read(buffer, 0, buffer.Length);
            }
            return buffer;
        }
#endif

        private byte[] InflateBuffer(byte[] b, int length)
        {
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            ZlibCodec decompressor = new ZlibCodec();
            byte[] DecompressedBytes = new byte[length];
            _output.WriteLine("\n============================================");
            _output.WriteLine("Size of Buffer to Inflate: {0} bytes.", b.Length);
            MemoryStream ms = new MemoryStream(DecompressedBytes);

            int rc = decompressor.InitializeInflate();

            decompressor.InputBuffer = b;
            decompressor.NextIn = 0;
            decompressor.AvailableBytesIn = b.Length;

            decompressor.OutputBuffer = buffer;

            for (int pass = 0; pass < 2; pass++)
            {
                FlushType flush = (pass == 0) ? FlushType.None : FlushType.Finish;
                do
                {
                    decompressor.NextOut = 0;
                    decompressor.AvailableBytesOut = buffer.Length;
                    rc = decompressor.Inflate(flush);

                    if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
                        throw new Exception("inflating: " + decompressor.Message);

                    if (buffer.Length - decompressor.AvailableBytesOut > 0)
                        ms.Write(
                            decompressor.OutputBuffer,
                            0,
                            buffer.Length - decompressor.AvailableBytesOut
                        );
                } while (decompressor.AvailableBytesIn > 0 || decompressor.AvailableBytesOut == 0);
            }

            decompressor.EndInflate();
            _output.WriteLine("TBO({0}).", decompressor.TotalBytesOut);
            return DecompressedBytes;
        }

        private void CompareBuffers(byte[] a, byte[] b)
        {
            _output.WriteLine("\n============================================");
            _output.WriteLine("Comparing...");

            if (a.Length != b.Length)
                throw new Exception(String.Format("not equal size ({0}!={1})", a.Length, b.Length));

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    throw new Exception("not equal");
            }
        }

        private byte[] DeflateBuffer(byte[] b, CompressionLevel level)
        {
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            ZlibCodec compressor = new ZlibCodec();

            _output.WriteLine("\n============================================");
            _output.WriteLine("Size of Buffer to Deflate: {0} bytes.", b.Length);
            MemoryStream ms = new MemoryStream();

            int rc = compressor.InitializeDeflate(level);

            compressor.InputBuffer = b;
            compressor.NextIn = 0;
            compressor.AvailableBytesIn = b.Length;

            compressor.OutputBuffer = buffer;

            for (int pass = 0; pass < 2; pass++)
            {
                FlushType flush = (pass == 0) ? FlushType.None : FlushType.Finish;
                do
                {
                    compressor.NextOut = 0;
                    compressor.AvailableBytesOut = buffer.Length;
                    rc = compressor.Deflate(flush);

                    if (rc != ZlibConstants.Z_OK && rc != ZlibConstants.Z_STREAM_END)
                        throw new Exception("deflating: " + compressor.Message);

                    if (buffer.Length - compressor.AvailableBytesOut > 0)
                        ms.Write(
                            compressor.OutputBuffer,
                            0,
                            buffer.Length - compressor.AvailableBytesOut
                        );
                } while (compressor.AvailableBytesIn > 0 || compressor.AvailableBytesOut == 0);
            }

            compressor.EndDeflate();
            Console.WriteLine("TBO({0}).", compressor.TotalBytesOut);

            ms.Seek(0, SeekOrigin.Begin);
            byte[] c = new byte[compressor.TotalBytesOut];
            ms.Read(c, 0, c.Length);
            return c;
        }

        [Fact]
        public void Zlib_GZipStream_FileName_And_Comments()
        {
            // select the name of the zip file
            string FileToCompress = System.IO.Path.Combine(TopLevelDir, "Zlib_GZipStream.dat");
            Assert.False(
                System.IO.File.Exists(FileToCompress),
                String.Format("The temporary zip file '{0}' already exists.", FileToCompress)
            );
            byte[] working = new byte[WORKING_BUFFER_SIZE];
            int n = -1;

            int sz = this.rnd.Next(21000) + 15000;
            _output.WriteLine("  Creating file: {0} sz({1})", FileToCompress, sz);
            CreateAndFillFileText(FileToCompress, sz);

            System.IO.FileInfo fi1 = new System.IO.FileInfo(FileToCompress);
            int crc1 = DoCrc(FileToCompress);

            // four trials, all combos of FileName and Comment null or not null.
            for (int k = 0; k < 4; k++)
            {
                string CompressedFile = String.Format("{0}-{1}.compressed", FileToCompress, k);

                using (Stream input = File.OpenRead(FileToCompress))
                {
                    using (FileStream raw = new FileStream(CompressedFile, FileMode.Create))
                    {
                        using (
                            GZipStream compressor = new GZipStream(
                                raw,
                                CompressionMode.Compress,
                                CompressionLevel.BestCompression,
                                true
                            )
                        )
                        {
                            // FileName is optional metadata in the GZip bytestream
                            if (k % 2 == 1)
                                compressor.FileName = FileToCompress;

                            // Comment is optional metadata in the GZip bytestream
                            if (k > 2)
                                compressor.Comment = "Compressing: " + FileToCompress;

                            byte[] buffer = new byte[1024];
                            n = -1;
                            while (n != 0)
                            {
                                if (n > 0)
                                    compressor.Write(buffer, 0, n);

                                n = input.Read(buffer, 0, buffer.Length);
                            }
                        }
                    }
                }

                System.IO.FileInfo fi2 = new System.IO.FileInfo(CompressedFile);

                Assert.True(
                    fi1.Length > fi2.Length,
                    String.Format(
                        "Compressed File is not smaller, trial {0} ({1}!>{2})",
                        k,
                        fi1.Length,
                        fi2.Length
                    )
                );

                // decompress twice:
                // once with System.IO.Compression.GZipStream and once with Ionic.Zlib.GZipStream
                for (int j = 0; j < 2; j++)
                {
                    using (var input = System.IO.File.OpenRead(CompressedFile))
                    {
                        Stream decompressor = null;
                        try
                        {
                            switch (j)
                            {
                                case 0:
                                    decompressor = new Ionic.Zlib.GZipStream(
                                        input,
                                        CompressionMode.Decompress,
                                        true
                                    );
                                    break;
                                case 1:
                                    decompressor = new System.IO.Compression.GZipStream(
                                        input,
                                        System.IO.Compression.CompressionMode.Decompress,
                                        true
                                    );
                                    break;
                            }

                            string DecompressedFile = String.Format(
                                "{0}.{1}.decompressed",
                                CompressedFile,
                                (j == 0) ? "Ionic" : "BCL"
                            );

                            _output.WriteLine(
                                "........{0} ...",
                                System.IO.Path.GetFileName(DecompressedFile)
                            );

                            using (var s2 = System.IO.File.Create(DecompressedFile))
                            {
                                n = -1;
                                while (n != 0)
                                {
                                    n = decompressor.Read(working, 0, working.Length);
                                    if (n > 0)
                                        s2.Write(working, 0, n);
                                }
                            }

                            int crc2 = DoCrc(DecompressedFile);
                            Assert.Equal<Int32>(crc1, crc2);
                        }
                        finally
                        {
                            if (decompressor != null)
                                decompressor.Dispose();
                        }
                    }
                }
            }
        }

        [Fact]
        public void Zlib_GZipStream_ByteByByte_CheckCrc()
        {
            // select the name of the zip file
            string FileToCompress = System.IO.Path.Combine(
                TopLevelDir,
                "Zlib_GZipStream_ByteByByte.dat"
            );
            Assert.False(
                System.IO.File.Exists(FileToCompress),
                String.Format("The temporary zip file '{0}' already exists.", FileToCompress)
            );
            byte[] working = new byte[WORKING_BUFFER_SIZE];
            int n = -1;

            int sz = this.rnd.Next(21000) + 15000;
            _output.WriteLine("  Creating file: {0} sz({1})", FileToCompress, sz);
            CreateAndFillFileText(FileToCompress, sz);

            System.IO.FileInfo fi1 = new System.IO.FileInfo(FileToCompress);
            int crc1 = DoCrc(FileToCompress);

            // four trials, all combos of FileName and Comment null or not null.
            for (int k = 0; k < 4; k++)
            {
                string CompressedFile = String.Format("{0}-{1}.compressed", FileToCompress, k);

                using (Stream input = File.OpenRead(FileToCompress))
                {
                    using (FileStream raw = new FileStream(CompressedFile, FileMode.Create))
                    {
                        using (
                            GZipStream compressor = new GZipStream(
                                raw,
                                CompressionMode.Compress,
                                CompressionLevel.BestCompression,
                                true
                            )
                        )
                        {
                            // FileName is optional metadata in the GZip bytestream
                            if (k % 2 == 1)
                                compressor.FileName = FileToCompress;

                            // Comment is optional metadata in the GZip bytestream
                            if (k > 2)
                                compressor.Comment = "Compressing: " + FileToCompress;

                            byte[] buffer = new byte[1024];
                            n = -1;
                            while (n != 0)
                            {
                                if (n > 0)
                                {
                                    for (int i = 0; i < n; i++)
                                        compressor.WriteByte(buffer[i]);
                                }

                                n = input.Read(buffer, 0, buffer.Length);
                            }
                        }
                    }
                }

                System.IO.FileInfo fi2 = new System.IO.FileInfo(CompressedFile);

                Assert.True(
                    fi1.Length > fi2.Length,
                    String.Format(
                        "Compressed File is not smaller, trial {0} ({1}!>{2})",
                        k,
                        fi1.Length,
                        fi2.Length
                    )
                );

                // decompress twice:
                // once with System.IO.Compression.GZipStream and once with Ionic.Zlib.GZipStream
                for (int j = 0; j < 2; j++)
                {
                    using (var input = System.IO.File.OpenRead(CompressedFile))
                    {
                        Stream decompressor = null;
                        try
                        {
                            switch (j)
                            {
                                case 0:
                                    decompressor = new Ionic.Zlib.GZipStream(
                                        input,
                                        CompressionMode.Decompress,
                                        true
                                    );
                                    break;
                                case 1:
                                    decompressor = new System.IO.Compression.GZipStream(
                                        input,
                                        System.IO.Compression.CompressionMode.Decompress,
                                        true
                                    );
                                    break;
                            }

                            string DecompressedFile = String.Format(
                                "{0}.{1}.decompressed",
                                CompressedFile,
                                (j == 0) ? "Ionic" : "BCL"
                            );

                            _output.WriteLine(
                                "........{0} ...",
                                System.IO.Path.GetFileName(DecompressedFile)
                            );

                            using (var s2 = System.IO.File.Create(DecompressedFile))
                            {
                                n = -1;
                                while (n != 0)
                                {
                                    n = decompressor.Read(working, 0, working.Length);
                                    if (n > 0)
                                        s2.Write(working, 0, n);
                                }
                            }

                            int crc2 = DoCrc(DecompressedFile);
                            Assert.Equal<Int32>(crc1, crc2);
                        }
                        finally
                        {
                            if (decompressor as Ionic.Zlib.GZipStream != null)
                            {
                                var gz = (Ionic.Zlib.GZipStream)decompressor;
                                gz.Close(); // sets the final CRC
                                Assert.Equal<Int32>(gz.Crc32, crc1);
                            }

                            if (decompressor != null)
                                decompressor.Dispose();
                        }
                    }
                }
            }
        }

        [Fact]
        public void Zlib_GZipStream_DecompressEmptyStream()
        {
            _DecompressEmptyStream(typeof(GZipStream));
        }

        [Fact]
        public void Zlib_ZlibStream_DecompressEmptyStream()
        {
            _DecompressEmptyStream(typeof(ZlibStream));
        }

        private void _DecompressEmptyStream(Type t)
        {
            byte[] working = new byte[WORKING_BUFFER_SIZE];

            // once politely, and the 2nd time through, try to read after EOF
            for (int m = 0; m < 2; m++)
            {
                using (MemoryStream ms1 = new MemoryStream())
                {
                    Object[] args = { ms1, CompressionMode.Decompress, false };
                    using (Stream decompressor = (Stream)Activator.CreateInstance(t, args))
                    {
                        using (MemoryStream ms2 = new MemoryStream())
                        {
                            int n = -1;
                            while (n != 0)
                            {
                                n = decompressor.Read(working, 0, working.Length);
                                if (n > 0)
                                    ms2.Write(working, 0, n);
                            }

                            // we know there is no more data.  Want to insure it does
                            // not throw.
                            if (m == 1)
                                n = decompressor.Read(working, 0, working.Length);

                            Assert.Equal<Int64>(ms2.Length, 0L);
                        }
                    }
                }
            }
        }

        [Fact]
        public void Zlib_DeflateStream_InMemory()
        {
            String TextToCompress = UntilHeExtends;

            CompressionLevel[] levels =
            {
                CompressionLevel.Level0,
                CompressionLevel.Level1,
                CompressionLevel.Default,
                CompressionLevel.Level7,
                CompressionLevel.BestCompression,
            };

            // compress with various Ionic levels, and System.IO.Compression (default level)
            for (int k = 0; k < levels.Length + 1; k++)
            {
                MemoryStream ms = new MemoryStream();

                Stream compressor = null;
                if (k == levels.Length)
                    compressor = new System.IO.Compression.DeflateStream(
                        ms,
                        System.IO.Compression.CompressionMode.Compress,
                        false
                    );
                else
                {
                    compressor = new Ionic.Zlib.DeflateStream(
                        ms,
                        CompressionMode.Compress,
                        levels[k],
                        false
                    );
                    _output.WriteLine("using level: {0}", levels[k].ToString());
                }

                _output.WriteLine(
                    "Text to compress is {0} bytes: '{1}'",
                    TextToCompress.Length,
                    TextToCompress
                );
                _output.WriteLine("using compressor: {0}", compressor.GetType().FullName);

                StreamWriter sw = new StreamWriter(compressor, Encoding.ASCII);
                sw.Write(TextToCompress);
                sw.Close();

                var a = ms.ToArray();
                _output.WriteLine("Compressed stream is {0} bytes long", a.Length);

                // de-compress with both Ionic and System.IO.Compression
                for (int j = 0; j < 2; j++)
                {
                    var slow = new MySlowMemoryStream(a); // want to force EOF
                    Stream decompressor = null;

                    switch (j)
                    {
                        case 0:
                            decompressor = new Ionic.Zlib.DeflateStream(
                                slow,
                                CompressionMode.Decompress,
                                false
                            );
                            break;
                        case 1:
                            decompressor = new System.IO.Compression.DeflateStream(
                                slow,
                                System.IO.Compression.CompressionMode.Decompress,
                                false
                            );
                            break;
                    }

                    _output.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

                    var sr = new StreamReader(decompressor, Encoding.ASCII);
                    string DecompressedText = sr.ReadToEnd();

                    _output.WriteLine(
                        "Read {0} characters: '{1}'",
                        DecompressedText.Length,
                        DecompressedText
                    );
                    _output.WriteLine("\n");
                    Assert.Equal<String>(TextToCompress, DecompressedText);
                }
            }
        }

        [Fact]
        public void Zlib_CloseTwice()
        {
            string TextToCompress = LetMeDoItNow;

            for (int i = 0; i < 3; i++)
            {
                MemoryStream ms1 = new MemoryStream();

                Stream compressor = null;
                switch (i)
                {
                    case 0:
                        compressor = new DeflateStream(
                            ms1,
                            CompressionMode.Compress,
                            CompressionLevel.BestCompression,
                            false
                        );
                        break;
                    case 1:
                        compressor = new GZipStream(ms1, CompressionMode.Compress, false);
                        break;
                    case 2:
                        compressor = new ZlibStream(ms1, CompressionMode.Compress, false);
                        break;
                }

                _output.WriteLine(
                    "Text to compress is {0} bytes: '{1}'",
                    TextToCompress.Length,
                    TextToCompress
                );
                _output.WriteLine("using compressor: {0}", compressor.GetType().FullName);

                StreamWriter sw = new StreamWriter(compressor, Encoding.ASCII);
                sw.Write(TextToCompress);
                sw.Close(); // implicitly closes compressor
                sw.Close(); // implicitly closes compressor, again

                compressor.Close(); // explicitly closes compressor
                var a = ms1.ToArray();
                _output.WriteLine("Compressed stream is {0} bytes long", a.Length);

                var ms2 = new MemoryStream(a);
                Stream decompressor = null;

                switch (i)
                {
                    case 0:
                        decompressor = new DeflateStream(ms2, CompressionMode.Decompress, false);
                        break;
                    case 1:
                        decompressor = new GZipStream(ms2, CompressionMode.Decompress, false);
                        break;
                    case 2:
                        decompressor = new ZlibStream(ms2, CompressionMode.Decompress, false);
                        break;
                }

                _output.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

                var sr = new StreamReader(decompressor, Encoding.ASCII);
                string DecompressedText = sr.ReadToEnd();

                // verify that multiple calls to Close() do not throw
                sr.Close();
                sr.Close();
                decompressor.Close();

                _output.WriteLine(
                    "Read {0} characters: '{1}'",
                    DecompressedText.Length,
                    DecompressedText
                );
                _output.WriteLine("\n");
                Assert.Equal<String>(TextToCompress, DecompressedText);
            }
        }

        [Fact]
        public void Zlib_DisposedException_DeflateStream()
        {
            string TextToCompress = LetMeDoItNow;

            MemoryStream ms1 = new MemoryStream();

            Stream compressor = new DeflateStream(ms1, CompressionMode.Compress, false);

            _output.WriteLine(
                "Text to compress is {0} bytes: '{1}'",
                TextToCompress.Length,
                TextToCompress
            );
            _output.WriteLine("using compressor: {0}", compressor.GetType().FullName);

            StreamWriter sw = new StreamWriter(compressor, Encoding.ASCII);
            sw.Write(TextToCompress);
            sw.Close(); // implicitly closes compressor
            sw.Close(); // implicitly closes compressor, again

            compressor.Close(); // explicitly closes compressor
            var a = ms1.ToArray();
            _output.WriteLine("Compressed stream is {0} bytes long", a.Length);

            var ms2 = new MemoryStream(a);
            Stream decompressor = new DeflateStream(ms2, CompressionMode.Decompress, false);

            _output.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

            var sr = new StreamReader(decompressor, Encoding.ASCII);
            string DecompressedText = sr.ReadToEnd();
            sr.Close();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                _output.WriteLine("decompressor.CanRead = {0}", decompressor.CanRead);
            });

            _output.WriteLine(
                "Read {0} characters: '{1}'",
                DecompressedText.Length,
                DecompressedText
            );
            _output.WriteLine("\n");
            Assert.Equal<String>(TextToCompress, DecompressedText);
        }

        [Fact]
        public void Zlib_DisposedException_GZipStream()
        {
            string TextToCompress = IhaveaDream;

            MemoryStream ms1 = new MemoryStream();

            Stream compressor = new GZipStream(ms1, CompressionMode.Compress, false);

            _output.WriteLine(
                "Text to compress is {0} bytes: '{1}'",
                TextToCompress.Length,
                TextToCompress
            );
            _output.WriteLine("using compressor: {0}", compressor.GetType().FullName);

            StreamWriter sw = new StreamWriter(compressor, Encoding.ASCII);
            sw.Write(TextToCompress);
            sw.Close(); // implicitly closes compressor
            sw.Close(); // implicitly closes compressor, again

            compressor.Close(); // explicitly closes compressor
            var a = ms1.ToArray();
            _output.WriteLine("Compressed stream is {0} bytes long", a.Length);

            var ms2 = new MemoryStream(a);
            Stream decompressor = new GZipStream(ms2, CompressionMode.Decompress, false);

            _output.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

            var sr = new StreamReader(decompressor, Encoding.ASCII);
            string DecompressedText = sr.ReadToEnd();
            sr.Close();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                _output.WriteLine("decompressor.CanRead = {0}", decompressor.CanRead);
            });

            _output.WriteLine(
                "Read {0} characters: '{1}'",
                DecompressedText.Length,
                DecompressedText
            );
            _output.WriteLine("\n");
            Assert.Equal<String>(TextToCompress, DecompressedText);
        }

        [Fact]
        public void Zlib_DisposedException_ZlibStream()
        {
            string TextToCompress = IhaveaDream;

            MemoryStream ms1 = new MemoryStream();

            Stream compressor = new ZlibStream(ms1, CompressionMode.Compress, false);

            _output.WriteLine(
                "Text to compress is {0} bytes: '{1}'",
                TextToCompress.Length,
                TextToCompress
            );
            _output.WriteLine("using compressor: {0}", compressor.GetType().FullName);

            StreamWriter sw = new StreamWriter(compressor, Encoding.ASCII);
            sw.Write(TextToCompress);
            sw.Close(); // implicitly closes compressor
            sw.Close(); // implicitly closes compressor, again

            compressor.Close(); // explicitly closes compressor
            var a = ms1.ToArray();
            _output.WriteLine("Compressed stream is {0} bytes long", a.Length);

            var ms2 = new MemoryStream(a);
            Stream decompressor = new ZlibStream(ms2, CompressionMode.Decompress, false);

            _output.WriteLine("using decompressor: {0}", decompressor.GetType().FullName);

            var sr = new StreamReader(decompressor, Encoding.ASCII);
            string DecompressedText = sr.ReadToEnd();
            sr.Close();

            Assert.Throws<ObjectDisposedException>(() =>
            {
                _output.WriteLine("decompressor.CanRead = {0}", decompressor.CanRead);
            });

            _output.WriteLine(
                "Read {0} characters: '{1}'",
                DecompressedText.Length,
                DecompressedText
            );
            _output.WriteLine("\n");
            Assert.Equal<String>(TextToCompress, DecompressedText);
        }

        [Fact]
        public void Zlib_Streams_VariousSizes()
        {
            byte[] working = new byte[WORKING_BUFFER_SIZE];
            int n = -1;
            Int32[] Sizes = { 8000, 88000, 188000, 388000, 580000, 1580000 };

            for (int p = 0; p < Sizes.Length; p++)
            {
                // both binary and text files
                for (int m = 0; m < 2; m++)
                {
                    int sz = this.rnd.Next(Sizes[p]) + Sizes[p];
                    string FileToCompress = System.IO.Path.Combine(
                        TopLevelDir,
                        String.Format("Zlib_Streams.{0}.{1}", sz, (m == 0) ? "txt" : "bin")
                    );
                    Assert.False(
                        System.IO.File.Exists(FileToCompress),
                        String.Format("The temporary file '{0}' already exists.", FileToCompress)
                    );
                    _output.WriteLine("Creating file {0}   {1} bytes", FileToCompress, sz);
                    if (m == 0)
                        CreateAndFillFileText(FileToCompress, sz);
                    else
                        _CreateAndFillBinary(FileToCompress, sz, false);

                    int crc1 = DoCrc(FileToCompress);
                    _output.WriteLine("Initial CRC: 0x{0:X8}", crc1);

                    // try both GZipStream and DeflateStream
                    for (int k = 0; k < 2; k++)
                    {
                        // compress with Ionic and System.IO.Compression
                        for (int i = 0; i < 2; i++)
                        {
                            string CompressedFileRoot = String.Format(
                                "{0}.{1}.{2}.compressed",
                                FileToCompress,
                                (k == 0) ? "GZIP" : "DEFLATE",
                                (i == 0) ? "Ionic" : "BCL"
                            );

                            int x = k + i * 2;
                            int z = (x == 0) ? 4 : 1;
                            // why 4 trials??   (only for GZIP and Ionic)
                            for (int h = 0; h < z; h++)
                            {
                                string CompressedFile =
                                    (x == 0)
                                        ? CompressedFileRoot + ".trial" + h
                                        : CompressedFileRoot;

                                using (var input = System.IO.File.OpenRead(FileToCompress))
                                {
                                    using (var raw = System.IO.File.Create(CompressedFile))
                                    {
                                        Stream compressor = null;
                                        try
                                        {
                                            switch (x)
                                            {
                                                case 0: // k == 0, i == 0
                                                    compressor = new Ionic.Zlib.GZipStream(
                                                        raw,
                                                        CompressionMode.Compress,
                                                        true
                                                    );
                                                    break;
                                                case 1: // k == 1, i == 0
                                                    compressor = new Ionic.Zlib.DeflateStream(
                                                        raw,
                                                        CompressionMode.Compress,
                                                        true
                                                    );
                                                    break;
                                                case 2: // k == 0, i == 1
                                                    compressor =
                                                        new System.IO.Compression.GZipStream(
                                                            raw,
                                                            System
                                                                .IO
                                                                .Compression
                                                                .CompressionMode
                                                                .Compress,
                                                            true
                                                        );
                                                    break;
                                                case 3: // k == 1, i == 1
                                                    compressor =
                                                        new System.IO.Compression.DeflateStream(
                                                            raw,
                                                            System
                                                                .IO
                                                                .Compression
                                                                .CompressionMode
                                                                .Compress,
                                                            true
                                                        );
                                                    break;
                                            }
                                            //_output.WriteLine("Compress with: {0} ..", compressor.GetType().FullName);

                                            _output.WriteLine(
                                                "........{0} ...",
                                                System.IO.Path.GetFileName(CompressedFile)
                                            );

                                            if (x == 0)
                                            {
                                                if (h != 0)
                                                {
                                                    Ionic.Zlib.GZipStream gzip =
                                                        compressor as Ionic.Zlib.GZipStream;

                                                    if (h % 2 == 1)
                                                        gzip.FileName = FileToCompress;

                                                    if (h > 2)
                                                        gzip.Comment =
                                                            "Compressing: " + FileToCompress;
                                                }
                                            }

                                            n = -1;
                                            while (
                                                (n = input.Read(working, 0, working.Length)) != 0
                                            )
                                            {
                                                compressor.Write(working, 0, n);
                                            }
                                        }
                                        finally
                                        {
                                            if (compressor != null)
                                                compressor.Dispose();
                                        }
                                    }
                                }

                                // now, decompress with Ionic and System.IO.Compression
                                // for (int j = 0; j < 2; j++)
                                for (int j = 1; j >= 0; j--)
                                {
                                    using (var input = System.IO.File.OpenRead(CompressedFile))
                                    {
                                        Stream decompressor = null;
                                        try
                                        {
                                            int w = k + j * 2;
                                            switch (w)
                                            {
                                                case 0: // k == 0, j == 0
                                                    decompressor = new Ionic.Zlib.GZipStream(
                                                        input,
                                                        CompressionMode.Decompress,
                                                        true
                                                    );
                                                    break;
                                                case 1: // k == 1, j == 0
                                                    decompressor = new Ionic.Zlib.DeflateStream(
                                                        input,
                                                        CompressionMode.Decompress,
                                                        true
                                                    );
                                                    break;
                                                case 2: // k == 0, j == 1
                                                    decompressor =
                                                        new System.IO.Compression.GZipStream(
                                                            input,
                                                            System
                                                                .IO
                                                                .Compression
                                                                .CompressionMode
                                                                .Decompress,
                                                            true
                                                        );
                                                    break;
                                                case 3: // k == 1, j == 1
                                                    decompressor =
                                                        new System.IO.Compression.DeflateStream(
                                                            input,
                                                            System
                                                                .IO
                                                                .Compression
                                                                .CompressionMode
                                                                .Decompress,
                                                            true
                                                        );
                                                    break;
                                            }

                                            //_output.WriteLine("Decompress: {0} ...", decompressor.GetType().FullName);
                                            string DecompressedFile = String.Format(
                                                "{0}.{1}.decompressed",
                                                CompressedFile,
                                                (j == 0) ? "Ionic" : "BCL"
                                            );

                                            _output.WriteLine(
                                                "........{0} ...",
                                                System.IO.Path.GetFileName(DecompressedFile)
                                            );

                                            using (var s2 = System.IO.File.Create(DecompressedFile))
                                            {
                                                n = -1;
                                                while (n != 0)
                                                {
                                                    n = decompressor.Read(
                                                        working,
                                                        0,
                                                        working.Length
                                                    );
                                                    if (n > 0)
                                                        s2.Write(working, 0, n);
                                                }
                                            }

                                            int crc2 = DoCrc(DecompressedFile);
                                            Assert.Equal<UInt32>((UInt32)crc1, (UInt32)crc2);
                                        }
                                        finally
                                        {
                                            if (decompressor != null)
                                                decompressor.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            _output.WriteLine("Done.");
        }

        private void PerformTrialWi8870(byte[] buffer)
        {
            _output.WriteLine("Original");

            byte[] compressedBytes = null;
            using (MemoryStream ms1 = new MemoryStream())
            {
                using (
                    DeflateStream compressor = new DeflateStream(
                        ms1,
                        CompressionMode.Compress,
                        false
                    )
                )
                {
                    compressor.Write(buffer, 0, buffer.Length);
                }
                compressedBytes = ms1.ToArray();
            }

            _output.WriteLine(
                "Compressed {0} bytes into {1} bytes",
                buffer.Length,
                compressedBytes.Length
            );

            byte[] decompressed = null;
            using (MemoryStream ms2 = new MemoryStream())
            {
                using (
                    var deflateStream = new DeflateStream(ms2, CompressionMode.Decompress, false)
                )
                {
                    deflateStream.Write(compressedBytes, 0, compressedBytes.Length);
                }
                decompressed = ms2.ToArray();
            }

            _output.WriteLine("Decompressed");

            bool check = true;
            if (buffer.Length != decompressed.Length)
            {
                _output.WriteLine("Different lengths.");
                check = false;
            }
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] != decompressed[i])
                    {
                        _output.WriteLine("byte {0} differs", i);
                        check = false;
                        break;
                    }
                }
            }

            Assert.True(check, "Data check failed.");
        }

        private byte[] RandomizeBuffer(int length)
        {
            byte[] buffer = new byte[length];
            int mod1 = 86 + this.rnd.Next(46) / 2 + 1;
            int mod2 = 50 + this.rnd.Next(72) / 2 + 1;
            for (int i = 0; i < length; i++)
            {
                if (i > 200)
                    buffer[i] = (byte)(i % mod1);
                else if (i > 100)
                    buffer[i] = (byte)(i % mod2);
                else if (i > 42)
                    buffer[i] = (byte)(i % 33);
                else
                    buffer[i] = (byte)i;
            }
            return buffer;
        }

        [Fact]
        public void Zlib_DeflateStream_wi8870()
        {
            for (int j = 0; j < 1000; j++)
            {
                byte[] buffer = RandomizeBuffer(117 + (this.rnd.Next(3) * 100));
                PerformTrialWi8870(buffer);
            }
        }

        [Fact]
        public void Zlib_ParallelDeflateStream()
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            _output.WriteLine("{0}: Zlib_ParallelDeflateStream Start", sw.Elapsed);

            int sz = 256 * 1024 + this.rnd.Next(120000);
            string FileToCompress = System.IO.Path.Combine(
                TopLevelDir,
                String.Format("Zlib_ParallelDeflateStream.{0}.txt", sz)
            );

            CreateAndFillFileText(FileToCompress, sz);

            _output.WriteLine("{0}: Created file: {1}", sw.Elapsed, FileToCompress);

            byte[] original = File.ReadAllBytes(FileToCompress);

            int crc1 = DoCrc(FileToCompress);

            _output.WriteLine("{0}: Original CRC: {1:X8}", sw.Elapsed, crc1);

            byte[] working = new byte[WORKING_BUFFER_SIZE];
            int n = -1;
            long originalLength;
            MemoryStream ms1 = new MemoryStream();
            {
                using (FileStream fs1 = File.OpenRead(FileToCompress))
                {
                    originalLength = fs1.Length;
                    using (var compressor = new Ionic.Zlib.ParallelDeflateOutputStream(ms1, true))
                    {
                        while ((n = fs1.Read(working, 0, working.Length)) != 0)
                        {
                            compressor.Write(working, 0, n);
                        }
                    }
                }
                ms1.Seek(0, SeekOrigin.Begin);
            }

            _output.WriteLine(
                "{0}: Compressed {1} bytes into {2} bytes",
                sw.Elapsed,
                originalLength,
                ms1.Length
            );

            var crc = new Ionic.Crc.CRC32();
            int crc2 = 0;
            byte[] decompressedBytes = null;
            using (MemoryStream ms2 = new MemoryStream())
            {
                using (var decompressor = new DeflateStream(ms1, CompressionMode.Decompress, false))
                {
                    while ((n = decompressor.Read(working, 0, working.Length)) != 0)
                    {
                        ms2.Write(working, 0, n);
                    }
                }
                _output.WriteLine("{0}: Decompressed", sw.Elapsed);
                _output.WriteLine("{0}: Decompressed length: {1}", sw.Elapsed, ms2.Length);
                ms2.Seek(0, SeekOrigin.Begin);
                crc2 = crc.GetCrc32(ms2);
                decompressedBytes = ms2.ToArray();
                _output.WriteLine("{0}: Decompressed CRC: {1:X8}", sw.Elapsed, crc2);
            }

            _output.WriteLine("{0}: Checking...", sw.Elapsed);

            bool check = true;
            if (originalLength != decompressedBytes.Length)
            {
                _output.WriteLine("Different lengths.");
                check = false;
            }
            else
            {
                for (int i = 0; i < decompressedBytes.Length; i++)
                {
                    if (original[i] != decompressedBytes[i])
                    {
                        _output.WriteLine("byte {0} differs", i);
                        check = false;
                        break;
                    }
                }
            }

            Assert.True(check, "Data check failed");
            _output.WriteLine("{0}: Done...", sw.Elapsed);
        }

        private int DoCrc(string filename)
        {
            using (Stream a = File.OpenRead(filename))
            using (var crc = new Ionic.Crc.CrcCalculatorStream(a))
            {
                byte[] working = new byte[WORKING_BUFFER_SIZE];
                int n = -1;
                while (n != 0)
                    n = crc.Read(working, 0, working.Length);
                return crc.Crc;
            }
        }

        private static void _CreateAndFillBinary(string Filename, Int64 size, bool zeroes)
        {
            Int64 bytesRemaining = size;
            System.Random rnd = new System.Random();
            // fill with binary data
            byte[] Buffer = new byte[20000];
            using (
                System.IO.Stream fileStream = new System.IO.FileStream(
                    Filename,
                    System.IO.FileMode.Create,
                    System.IO.FileAccess.Write
                )
            )
            {
                while (bytesRemaining > 0)
                {
                    int sizeOfChunkToWrite =
                        (bytesRemaining > Buffer.Length) ? Buffer.Length : (int)bytesRemaining;
                    if (!zeroes)
                        rnd.NextBytes(Buffer);
                    fileStream.Write(Buffer, 0, sizeOfChunkToWrite);
                    bytesRemaining -= sizeOfChunkToWrite;
                }
                fileStream.Close();
            }
        }

        internal static void CreateAndFillFileText(string Filename, Int64 size)
        {
            Int64 bytesRemaining = size;
            System.Random rnd = new System.Random();
            // fill the file with text data
            using (System.IO.StreamWriter sw = System.IO.File.CreateText(Filename))
            {
                do
                {
                    // pick a word at random
                    string selectedWord = LoremIpsumWords[rnd.Next(LoremIpsumWords.Length)];
                    if (bytesRemaining < selectedWord.Length + 1)
                    {
                        sw.Write(selectedWord.Substring(0, (int)bytesRemaining));
                        bytesRemaining = 0;
                    }
                    else
                    {
                        sw.Write(selectedWord);
                        sw.Write(" ");
                        bytesRemaining -= (selectedWord.Length + 1);
                    }
                } while (bytesRemaining > 0);
                sw.Close();
            }
        }

        [Fact]
        public void TestAdler32()
        {
            // create a buffer full of 0xff's
            var buffer = new byte[2048 * 4];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 255;
            }
            ;

            uint goal = 4104380882;
            var testAdler = new Action<int>(chunk =>
            {
                var index = 0;
                var adler = Adler.Adler32(0, null, 0, 0);
                while (index < buffer.Length)
                {
                    var length = Math.Min(buffer.Length - index, chunk);
                    adler = Adler.Adler32(adler, buffer, index, length);
                    index = index + chunk;
                }
                Assert.Equal<uint>(adler, goal);
            });

            testAdler(3979);
            testAdler(3980);
            testAdler(3999);
        }

        internal static string LetMeDoItNow =
            "I expect to pass through the world but once. Any good therefore that I can do, or any kindness I can show to any creature, let me do it now. Let me not defer it, for I shall not pass this way again. -- Anonymous, although some have attributed it to Stephen Grellet";

        internal static string UntilHeExtends =
            "Until he extends the circle of his compassion to all living things, man will not himself find peace. - Albert Schweitzer, early 20th-century German Nobel Peace Prize-winning mission doctor and theologian.";

        internal static string WhatWouldThingsHaveBeenLike =
            "'What would things have been like [in Russia] if during periods of mass arrests people had not simply sat there, paling with terror at every bang on the downstairs door and at every step on the staircase, but understood they had nothing to lose and had boldly set up in the downstairs hall an ambush of half a dozen people?' -- Alexander Solzhenitsyn";

        internal static string GoPlacidly =
            @"Go placidly amid the noise and haste, and remember what peace there may be in silence.

As far as possible, without surrender, be on good terms with all persons. Speak your truth quietly and clearly; and listen to others, even to the dull and the ignorant, they too have their story. Avoid loud and aggressive persons, they are vexations to the spirit.

If you compare yourself with others, you may become vain and bitter; for always there will be greater and lesser persons than yourself. Enjoy your achievements as well as your plans. Keep interested in your own career, however humble; it is a real possession in the changing fortunes of time.

Exercise caution in your business affairs, for the world is full of trickery. But let this not blind you to what virtue there is; many persons strive for high ideals, and everywhere life is full of heroism. Be yourself. Especially, do not feign affection. Neither be cynical about love, for in the face of all aridity and disenchantment it is perennial as the grass.

Take kindly to the counsel of the years, gracefully surrendering the things of youth. Nurture strength of spirit to shield you in sudden misfortune. But do not distress yourself with imaginings. Many fears are born of fatigue and loneliness.

Beyond a wholesome discipline, be gentle with yourself. You are a child of the universe, no less than the trees and the stars; you have a right to be here. And whether or not it is clear to you, no doubt the universe is unfolding as it should.

Therefore be at peace with God, whatever you conceive Him to be, and whatever your labors and aspirations, in the noisy confusion of life, keep peace in your soul.

With all its sham, drudgery and broken dreams, it is still a beautiful world.

Be cheerful. Strive to be happy.

Max Ehrmann c.1920
";

        internal static string IhaveaDream =
            @"Let us not wallow in the valley of despair, I say to you today, my friends.

And so even though we face the difficulties of today and tomorrow, I still have a dream. It is a dream deeply rooted in the American dream.

I have a dream that one day this nation will rise up and live out the true meaning of its creed: 'We hold these truths to be self-evident, that all men are created equal.'

I have a dream that one day on the red hills of Georgia, the sons of former slaves and the sons of former slave owners will be able to sit down together at the table of brotherhood.

I have a dream that one day even the state of Mississippi, a state sweltering with the heat of injustice, sweltering with the heat of oppression, will be transformed into an oasis of freedom and justice.

I have a dream that my four little children will one day live in a nation where they will not be judged by the color of their skin but by the content of their character.

I have a dream today!

I have a dream that one day, down in Alabama, with its vicious racists, with its governor having his lips dripping with the words of 'interposition' and 'nullification' -- one day right there in Alabama little black boys and black girls will be able to join hands with little white boys and white girls as sisters and brothers.

I have a dream today!

I have a dream that one day every valley shall be exalted, and every hill and mountain shall be made low, the rough places will be made plain, and the crooked places will be made straight; 'and the glory of the Lord shall be revealed and all flesh shall see it together.'2
";

        internal static string LoremIpsum =
            "Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Integer "
            + "vulputate, nibh non rhoncus euismod, erat odio pellentesque lacus, sit "
            + "amet convallis mi augue et odio. Phasellus cursus urna facilisis "
            + "quam. Suspendisse nec metus et sapien scelerisque euismod. Nullam "
            + "molestie sem quis nisl. Fusce pellentesque, ante sed semper egestas, sem "
            + "nulla vestibulum nulla, quis sollicitudin leo lorem elementum "
            + "wisi. Aliquam vestibulum nonummy orci. Sed in dolor sed enim ullamcorper "
            + "accumsan. Duis vel nibh. Class aptent taciti sociosqu ad litora torquent "
            + "per conubia nostra, per inceptos hymenaeos. Sed faucibus, enim sit amet "
            + "venenatis laoreet, nisl elit posuere est, ut sollicitudin tortor velit "
            + "ut ipsum. Aliquam erat volutpat. Phasellus tincidunt vehicula "
            + "eros. Curabitur vitae erat. "
            + "\n "
            + "Quisque pharetra lacus quis sapien. Duis id est non wisi sagittis "
            + "adipiscing. Nulla facilisi. Etiam quam erat, lobortis eu, facilisis nec, "
            + "blandit hendrerit, metus. Fusce hendrerit. Nunc magna libero, "
            + "sollicitudin non, vulputate non, ornare id, nulla.  Suspendisse "
            + "potenti. Nullam in mauris. Curabitur et nisl vel purus vehicula "
            + "sodales. Class aptent taciti sociosqu ad litora torquent per conubia "
            + "nostra, per inceptos hymenaeos. Cum sociis natoque penatibus et magnis "
            + "dis parturient montes, nascetur ridiculus mus. Donec semper, arcu nec "
            + "dignissim porta, eros odio tempus pede, et laoreet nibh arcu et "
            + "nisl. Morbi pellentesque eleifend ante. Morbi dictum lorem non "
            + "ante. Nullam et augue sit amet sapien varius mollis. "
            + "\n "
            + "Nulla erat lorem, fringilla eget, ultrices nec, dictum sed, "
            + "sapien. Aliquam libero ligula, porttitor scelerisque, lobortis nec, "
            + "dignissim eu, elit. Etiam feugiat, dui vitae laoreet faucibus, tellus "
            + "urna molestie purus, sit amet pretium lorem pede in erat.  Ut non libero "
            + "et sapien porttitor eleifend. Vestibulum ante ipsum primis in faucibus "
            + "orci luctus et ultrices posuere cubilia Curae; In at lorem et lacus "
            + "feugiat iaculis. Nunc tempus eros nec arcu tristique egestas. Quisque "
            + "metus arcu, pretium in, suscipit dictum, bibendum sit amet, "
            + "mauris. Aliquam non urna. Suspendisse eget diam. Aliquam erat "
            + "volutpat. In euismod aliquam lorem. Mauris dolor nisl, consectetuer sit "
            + "amet, suscipit sodales, rutrum in, lorem. Nunc nec nisl. Nulla ante "
            + "libero, aliquam porttitor, aliquet at, imperdiet sed, diam. Pellentesque "
            + "tincidunt nisl et ipsum. Suspendisse purus urna, semper quis, laoreet "
            + "in, vestibulum vel, arcu. Nunc elementum eros nec mauris. "
            + "\n "
            + "Vivamus congue pede at quam. Aliquam aliquam leo vel turpis. Ut "
            + "commodo. Integer tincidunt sem a risus. Cras aliquam libero quis "
            + "arcu. Integer posuere. Nulla malesuada, wisi ac elementum sollicitudin, "
            + "libero libero molestie velit, eu faucibus est ante eu libero. Sed "
            + "vestibulum, dolor ac ultricies consectetuer, tellus risus interdum diam, "
            + "a imperdiet nibh eros eget mauris. Donec faucibus volutpat "
            + "augue. Phasellus vitae arcu quis ipsum ultrices fermentum. Vivamus "
            + "ultricies porta ligula. Nullam malesuada. Ut feugiat urna non "
            + "turpis. Vivamus ipsum. Vivamus eleifend condimentum risus. Curabitur "
            + "pede. Maecenas suscipit pretium tortor. Integer pellentesque. "
            + "\n "
            + "Mauris est. Aenean accumsan purus vitae ligula. Lorem ipsum dolor sit "
            + "amet, consectetuer adipiscing elit. Nullam at mauris id turpis placerat "
            + "accumsan. Sed pharetra metus ut ante. Aenean vel urna sit amet ante "
            + "pretium dapibus. Sed nulla. Sed nonummy, lacus a suscipit semper, erat "
            + "wisi convallis mi, et accumsan magna elit laoreet sem. Nam leo est, "
            + "cursus ut, molestie ac, laoreet id, mauris. Suspendisse auctor nibh. "
            + "\n";

        static string[] LoremIpsumWords;

        private const int WORKING_BUFFER_SIZE = 0x4000;
    }

    public class MySlowMemoryStream : MemoryStream
    {
        // ctor
        public MySlowMemoryStream(byte[] bytes)
            : base(bytes, false) { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException();

            if (count == 0)
                return 0;

            // force stream to read just one byte at a time
            int NextByte = base.ReadByte();
            if (NextByte == -1)
                return 0;

            buffer[offset] = (byte)NextByte;
            return 1;
        }
    }
}
