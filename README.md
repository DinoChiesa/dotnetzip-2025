# DotNetZip - migrated repo

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



(TO BE COMPLETED)
