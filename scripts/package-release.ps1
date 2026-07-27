param(
    [Parameter(Mandatory = $false)]
    [string]$Version = "1.41",

    [Parameter(Mandatory = $false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [Parameter(Mandatory = $false)]
    [string]$OutputDirectory = "."
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$buildDirectory = Join-Path $repositoryRoot "bin\$Configuration"
$packageName = "SMUDebugTool_zh-CN_v$Version"
$outputDirectoryPath = [IO.Path]::GetFullPath($OutputDirectory)
$outputZip = Join-Path $outputDirectoryPath "$packageName.zip"
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ("SMUDebugTool-package-" + [Guid]::NewGuid().ToString("N"))
$upstreamZip = Join-Path $temporaryRoot "SMUDebugTool_v1.41.zip"
$upstreamDirectory = Join-Path $temporaryRoot "upstream"
$packageDirectory = Join-Path $temporaryRoot $packageName

$upstreamUrl = "https://github.com/irusanov/SMUDebugTool/releases/download/v1.41/SMUDebugTool_v1.41_cs_test_20260722.zip"
$upstreamSha256 = "b55101d7bcc6670bb07cc06b7e7a90bb8c0563c50efb3ff0166e53e56f839023"

$requiredBuildFiles = @(
    "SMUDebugTool.exe",
    "SMUDebugTool.exe.config",
    "SMUDebugTool.pdb",
    "ZenStates-Core.dll",
    "Microsoft.Win32.TaskScheduler.dll",
    "Newtonsoft.Json.dll"
)

$requiredUpstreamFiles = @(
    "InpOut.LICENSE.txt",
    "inpoutx64.dll",
    "WinIo32.dll",
    "WinIo32.LICENSE.txt",
    "WinIo32.sys",
    "WinRing0.LICENSE.txt",
    "ZenStates-Core.pdb"
)

try
{
    New-Item -ItemType Directory -Path $temporaryRoot, $upstreamDirectory, $packageDirectory -Force | Out-Null
    New-Item -ItemType Directory -Path $outputDirectoryPath -Force | Out-Null

    foreach ($file in $requiredBuildFiles)
    {
        $path = Join-Path $buildDirectory $file
        if (-not (Test-Path -LiteralPath $path -PathType Leaf))
        {
            throw "Required build output is missing: $path"
        }
    }

    Invoke-WebRequest -Uri $upstreamUrl -OutFile $upstreamZip
    $actualSha256 = (Get-FileHash -LiteralPath $upstreamZip -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualSha256 -ne $upstreamSha256)
    {
        throw "Upstream release checksum mismatch. Expected $upstreamSha256, got $actualSha256."
    }

    Expand-Archive -LiteralPath $upstreamZip -DestinationPath $upstreamDirectory

    foreach ($file in $requiredUpstreamFiles)
    {
        $path = Join-Path $upstreamDirectory $file
        if (-not (Test-Path -LiteralPath $path -PathType Leaf))
        {
            throw "Required upstream runtime file is missing: $path"
        }

        Copy-Item -LiteralPath $path -Destination $packageDirectory
    }

    foreach ($file in $requiredBuildFiles)
    {
        Copy-Item -LiteralPath (Join-Path $buildDirectory $file) -Destination $packageDirectory
    }

    Copy-Item -LiteralPath (Join-Path $repositoryRoot "README.md") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "NOTICE.md") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "LICENSE.md") -Destination (Join-Path $packageDirectory "LICENSE")
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ThirdParty\PawnIO.LICENSE.txt") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ReleaseAssets\INSTALL_PAWNIO.txt") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ReleaseAssets\INSTALL_PAWNIO.url") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ReleaseAssets\profiles") -Destination $packageDirectory -Recurse

    if (Test-Path -LiteralPath $outputZip)
    {
        Remove-Item -LiteralPath $outputZip -Force
    }

    Compress-Archive -LiteralPath $packageDirectory -DestinationPath $outputZip -CompressionLevel Optimal

    Write-Output "Created release package: $outputZip"
    Write-Output "SHA-256: $((Get-FileHash -LiteralPath $outputZip -Algorithm SHA256).Hash.ToLowerInvariant())"
}
finally
{
    if (Test-Path -LiteralPath $temporaryRoot)
    {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
