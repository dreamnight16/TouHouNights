param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.5.9f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$editorDirectory = Split-Path -Parent $UnityEditor
$uiRuntime = Join-Path $editorDirectory 'Data\Resources\PackageManager\BuiltInPackages\com.unity.ugui\Runtime'
if (-not (Test-Path -LiteralPath $UnityEditor) -or -not (Test-Path -LiteralPath $uiRuntime)) {
    throw 'Unity 6000.5.9f1 and its built-in UGUI runtime sources are required.'
}

# 隔离构建副本；使用同版本编辑器自带源码，避免依赖本机故障的包解析缓存。
$workProject = Join-Path $projectRoot ('Builds\SubmissionWork-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $workProject -Force | Out-Null
foreach ($folder in @('Assets', 'Packages', 'ProjectSettings')) {
    New-Item -ItemType Directory -Path (Join-Path $workProject $folder) -Force | Out-Null
    Get-ChildItem -Force -LiteralPath (Join-Path $projectRoot $folder) |
        Where-Object { $_.Name -notin @('_Recovery', '_Recovery.meta') } |
        Copy-Item -Destination (Join-Path $workProject $folder) -Recurse -Force
}
$vendor = Join-Path $workProject 'Assets\Vendor\UGUI'
New-Item -ItemType Directory -Path $vendor -Force | Out-Null
Copy-Item -LiteralPath $uiRuntime -Destination (Join-Path $vendor 'Runtime') -Recurse
if (Test-Path -LiteralPath ($uiRuntime + '.meta')) {
    Copy-Item -LiteralPath ($uiRuntime + '.meta') -Destination $vendor
}
$logs = Join-Path $projectRoot 'Builds\Logs'
New-Item -ItemType Directory -Path $logs -Force | Out-Null
$logFile = Join-Path $logs 'windows-submission-build.log'
$arguments = '-batchmode -nographics -noUpm -quit -projectPath "' + $workProject +
    '" -executeMethod SubmissionBuild.BuildWindows -logFile "' + $logFile + '"'
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if ($process.ExitCode -ne 0) { throw "Unity build failed. See $logFile" }
$builtPlayer = Join-Path $workProject 'Builds\Windows\TouHouNights.exe'
if (-not (Test-Path -LiteralPath $builtPlayer)) { throw 'Build did not produce TouHouNights.exe.' }
$output = Join-Path $projectRoot 'Builds\Windows'
New-Item -ItemType Directory -Path $output -Force | Out-Null
Get-ChildItem -LiteralPath (Split-Path -Parent $builtPlayer) |
    Copy-Item -Destination $output -Recurse -Force
Write-Output "Windows build ready: $output"
