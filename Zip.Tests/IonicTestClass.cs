// IonicTestClass.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009, 2025 Dino Chiesa.
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

using Xunit.Abstractions;
using Assert = XunitAssertMessages.AssertM;

namespace Ionic.Zip.Tests.Utilities
{
    public class IonicTestClass : IDisposable
    {
        protected System.Random _rnd;
        protected System.Collections.Generic.List<string> _DirsToRemove;
        protected string CurrentDir = null;
        protected string TopLevelDir = null;
        private string _wzunzip = null;
        private string _wzzip = null;
        private string _sevenzip = null;
        private string _gzip = null;
        private string _infozipzip = null;
        private string _infozipunzip = null;
        private bool? _GZipIsPresent;
        private bool? _WinZipIsPresent;
        private bool? _SevenZipIsPresent;
        private bool? _InfoZipIsPresent;
        protected static string TEMP = System.Environment.GetEnvironmentVariable("TEMP");

        //protected Ionic.CopyData.Transceiver _txrx;

        protected ITestOutputHelper _output;

        public IonicTestClass()
        {
            _rnd = new System.Random();
            _DirsToRemove = new System.Collections.Generic.List<string>();
            CurrentDir = Directory.GetCurrentDirectory();
            TestUtilities.Initialize(out TopLevelDir);
            _DirsToRemove.Add(TopLevelDir);
        }

        public void Dispose()
        {
            TestUtilities.CleanUp(CurrentDir, _DirsToRemove, _output);

            // check that the test did not leave rubbish in the wrong dir
            string testSrc = TestUtilities.GetTestSrcDir();
            string binDir = Path.Combine(testSrc, "bin\\Debug\\net9.0");
            string[] entries = Directory.GetFileSystemEntries(binDir);
            // foreach (var fn in entries) {
            //     _output.WriteLine("  fn: {0}", fn);
            // }
            Assert.False(entries.Any(f => f.Replace(binDir+"\\", "").StartsWith("verify")), "rubbish in the bin directory");
            Assert.False(entries.Any(f => f.Replace(binDir+"\\", "").StartsWith("unpack")), "rubbish in the bin directory");
            Assert.False(entries.Any(f => f.EndsWith(".zip")), "rubbish in the bin directory");
            Assert.False(entries.Any(f => f.EndsWith(".txt")), "rubbish in the bin directory");
            Assert.False(entries.Length > 46, "rubbish in the bin directory");
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

            // Microsoft.VisualStudio.TestTools.UnitTesting
            this._output.WriteLine("running command: {0} {1}", program, args);

            string output;
            int rc = TestUtilities.Exec_NoContext(program, args, waitForExit, out output);

            if (rc != 0)
                throw new Exception(String.Format("Non-zero RC {0}: {1}", program, output));

            if (emitOutput)
                this._output.WriteLine("output: {0}", output);
            else
                this._output.WriteLine("A-OK. (output suppressed)");

            return output;
        }


        public class AsyncReadState
        {
            public System.IO.Stream s;
            public byte[] buf= new byte[1024];
        }


        internal int ExecRedirectStdOut(string program, string args, string outFile)
        {
            if (program == null)
                throw new ArgumentException("program");

            if (args == null)
                throw new ArgumentException("args");

            this._output.WriteLine("running command: {0} {1}", program, args);

            Stream fs = File.Create(outFile);
            try
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
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                };

                p.Start();

                var stdout = p.StandardOutput.BaseStream;
                var rs = new AsyncReadState { s = stdout };
                Action<System.IAsyncResult> readAsync1 = null;
                var readAsync = new Action<System.IAsyncResult>( (ar) => {
                        AsyncReadState state = (AsyncReadState) ar.AsyncState;
                        int n = state.s.EndRead(ar);
                        if (n > 0)
                        {
                            fs.Write(state.buf, 0, n);
                            state.s.BeginRead(state.buf,
                                              0,
                                              state.buf.Length,
                                              new System.AsyncCallback(readAsync1),
                                              state);
                        }
                    });
                readAsync1 = readAsync; // ??

                // kickoff
                stdout.BeginRead(rs.buf,
                                 0,
                                 rs.buf.Length,
                                 new System.AsyncCallback(readAsync),
                                 rs);

                p.WaitForExit();

                this._output.WriteLine("Process exited, rc={0}", p.ExitCode);

                return p.ExitCode;
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }
        }


        protected string sevenZip
        {
            get { return SevenZipIsPresent ? _sevenzip : null; }
        }

        protected string gzip
        {
            get { return GZipIsPresent ? _gzip : null; }
        }

        protected string infoZip
        {
            get { return InfoZipIsPresent ? _infozipzip : null; }
        }

        protected string infoZipUnzip
        {
            get { return InfoZipIsPresent ? _infozipunzip : null; }
        }

        protected string wzzip
        {
            get { return WinZipIsPresent ? _wzzip : null; }
        }

        protected string wzunzip
        {
            get { return WinZipIsPresent ? _wzunzip : null; }
        }

        protected bool GZipIsPresent
        {
            get
            {
                if (_GZipIsPresent == null)
                {

                    string sourceDir = TestUtilities.GetTestSrcDir();
                    _gzip =
                        Path.Combine(sourceDir, "..\\Tools\\GZip\\bin\\Debug\\net9.0\\GZip.exe");

                    _GZipIsPresent = new Nullable<bool>(File.Exists(_gzip));
                }
                return _GZipIsPresent.Value;
            }
        }

        protected bool WinZipIsPresent
        {
            get
            {
                if (_WinZipIsPresent == null)
                {
                    string progfiles = null;
                    if (_wzunzip == null || _wzzip == null)
                    {
                        progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                        _wzunzip = Path.Combine(progfiles, "winzip\\wzunzip.exe");
                        _wzzip = Path.Combine(progfiles, "winzip\\wzzip.exe");
                    }
                    _WinZipIsPresent = new Nullable<bool>(File.Exists(_wzunzip) && File.Exists(_wzzip));
                }
                return _WinZipIsPresent.Value;
            }
        }

        protected bool SevenZipIsPresent
        {
            get
            {
                if (_SevenZipIsPresent == null)
                {
                    string progfiles = null;
                    if (_sevenzip == null)
                    {
                        progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles");
                        _sevenzip = Path.Combine(progfiles, "7-zip\\7z.exe");
                    }
                    _SevenZipIsPresent = new Nullable<bool>(File.Exists(_sevenzip));
                }
                return _SevenZipIsPresent.Value;
            }
        }


        protected bool InfoZipIsPresent
        {
            get
            {
                if (_InfoZipIsPresent == null)
                {
                    string progfiles = null;
                    if (_infozipzip == null)
                    {
                        progfiles = System.Environment.GetEnvironmentVariable("ProgramFiles(x86)");
                        _infozipzip = Path.Combine(progfiles, "infozip.org\\zip.exe");
                        _infozipunzip = Path.Combine(progfiles, "infozip.org\\unzip.exe");
                    }
                    _InfoZipIsPresent = new Nullable<bool>(File.Exists(_infozipzip) &&
                                                           File.Exists(_infozipunzip));
                }
                return _InfoZipIsPresent.Value;
            }
        }

        internal string BasicVerifyZip(string zipfile)
        {
            return BasicVerifyZip(zipfile, null);
        }


        internal string BasicVerifyZip(string zipfile, string password)
        {
            return BasicVerifyZip(zipfile, password, true);
        }

        internal string BasicVerifyZip(string zipfile, string password, bool emitOutput)
        {
            return BasicVerifyZip(zipfile, password, emitOutput, null);
        }


        internal string BasicVerifyZip(string zipfile, string password, bool emitOutput,
                                       EventHandler<ExtractProgressEventArgs> extractProgress)
        {
            // basic verification of the zip file - can it be extracted?
            // The extraction tool will verify checksums and passwords, as appropriate
#if NOT
            if (WinZipIsPresent)
            {
                TestContext.WriteLine("Verifying zip file {0} with WinZip", zipfile);
                string args = (password == null)
                    ? String.Format("-t {0}", zipfile)
                    : String.Format("-s{0} -t {1}", password, zipfile);

                string wzunzipOut = this.Exec(wzunzip, args, true, emitOutput);
            }
            else
#endif
            {
                string tld = Path.GetDirectoryName(zipfile);

                _output.WriteLine("Verifying zip file with DotNetZip...");
                _output.WriteLine("   file {0} ", (zipfile!=null)? zipfile.Replace(TEMP,"") : "--null--");

                ReadOptions options = new ReadOptions();
                if (emitOutput)
                    options.StatusMessageWriter = new StringWriter();

                string extractDir = Path.Combine(tld, TestUtilities.UniqueDir("verify"));

                using (ZipFile zip2 = ZipFile.Read(zipfile, options))
                {
                    zip2.Password = password;
                    if (extractProgress != null)
                        zip2.ExtractProgress += extractProgress;
                    zip2.ExtractAll(extractDir);
                }
                // emit output, as desired
                if (emitOutput)
                    _output.WriteLine("{0}",options.StatusMessageWriter.ToString());

                return extractDir;
            }
        }

        internal static void CreateFilesAndChecksums(string subdir,
                                                     out string[] filesToZip,
                                                     out Dictionary<string, byte[]> checksums)
        {
            CreateFilesAndChecksums(subdir, 0, 0, out filesToZip, out checksums);
        }


        internal static void CreateFilesAndChecksums(string subdir,
                                                     int numFiles,
                                                     int baseSize,
                                                     out string[] filesToZip,
                                                     out Dictionary<string, byte[]> checksums)
        {
            // create a bunch of files
            filesToZip = TestUtilities.GenerateFilesFlat(subdir, numFiles, baseSize);
            DateTime atMidnight = new DateTime(DateTime.Now.Year,
                                               DateTime.Now.Month,
                                               DateTime.Now.Day);
            DateTime fortyFiveDaysAgo = atMidnight - new TimeSpan(45, 0, 0, 0);

            // get checksums for each one
            checksums = new Dictionary<string, byte[]>();

            var rnd = new System.Random();
            foreach (var f in filesToZip)
            {
                if (rnd.Next(3) == 0)
                    File.SetLastWriteTime(f, fortyFiveDaysAgo);
                else
                    File.SetLastWriteTime(f, atMidnight);

                var key = Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
        }

        protected static void CreateLargeFilesWithChecksums
            (string subdir,
             int numFiles,
             Action<int,int,Int64> update,
             out string[] filesToZip,
             out Dictionary<string,byte[]> checksums)
        {
            var rnd = new System.Random();
            // create a bunch of files
            filesToZip = TestUtilities.GenerateFilesFlat(subdir,
                                                         numFiles,
                                                         256 * 1024,
                                                         3 * 1024 * 1024,
                                                         update);

            var dates = new DateTime[rnd.Next(6) + 7];
             // midnight
            dates[0] = new DateTime(DateTime.Now.Year,
                                    DateTime.Now.Month,
                                    DateTime.Now.Day);

            for (int i=1; i < dates.Length; i++)
            {
                dates[i] = DateTime.Now -
                    new TimeSpan(rnd.Next(300),
                                 rnd.Next(23),
                                 rnd.Next(60),
                                 rnd.Next(60));
            }

            // get checksums for each one
            checksums = new Dictionary<string, byte[]>();

            foreach (var f in filesToZip)
            {
                File.SetLastWriteTime(f, dates[rnd.Next(dates.Length)]);
                var key = Path.GetFileName(f);
                var chk = TestUtilities.ComputeChecksum(f);
                checksums.Add(key, chk);
            }
        }



        protected void VerifyChecksums(string extractDir,
            System.Collections.Generic.IEnumerable<String> filesToCheck,
            Dictionary<string, byte[]> checksums)
        {
            _output.WriteLine("");
            _output.WriteLine("Verify checksums...");
            int count = 0;
            foreach (var fqPath in filesToCheck)
            {
                var f = Path.GetFileName(fqPath);
                var extractedFile = Path.Combine(extractDir, f);
                Assert.True(File.Exists(extractedFile), String.Format("File does not exist ({0})", extractedFile));
                var chk = TestUtilities.ComputeChecksum(extractedFile);
                Assert.Equal<String>(TestUtilities.CheckSumToString(checksums[f]),
                                        TestUtilities.CheckSumToString(chk),
                                        String.Format("Checksums for file {0} do not match.", f));
                count++;
            }

            if (checksums.Count < count)
            {
                _output.WriteLine("There are {0} more extracted files than checksums", count - checksums.Count);
                foreach (var file in filesToCheck)
                {
                    if (!checksums.ContainsKey(file))
                        _output.WriteLine("Missing: {0}", Path.GetFileName(file));
                }
            }

            if (checksums.Count > count)
            {
                _output.WriteLine("There are {0} more checksums than extracted files", checksums.Count - count);
                foreach (var file in checksums.Keys)
                {
                    var selection = from f in filesToCheck where Path.GetFileName(f).Equals(file) select f;

                    if (selection.Count() == 0)
                        _output.WriteLine("Missing: {0}", Path.GetFileName(file));
                }
            }


            Assert.Equal<Int32>(checksums.Count, count, "There's a mismatch between the checksums and the filesToCheck.");
        }

        internal static int CountEntries(string zipfile)
        {
            int entries = 0;
            using (ZipFile zip = ZipFile.Read(zipfile))
            {
                foreach (ZipEntry e in zip)
                    if (!e.IsDirectory) entries++;
            }
            return entries;
        }

    }


}
