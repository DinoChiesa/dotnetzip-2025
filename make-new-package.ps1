# read the version from .\Zip\Zip.csproj

#$version = Get-Date -Format "yyyy.MM.dd"
$version = (Select-XML -path Zip\Zip.csproj  -xpath "/Project/PropertyGroup/Version/text()").node.Value

.\Tools\dnzzip\bin\Release\net9.0\dnzzip.exe  "Release-$version-bin.zip"  -UTnow -zc "Release $version binaries" `
    -d Zip `
    Zip\bin\Release\net9.0 `
    -d Zlib `
    Zlib\bin\Release\net9.0 `
    -d BZip2 `
    BZip2\bin\Release\net9.0 `
    -d Tools\dnzgzip `
    Tools\dnzgzip\bin\Release\net9.0 `
    -d Tools\dnzzip `
    Tools\dnzzip\bin\Release\net9.0 `
    -d Tools\dnzunzip `
    Tools\dnzunzip\bin\Release\net9.0 `
    -d Tools\dnzbzip2 `
    Tools\dnzbzip2\bin\Release\net9.0

.\Tools\dnzzip\bin\Release\net9.0\dnzzip.exe  "Release-$version-src.zip" `
    -d DotNetZip DotNetZip-2025.sln publish-tools.ps1 make-new-package.ps1 `
    -d DotNetZip/Zip -D Zip  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/Zip.Tests -D Zip.Tests  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/Zlib -D Zlib  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/Zlib.Tests -D Zlib.Tests  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/BZip2 -D BZip2  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/BZip2.Tests -D BZip2.Tests  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/CommonSrc -D CommonSrc  -E "name = *.cs" `
    -d DotNetZip/CommonTestSrc -D CommonTestSrc  -E "name = *.cs" `
    -d DotNetZip/Tools/dnzzip -D Tools/dnzzip  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/Tools/dnzgzip -D Tools/dnzgzip  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/Tools/dnzbzip2 -D Tools/dnzbzip2  -E "(name = *.cs) OR (name = *.csproj)" `
    -d DotNetZip/Tools/dnzunzip -D Tools/dnzunzip  -E "(name = *.cs) OR (name = *.csproj)"
