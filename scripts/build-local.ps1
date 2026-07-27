param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $repoRoot "ZenStatesDebugTool.csproj"
$outputDirectory = Join-Path $repoRoot ("bin\" + $Configuration)
$intermediateDirectory = Join-Path $repoRoot ("obj\Local" + $Configuration)
$referenceRoot = Join-Path $repoRoot "packages\Microsoft.NETFramework.ReferenceAssemblies.net45.1.0.3\build\.NETFramework\v4.5"

if (!(Test-Path $referenceRoot)) {
    throw "Missing .NET Framework 4.5 reference assemblies. Run NuGet restore first."
}

$sdkLine = (& dotnet --list-sdks | Select-Object -Last 1)
if (!$sdkLine -or $sdkLine -notmatch "^([^\s]+)\s+\[(.+)\]$") {
    throw "Unable to locate the installed .NET SDK."
}

$sdkVersion = $Matches[1]
$sdkRoot = $Matches[2]
$compiler = Join-Path $sdkRoot ($sdkVersion + "\Roslyn\bincore\csc.dll")

if (!(Test-Path $compiler)) {
    throw "Unable to locate the Roslyn C# compiler."
}

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $intermediateDirectory | Out-Null

Add-Type -AssemblyName System.Windows.Forms

[xml]$project = Get-Content -LiteralPath $projectFile
$namespace = New-Object System.Xml.XmlNamespaceManager($project.NameTable)
$namespace.AddNamespace("msb", "http://schemas.microsoft.com/developer/msbuild/2003")

$resources = @()
foreach ($node in $project.SelectNodes("//msb:EmbeddedResource", $namespace)) {
    $relativePath = [string]$node.Include
    $resxPath = Join-Path $repoRoot $relativePath
    $logicalName = "ZenStatesDebugTool." +
        (($relativePath -replace "\\", ".") -replace "\.resx$", ".resources")
    $resourcePath = Join-Path $intermediateDirectory (
        ($relativePath -replace "[\\/:]", "_") -replace "\.resx$", ".resources")

    $reader = New-Object System.Resources.ResXResourceReader($resxPath)
    $reader.BasePath = Split-Path -Parent $resxPath
    $writer = New-Object System.Resources.ResourceWriter($resourcePath)
    try {
        foreach ($entry in $reader) {
            $writer.AddResource($entry.Key, $entry.Value)
        }
    }
    finally {
        $reader.Close()
        $writer.Close()
    }
    $resources += ("/resource:" + $resourcePath + "," + $logicalName)
}

$sources = @()
foreach ($node in $project.SelectNodes("//msb:Compile", $namespace)) {
    $sources += Join-Path $repoRoot ([string]$node.Include)
}

$frameworkReferences = @(
    "mscorlib.dll",
    "Microsoft.CSharp.dll",
    "Microsoft.VisualBasic.dll",
    "System.dll",
    "System.Core.dll",
    "System.Data.dll",
    "System.Data.DataSetExtensions.dll",
    "System.Deployment.dll",
    "System.Drawing.dll",
    "System.Management.dll",
    "System.ServiceProcess.dll",
    "System.Windows.Forms.dll",
    "System.Xml.dll",
    "System.Xml.Linq.dll"
) | ForEach-Object { Join-Path $referenceRoot $_ }

$packageReferences = @(
    (Join-Path $repoRoot "packages\AntdUI.2.4.3\lib\net40\AntdUI.dll"),
    (Join-Path $repoRoot "packages\TaskScheduler.2.10.1\lib\net40\Microsoft.Win32.TaskScheduler.dll"),
    (Join-Path $repoRoot "packages\Newtonsoft.Json.13.0.3\lib\net45\Newtonsoft.Json.dll"),
    (Join-Path $repoRoot "Prebuilt\ZenStates-Core.dll")
)

$outputExe = Join-Path $outputDirectory "SMUDebugTool.exe"
$arguments = @(
    $compiler,
    "/nologo",
    "/noconfig",
    "/nostdlib+",
    "/target:winexe",
    "/platform:anycpu",
    "/langversion:latest",
    "/deterministic+",
    ("/out:" + $outputExe),
    ("/win32icon:" + (Join-Path $repoRoot "Resources\ZenStatesDebug.ico")),
    ("/win32manifest:" + (Join-Path $repoRoot "app.manifest"))
)

if ($Configuration -eq "Release") {
    $arguments += @("/optimize+", "/debug:pdbonly")
}
else {
    $arguments += @("/optimize-", "/debug:full", "/define:DEBUG;TRACE")
}

$arguments += ($frameworkReferences + $packageReferences |
    ForEach-Object { "/reference:" + $_ })
$arguments += $resources
$arguments += $sources

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed."
}

Copy-Item $packageReferences -Destination $outputDirectory -Force
Copy-Item (Join-Path $repoRoot "app.config") ($outputExe + ".config") -Force

Write-Host ("Built " + $outputExe)
