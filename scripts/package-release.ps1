param(
    [Parameter(Mandatory = $false)]
    [string]$Version = "1.41.3",

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
$pawnIoSourceDirectory = Join-Path $temporaryRoot "pawnio-source"
$dotNetInstallerName = "NDP481-x86-x64-AllOS-ENU.exe"
$dotNetInstaller = Join-Path $temporaryRoot $dotNetInstallerName
$pawnIoInstallerName = "PawnIO_setup.exe"
$pawnIoInstaller = Join-Path $temporaryRoot $pawnIoInstallerName
$pawnIoSourceName = "PawnIO_source_2.2.0.zip"
$pawnIoSource = Join-Path $temporaryRoot $pawnIoSourceName
$pawnPpSourceName = "PawnPP_source_e64e4c3.zip"
$pawnPpSource = Join-Path $temporaryRoot $pawnPpSourceName

$upstreamUrl = "https://github.com/irusanov/SMUDebugTool/releases/download/v1.41/SMUDebugTool_v1.41_cs_test_20260722.zip"
$upstreamSha256 = "b55101d7bcc6670bb07cc06b7e7a90bb8c0563c50efb3ff0166e53e56f839023"
$dotNetInstallerUrl = "https://download.microsoft.com/download/4/b/2/cd00d4ed-ebdd-49ee-8a33-eabc3d1030e3/NDP481-x86-x64-AllOS-ENU.exe"
$dotNetInstallerSha256 = "c0ca2e0c9cd18a24a0a77369a13fae2c2c4e8bc83355dd24e5ddc00f9d791fe3"
$pawnIoInstallerUrl = "https://github.com/namazso/PawnIO.Setup/releases/download/2.2.0/PawnIO_setup.exe"
$pawnIoInstallerSha256 = "1f519a22e47187f70a1379a48ca604981c4fcf694f4e65b734aaa74a9fba3032"
$pawnIoSourceUrl = "https://codeload.github.com/namazso/PawnIO/zip/refs/tags/2.2.0"
$pawnIoSourceSha256 = "93aa5d410b76c71e9004cac406ed19d0550a735410a9abe4d0c9a838b8b98eac"
$pawnPpSourceUrl = "https://codeload.github.com/namazso/PawnPP/zip/e64e4c37b2d8ba0d8ee57205faf8183aee12c438"
$pawnPpSourceSha256 = "20aa0638b95c90d296935310f3f28cc22de1fb24855c5baa6721e2ae95eb6042"

$requiredBuildFiles = @(
    "AntdUI.dll",
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
    New-Item -ItemType Directory -Path $temporaryRoot, $upstreamDirectory, $packageDirectory, $pawnIoSourceDirectory -Force | Out-Null
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

    Invoke-WebRequest -Uri $dotNetInstallerUrl -OutFile $dotNetInstaller
    $actualDotNetSha256 = (Get-FileHash -LiteralPath $dotNetInstaller -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualDotNetSha256 -ne $dotNetInstallerSha256)
    {
        throw ".NET Framework installer checksum mismatch. Expected $dotNetInstallerSha256, got $actualDotNetSha256."
    }

    Invoke-WebRequest -Uri $pawnIoInstallerUrl -OutFile $pawnIoInstaller
    $actualPawnIoSha256 = (Get-FileHash -LiteralPath $pawnIoInstaller -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualPawnIoSha256 -ne $pawnIoInstallerSha256)
    {
        throw "PawnIO installer checksum mismatch. Expected $pawnIoInstallerSha256, got $actualPawnIoSha256."
    }

    Invoke-WebRequest -Uri $pawnIoSourceUrl -OutFile $pawnIoSource
    $actualPawnIoSourceSha256 = (Get-FileHash -LiteralPath $pawnIoSource -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualPawnIoSourceSha256 -ne $pawnIoSourceSha256)
    {
        throw "PawnIO source checksum mismatch. Expected $pawnIoSourceSha256, got $actualPawnIoSourceSha256."
    }

    Invoke-WebRequest -Uri $pawnPpSourceUrl -OutFile $pawnPpSource
    $actualPawnPpSourceSha256 = (Get-FileHash -LiteralPath $pawnPpSource -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualPawnPpSourceSha256 -ne $pawnPpSourceSha256)
    {
        throw "PawnPP source checksum mismatch. Expected $pawnPpSourceSha256, got $actualPawnPpSourceSha256."
    }

    Expand-Archive -LiteralPath $upstreamZip -DestinationPath $upstreamDirectory
    Expand-Archive -LiteralPath $pawnIoSource -DestinationPath $pawnIoSourceDirectory

    $pawnIoLicense = Join-Path $pawnIoSourceDirectory "PawnIO-2.2.0\COPYING"
    if (-not (Test-Path -LiteralPath $pawnIoLicense -PathType Leaf))
    {
        throw "PawnIO GPL-2.0 license is missing from the official source archive."
    }

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
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "CHANGELOG.md") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "NOTICE.md") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "LICENSE.md") -Destination (Join-Path $packageDirectory "LICENSE")
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ThirdParty\AntdUI.LICENSE.txt") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ReleaseAssets\INSTALL_PAWNIO.txt") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ReleaseAssets\INSTALL_PAWNIO.url") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ReleaseAssets\INSTALL_DOTNET.txt") -Destination $packageDirectory
    Copy-Item -LiteralPath (Join-Path $repositoryRoot "ReleaseAssets\RUN_SMUDEBUGTOOL.cmd") -Destination $packageDirectory
    Copy-Item -LiteralPath $dotNetInstaller -Destination (Join-Path $packageDirectory $dotNetInstallerName)
    Copy-Item -LiteralPath $pawnIoInstaller -Destination (Join-Path $packageDirectory $pawnIoInstallerName)
    Copy-Item -LiteralPath $pawnIoLicense -Destination (Join-Path $packageDirectory "PawnIO.LICENSE.txt")
    Copy-Item -LiteralPath $pawnIoSource -Destination (Join-Path $packageDirectory $pawnIoSourceName)
    Copy-Item -LiteralPath $pawnPpSource -Destination (Join-Path $packageDirectory $pawnPpSourceName)
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
