param([switch]$Installer)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
 dotnet publish src/Playdeck/Playdeck.csproj -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o Portable
 if ($LASTEXITCODE -ne 0) { throw 'UI publish failed' }
 dotnet publish src/Playdeck.Tracker/Playdeck.Tracker.csproj -c Release -r win-x64 --self-contained true -p:PublishReadyToRun=true -o work/tracker
 if ($LASTEXITCODE -ne 0) { throw 'Tracker publish failed' }
 # Copy only tracker-specific files. Core's WindowsBase facade must never overwrite WPF's WindowsBase assembly.
 Copy-Item work/tracker/Playdeck.Tracker* Portable -Force
 dotnet build src/Playdeck.Tests/Playdeck.Tests.csproj -c Release
 if ($LASTEXITCODE -ne 0) { throw 'Test build failed' }
 & src/Playdeck.Tests/bin/Release/net10.0-windows/Playdeck.Tests.exe Portable/Playdeck.Tracker.exe
 if ($LASTEXITCODE -ne 0) { throw 'Regression tests failed' }
 New-Item -ItemType Directory -Force Portable/LICENSES | Out-Null
 Copy-Item src/Playdeck/app.ico Portable/Playdeck.ico -Force
 Copy-Item README.md Portable/README.md -Force
 Copy-Item LICENSE,THIRD_PARTY_NOTICES.md,CHANGELOG.md Portable -Force
 New-Item -ItemType Directory -Force Portable/docs | Out-Null
 Copy-Item docs/* Portable/docs -Recurse -Force
 Copy-Item src/Playdeck/Assets/Fonts/OFL.txt Portable/LICENSES/LilitaOne-OFL.txt -Force
 Copy-Item src/Playdeck/Assets/Fonts/Barlow-OFL.txt Portable/LICENSES/Barlow-OFL.txt -Force
 if ($Installer) {
  $compiler = Join-Path $env:LOCALAPPDATA 'Programs/Inno Setup 6/ISCC.exe'
  if (!(Test-Path $compiler)) { $compiler = (Get-Command ISCC.exe -ErrorAction Stop).Source }
  & $compiler Installer.iss
  if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
 }
} finally { Pop-Location }

