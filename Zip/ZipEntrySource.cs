// ZipEntrySource.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
// This code is licensed under the Apache 2.0 License.
// See the file LICENSE.txt that accompanies the source code, for the license details.
//
// ------------------------------------------------------------------

namespace Ionic.Zip
{
    /// <summary>
    /// An enum that specifies the source of the ZipEntry.
    /// </summary>
    public enum ZipEntrySource
    {
        /// <summary>
        /// Default value.  Invalid on a bonafide ZipEntry.
        /// </summary>
        None = 0,

        /// <summary>
        /// The entry was instantiated by calling AddFile() or another method that
        /// added an entry from the filesystem.
        /// </summary>
        FileSystem,

        /// <summary>
        /// The entry was instantiated via <see cref="Ionic.Zip.ZipFile.AddEntry(string,string)"/> or
        /// <see cref="Ionic.Zip.ZipFile.AddEntry(string,System.IO.Stream)"/> .
        /// </summary>
        Stream,

        /// <summary>
        /// The ZipEntry was instantiated by reading a zipfile.
        /// </summary>
        ZipFile,

        /// <summary>
        /// The content for the ZipEntry will be or was provided by the WriteDelegate.
        /// </summary>
        WriteDelegate,

        /// <summary>
        /// The content for the ZipEntry will be obtained from the stream dispensed by the <c>OpenDelegate</c>.
        /// The entry was instantiated via <see cref="Ionic.Zip.ZipFile.AddEntry(string,OpenDelegate,CloseDelegate)"/>.
        /// </summary>
        JitStream,

        /// <summary>
        /// The content for the ZipEntry will be or was obtained from a <c>ZipOutputStream</c>.
        /// </summary>
        ZipOutputStream,
    }

}
