# DotNetZip - Zip and Unzip in .NET (C# or any .NET language)
DotNetZip is a .NET class library and toolset for manipulating zip files. Use it to easily create, extract, or update zip files,
within any .NET program.

Originally created in 2008, based on the full .NET Framework and Windows, the current version is
reduced in scope, and runs on .NET 9.

DotNetZip supports these scenarios:
- a Windows Service that periodically zips up a directory for backup and archival purposes
- a Windows Forms app that creates AES-encrypted zip archives for privacy of archived content.
- An administrative script in PowerShell or VBScript that performs backup and archival.
- creating zip files from stream content, saving to a stream, extracting to a stream, reading from a stream
- creation of self-extracting archives.

If all you want is a better DeflateStream or GZipStream class to replace the one that is
built-into the .NET BCL, DotNetZip has that, too. DotNetZip's DeflateStream and
GZipStream are available in a standalone assembly, based on a .NET port of Zlib. These
streams support compression levels and deliver much better performance than the built-in
classes. There is also a ZlibStream to complete the set (RFC 1950, 1951, 1952).

### To use

Find this on Nuget at
[DotNetZip.Original](https://www.nuget.org/packages/DotNetZip.Original/), or add
a reference to the package from the command line:

```
dotnet add package DotNetZip.Original --version 2025.1.26
```


### Example Usage

Here's some C# code that creates a zip file.
```
using (ZipFile zip = new ZipFile())
{
  // add this map file into the "images" directory in the zip archive
  zip.AddFile("c:\\images\\personal\\7440-N49th.png", "images");
  // add the report into a different directory in the archive
  zip.AddFile("c:\\Reports\\2008-Regional-Sales-Report.pdf", "files");
  zip.AddFile("ReadMe.txt");
  zip.Save("MyZipFile.zip");
}
```

And this shows how you can read an existing zip file, and optionally extract entries.
```
using (ZipFile zip = ZipFile.Read("c:\\users\\me\\Downloads\\archive.zip"))
{
  Console.WriteLine("That zipfile contains {0} entries.", zip.Entries.Count);
  foreach (var entry in zip)
  {
     // choose to extract or not
     if (e.FileName.EndsWith(".txt")) {
       e.Extract(extractDir);
     }
  }
}
```

Here is some VB code that unpacks a zip file (extracts all the entries):
```
  Dim ZipToUnpack As String = "C1P3SML.zip"
   Dim TargetDir As String = "C1P3SML"
   Console.WriteLine("Extracting file {0} to {1}", ZipToUnpack, TargetDir)
   Using zip1 As ZipFile = ZipFile.Read(ZipToUnpack)
       AddHandler zip1.ExtractProgress, AddressOf MyExtractProgress
       Dim e As ZipEntry
       ' here, we extract every entry, but we could extract
       ' based on entry name, size, date, etc.
       For Each e In zip1
           e.Extract(TargetDir, ExtractExistingFileAction.OverwriteSilently)
       Next
   End Using
```

## A Brief List of Features

- Class library to read, extract, and update ZIP files
- Read or write to files or streams
- Libraries for BZip and GZip as well
- support for various code pages (IBM437 is the default, per the ZIP spec)
- Can read/write zip files with entry timestamps in Windows format, or in Unix format. (I'll bet you didn't know that zip files had different formats for timestamps)
- Supports ZIP64 for archives and files over 4.2gb
- support for Zip Input and Output streams
- Support for DeflateStream and GZipStream
- Event handlers for monitoring progress of Save and Extract; handy for long-running operations
- support for Unicode comments on zip entries and archives
- interface allows you to programmatically specify what to do when extracting would overwrite an existing file
- lots more...


## History, and Changes from the Original DotNetZip

This version of DotNetZip is now built on .NET Core.

When I created DotNetZip, I built versions of the library for the .NET
Framework, the .NET Compact Framework, and for Silverlight. Also I built in
COM-Interop, so you could invoke the library via any COM-enabled environment,
like VB6 or perl, and so on.  None of these are in scope, currently.


This version of DotNetZip builds on .NET Core. It has no dependencies on Windows
libraries.


## Why was DotNetZip Created?

The System.IO.Compression namespace available in the Microsoft .NET Framework
{v2.0 v3.0 v3.5} included base class libraries supporting compression within
streams - both the Deflate (ietf rfc-1951) and Gzip (ietf rfc-1952) compression
formats are supported, in the DeflateStream and GZipStream classes,
respectively.

But the System.IO.Compression namespace provides streaming compression only -
useful for compressing a single stream or block of bytes, but not directly
useful for creating compressed archives, like .zip files or .gzip files. The
compressed archives, in addition to containing the compressed stream (or file)
data, also include header information, what might be called "metadata". For .zip
files, the format is defined by PKWare Inc; For gzip, the format is described in
RFC-1952. But the System.IO.Compression.DeflateStream class does not parse the
metadata for such files. The classes in the System.IO.Compression namespace do
not (or did not, at the time DotNetZip was created) directly support formatting zip or gzip archive headers and so on.

As a result, if you wanted to build an application with the .NET Framework,
c.2008, and you wanted to manipulate .ZIP files, you needed an external
library to do so.

I wrote this class library to provide the handling for Zip files that the .NET Framework base class library lacked. Using this library, you can build .NET applications that read and write zip-format files. The library relies on the System.IO.Compression.DeflateStream class, to perform compression. Around that capability, the library adds logic for formatting zip headers, doing encryption and decryption, verifying zip contents, managing zip entries in an archive, and so on.

## Why Another Library?

At the time DotNetZip was created, there were various Zip class libraries available for .NET applications. For example,

- *The J# runtime.* (Now defunct, I imagine)  it is possible to read and write zip files within .NET via the J# runtime. But some people don't like to install the extra J# DLL, which is very large, just for the zip capability. Also , the end-of-life of J# has been announced by Microsoft.

- *SharpZipLib* - a 3rd party LGPL-based (or is it GPL?) library. It works, But some people don't like the license, and some don't like the programming model.

- *Commercial Tools* - There are commercial tools (from ComponentOne, XCeed, etc). But some people don't want to incur the cost.

- The *System.Packaging* namespace added in .NET 3.0. When I looked at it, it was a little complicated.

DotNetZip is an alternative to all of these options. It is open source, Apache
licensed. It is free of cost, though donations are always welcomed. It is small
and simple, but it does what you need. It does not require J#. Unlike the
System.Packaging classes, DotNetZip is designed specifically for .ZIP files and
is easier to use because of that.


## Frequently Asked Questions

### How does this Zip Library work?

DotNetZip is packaged as a single DLL, a single assembly. It is fully managed
code, written in C#, and provides support for reading and writing Zip archive
files and streams. The main type is ZipFile, featuring methods like Add(),
Extract() and Save(). There are string and int indexers for the entries of the
ZipFile. There are properties for things like password protection, unicode and
codepage support, and ZIP64 behavior. And there are progress events for Reading,
Saving, and Extracting.

### What do I need, in order to be able to create and read zip files from within my application using this library?

To use the zip capability in your applications, you need to be using the .NET
Core 9.0 or later. It is packaged on nuget as [`DotNetZip.Original`](https://www.nuget.org/packages/DotNetZip.Original/).

```
dotnet add package DotNetZip.Original --version 2025.1.26
```

You do not need to download the sourcecode of DotNetZip in order to use it. You can simply download the binary.

You can use the Zip library from any application, whether a console application, a Windows-Forms application, a server-based application like an ASP.NET page, a smart-device app, a Windows Service, or something else. You can use C#, VB.NET,  or any other .NET language.


### What do I need to build this library from the source?

You need the .NET Core 9.0 or later.  I haven't tried building in any other environment.

### Is this library _supported_ on <insert your platform here>?

It's not supported at all. I have unit tests in the repo.  That's it.


### Does the library support zero-length zip entries, zipfile comments, zip entry comments, zipping up empty directories, recursive directory traversal, zipping up selected files by filename (with wildcards), and password-protecting entries?
Yes.

### Does the library handle ZIP64?
Yes. The original ZIP specification allowed up to 65535 entries in a zip archive, and archive and entry sizes up to 4.2g. The ZIP64 extensions raise those restrictions, at the expense of compatibility and interoperability. DotNetZip can read or write "standard" zip files or ZIP64 zip files.

### Does the library do AES 128 or 256 encryption?

Yes. DotNetZip's AES encryption is compatible with the WinZip AES-encryption extension to the PKWare ZIP specification. DotNetZip uses the same FIPS-197 compliant data format and approach. If you create an AES-encrypted ZIP file with DotNetZip, you will be able to view it and extract it in WinZip, and vice versa. Also, DotNetZip's AES will work with any other tool that is compatible with WinZip. NB: If you open an AES-encrypted ZIP file in Windows Explorer, whether generated by DotNetZip or some other tool, you will be able to view the list of files, but you won't be able to extract entries.

By The Way I believe this version of the encryption builds on SHA1 hashes, and is... not really as secure as you might like it to be.  It's been 15 years since I implemented this, so ....this is to be expected.

I don't have advice for you on how to more securely encrypt your ZIP archives.


### Can I use the DotNetZip library to read .docx files, .xslx files, or other formats that use ZIP internally?

Yes. They are just zip files. Some people use DotNetZip to produce or edit .xlsx or .docx files. Keep in mind, the Packaging APIs originally introduced in .NET 3.0 are optimized for producing .docx files and .xlsx files.

### Can the library be used to read a zip file that has entries with filenames containing Chinese characters, produced by WinRAR?
Yes. To do this you would specify the "big5" code page (cp 950) when reading the zip. In addition to Chinese, it can handle any code page.

### What about reading and writing zip files with other languages and code pages? Portugese? Hebrew? Arabic? Greek? Cyrillic? Norwegian? Finnish? etc?
Yes. Just specify the appropriate code page when reading or writing the zip archive. 

### Does the library support zipping to a stream? Or unzipping from a stream?
Yes, you can zip up files and Save the zip archive to a stream. As well you can Read a zip archive from an open stream - I use this for embedded resources in apps: I call GetManifestResourceStream(), and then unzip that resource. Reading and writing streams complements the capability of being able to Save to a plain file or read from a plain file. The Save-to-a-stream capability allows you to write a zip archive out to, for example, the ASP.NET Response.Output stream, without creating an intermediate file. Very nice for ASP.NET applications.


### Ok, the library can write a zip archive to a stream, and read a zip from a stream, But... can the library add an entry to a zipfile, reading content from a stream? can the library unzip a single entry into a stream? Can an application read an entry as a stream?
Yes. Yes. Yes. Unlike some other libraries, in most cases DotNetZip handles the streaming; your application does not need to implement a Read/Write data pump. The application needs only to open the streams. Using the stream support, you could, for example, open a zip archive, and then modify the files in the archive, and Save out to a Response.OutputStream in ASP.NET, without ever writing a file to the disk. All the zip file content can be manipulated in memory (using MemoryStream for example). 

### Does this library allow removal of entries from zip files, or updating of entries in zip files?
Yes.

### Can I embed the source for DotNetZip into my own project?
Yes - that's allowed by the license. But you may want to think about just redistributing the binary DLL - it is a much easier option. 

### Can I distribute the binary DLL with my own project?
Yes - that's allowed by the license.

### What's the mainstream approach for using DotNetZip in a 3rd party app?
The mainstream approach is to distribute the binary DLL with your own app.



(TO BE COMPLETED)
