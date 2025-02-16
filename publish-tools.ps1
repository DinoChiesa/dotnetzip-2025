$tools = @(
    "dnzunzip",
    "dnzgzip",
    "dnzzip",
    "dnzbzip2"
)

# Command to execute
$command = "dotnet --% publish Tools/{0}/{0}.csproj " +
 "   -c Release -r win-x64 -o ./dotnetzip-tools " +
 "   /p:PublishSingleFile=true " +
 "   /p:CopyOutputSymbolsToPublishDirectory=false" +
 "   /p:SkipCopyingSymbolsToOutputDirectory=true" +
 "   --self-contained false "

foreach ($tool in $tools) {
    $commandToExecute = $command -f $tool
    Write-Host "Publishing: $tool"

    try {
        Invoke-Expression -Command "$commandToExecute" | Out-Host
    }
    catch {
        Write-Error "Error executing command '$commandToExecute': $_"
    }
}


$version = (Select-XML -path Zip\Zip.csproj  -xpath "/Project/PropertyGroup/Version/text()").node.Value

$outputFilename = "dotnetzip-tools-$version.zip"
write-host "zip: $outputFilename"
#Compress-Archive -Path "dotnetzip-tools" -DestinationPath $outputFilename
.\dotnetzip-tools\dnzzip.exe $outputFilename -D dotnetzip-tools -E "name = *.exe"

$sha256 = Get-FileHash -Algorithm SHA256 -Path $outputFilename
write-host "Hash: $($sha256.hash)" # Stupidest syntax ever

Write-Host "Finished processing."
