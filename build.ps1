param([switch]$Test, [string]$TestOutput, [string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8 with the C# compiler is required on Windows.' }
$buildDir = if ($OutputDirectory) { $OutputDirectory } else { Join-Path $PSScriptRoot 'dist' }
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
$references = @('System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Xaml.dll', 'WPF\WindowsFormsIntegration.dll', 'System.Web.Extensions.dll', 'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll', 'WPF\PresentationFramework.dll')
$compilerArgs = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/utf8output', ('/out:' + (Join-Path $buildDir 'Veyra.exe')), ('/win32icon:' + (Join-Path $PSScriptRoot 'src\Veyra.ico')), ('/win32manifest:' + (Join-Path $PSScriptRoot 'src\app.manifest')), ('/resource:' + (Join-Path $PSScriptRoot 'src\MainWindow.xaml') + ',Veyra.MainWindow.xaml'))
foreach ($reference in $references) { $compilerArgs += '/reference:' + (Join-Path $framework $reference) }
foreach ($art in (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'assets\wallpapers') -Filter '*.png')) { $compilerArgs += '/resource:' + $art.FullName + ',Veyra.Art.' + $art.Name }
$webview = Join-Path $PSScriptRoot 'vendor\webview2'
foreach ($name in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','Microsoft.Web.WebView2.Wpf.dll')) { $compilerArgs += '/reference:' + (Join-Path $webview $name) }
foreach ($asset in (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'src\renderer') -File)) { $compilerArgs += '/resource:' + $asset.FullName + ',Veyra.Renderer.' + $asset.Name }
foreach ($art in (Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'assets\previews') -Filter '*.png')) { $compilerArgs += '/resource:' + $art.FullName + ',Veyra.Preview.' + $art.Name }
& $compiler @compilerArgs @sources
if ($LASTEXITCODE -ne 0) { throw 'Veyra compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src\Veyra.exe.config') -Destination $buildDir -Force
foreach ($name in @('Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','Microsoft.Web.WebView2.Wpf.dll','WebView2Loader.dll')) { Copy-Item -LiteralPath (Join-Path $webview $name) -Destination $buildDir -Force }
Write-Output ('Built ' + (Join-Path $buildDir 'Veyra.exe'))
if ($Test) {
    $testRoot = if ($TestOutput) { $TestOutput } else { Join-Path $env:TEMP ('VeyraTests-' + [Guid]::NewGuid().ToString('N')) }
    $process = Start-Process -FilePath (Join-Path $buildDir 'Veyra.exe') -ArgumentList @('--self-test', ('"' + $testRoot + '"')) -WindowStyle Hidden -Wait -PassThru
    Get-Content -LiteralPath (Join-Path $testRoot 'test-results.txt')
    if ($process.ExitCode -ne 0) { throw 'Veyra checks failed.' }
}
