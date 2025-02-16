

# Modify the version of the package manually, I guess.

# Really, it should read the version from .\Zip\Zip.csproj


.\Tools\dnzzip\bin\Release\net9.0\dnzzip.exe  Release-2025.02.15.zip  -UTnow -zc "Release 2025.02.15" `
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
