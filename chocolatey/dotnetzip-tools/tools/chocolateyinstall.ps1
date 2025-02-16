$ErrorActionPreference = 'Stop'; # stop on all errors

$version = "2025.02.15"
#$toolsDir  = Get-ToolsLocation
# not sure about this location
$toolsDir  = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)"
$url = "https://github.com/DinoChiesa/dotnetzip-2025/releases/download/v2025.02.15/dotnetzip-tools-20250215.zip"
$hash = '9478CE160EBAFC33677A9FB0E5E16A6B4D5AFF50DCE5ACD9EA313FB2D4C8FD6E'

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  destination   = "$toolsDir"
  url           = "$url"
  checksum      = $hash
  checksumType  = 'SHA256'
}

Install-ChocolateyZipPackage @packageArgs
