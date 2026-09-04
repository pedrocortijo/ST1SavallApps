$ErrorActionPreference = 'Stop'
$source = 'F:\Applications\ST1SavallApps\publish-wialon-api'
$target = 'E:\St1Savall'
if (!(Test-Path -LiteralPath $source)) { throw 'No existe la publicación preparada.' }
if (!(Test-Path -LiteralPath $target)) { throw 'No existe la ruta física de IIS.' }

# Las credenciales de Wialon y Mapbox se cifran con ASP.NET Data Protection.
# Este anillo de claves debe sobrevivir a los reciclados del pool y a los despliegues.
$keysPath = Join-Path $target 'App_Data\DataProtection-Keys'
New-Item -ItemType Directory -Path $keysPath -Force | Out-Null
& icacls $keysPath /grant 'IIS AppPool\ST1Savall.API:(OI)(CI)M' /T /C | Out-Null
if ($LASTEXITCODE -ne 0) { throw "No se pudieron asignar permisos de escritura a $keysPath." }
$offline = Join-Path $target 'app_offline.htm'
[IO.File]::WriteAllText($offline, '<html><body>Actualizando servicio...</body></html>')
Start-Sleep -Seconds 2
robocopy $source $target /E /COPY:DAT /DCOPY:T /R:2 /W:2 /XF appsettings.json appsettings.Development.json appsettings.Production.json
$code = $LASTEXITCODE
if ($code -ge 8) { throw "Robocopy falló con código $code." }
Remove-Item -LiteralPath $offline -Force -ErrorAction SilentlyContinue
& 'C:\Windows\System32\inetsrv\appcmd.exe' recycle apppool /apppool.name:'ST1Savall.API'
Write-Output "RobocopyExitCode=$code"
Write-Output 'Despliegue completado y pool reciclado.'