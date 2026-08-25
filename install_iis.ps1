<#
.SYNOPSIS
    Script de instalación y configuración de IIS (Internet Information Services) y ASP.NET Core Hosting Bundle (.NET 9).
.DESCRIPTION
    Habilita las características de IIS necesarias para producción/desarrollo de aplicaciones web y APIs ASP.NET Core / Blazor.
#>

param (
    [switch]$InstallDotNetHostingBundle = $true
)

# 1. Comprobar permisos de administrador
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Elevando privilegios a Administrador..." -ForegroundColor Yellow
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  Instalación de IIS y Componentes para ASP.NET Core " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

# 2. Lista de características IIS a habilitar
$features = @(
    "IIS-WebServerRole",
    "IIS-WebServer",
    "IIS-CommonHttpFeatures",
    "IIS-StaticContent",
    "IIS-DefaultDocument",
    "IIS-DirectoryBrowsing",
    "IIS-HttpErrors",
    "IIS-HttpRedirect",
    "IIS-ApplicationDevelopment",
    "IIS-WebSockets",
    "IIS-NetFxExtensibility45",
    "IIS-ASPNET45",
    "IIS-ISAPIExtensions",
    "IIS-ISAPIFilter",
    "IIS-HealthAndDiagnostics",
    "IIS-HttpLogging",
    "IIS-LoggingLibraries",
    "IIS-RequestMonitor",
    "IIS-HttpTracing",
    "IIS-Security",
    "IIS-RequestFiltering",
    "IIS-BasicAuthentication",
    "IIS-WindowsAuthentication",
    "IIS-WebServerManagementTools",
    "IIS-ManagementConsole",
    "IIS-ManagementScriptingTools",
    "IIS-ApplicationInit"
)

Write-Host "`n[1/3] Habilitando características de IIS con DISM..." -ForegroundColor Green
$dismArgs = @("/Online", "/Enable-Feature", "/All", "/NoRestart") + ($features | ForEach-Object { "/FeatureName:$_" })
$proc = Start-Process -FilePath "dism.exe" -ArgumentList $dismArgs -Wait -PassThru -NoNewWindow

if ($proc.ExitCode -eq 0 -or $proc.ExitCode -eq 3010) {
    Write-Host "IIS se ha instalado correctamente." -ForegroundColor Green
} else {
    Write-Host "Advertencia/Error al instalar características de IIS (Código de salida: $($proc.ExitCode))." -ForegroundColor Yellow
}

# 3. Comprobar y asegurar inicio de servicio W3SVC
Write-Host "`n[2/3] Verificando servicio W3SVC (World Wide Web Publishing Service)..." -ForegroundColor Green
try {
    Set-Service -Name W3SVC -StartupType Automatic -ErrorAction SilentlyContinue
    Start-Service -Name W3SVC -ErrorAction SilentlyContinue
    $service = Get-Service -Name W3SVC -ErrorAction SilentlyContinue
    Write-Host "Estado de W3SVC: $($service.Status)" -ForegroundColor Cyan
} catch {
    Write-Host "No se pudo iniciar el servicio W3SVC automáticamente: $_" -ForegroundColor Yellow
}

# 4. Instalar .NET 9 ASP.NET Core Hosting Bundle si no está presente
if ($InstallDotNetHostingBundle) {
    Write-Host "`n[3/3] Comprobando ASP.NET Core Hosting Bundle (.NET 9)..." -ForegroundColor Green
    $ancmDll = "C:\Program Files\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
    
    if (Test-Path $ancmDll) {
        Write-Host "El módulo ASP.NET Core (ANCM v2) ya está instalado." -ForegroundColor Green
    } else {
        Write-Host "Descargando e instalando ASP.NET Core 9.0 Hosting Bundle..." -ForegroundColor Yellow
        $tempDir = [System.IO.Path]::GetTempPath()
        $installerPath = Join-Path $tempDir "dotnet-hosting-9.0.x-win.exe"
        $downloadUrl = "https://aka.ms/dotnet/9.0/dotnet-hosting-win.exe"

        try {
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 -bor [Net.SecurityProtocolType]::Tls13
            Invoke-WebRequest -Uri $downloadUrl -OutFile $installerPath -UseBasicParsing
            Write-Host "Ejecutando instalador del Hosting Bundle..." -ForegroundColor Green
            $installProc = Start-Process -FilePath $installerPath -ArgumentList "/quiet /norestart" -Wait -PassThru
            
            if ($installProc.ExitCode -eq 0 -or $installProc.ExitCode -eq 3010) {
                Write-Host "ASP.NET Core Hosting Bundle instalado con éxito." -ForegroundColor Green
                # Reiniciar IIS para que cargue el nuevo módulo
                Write-Host "Reiniciando IIS..." -ForegroundColor Cyan
                & iisreset
            } else {
                Write-Host "El instalador finalizó con código $($installProc.ExitCode)." -ForegroundColor Yellow
            }
        } catch {
            Write-Host "Error al descargar/instalar el Hosting Bundle: $_" -ForegroundColor Red
            Write-Host "Puedes descargarlo manualmente desde: $downloadUrl" -ForegroundColor Yellow
        }
    }
}

Write-Host "`n=====================================================" -ForegroundColor Cyan
Write-Host "  ¡Proceso completado!" -ForegroundColor Green
Write-Host "  Puedes acceder a: http://localhost" -ForegroundColor Green
Write-Host "  Consola de IIS: Ejecutar 'inetmgr' en Windows" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Cyan

Write-Host "`nPresiona Enter para cerrar esta ventana..." -ForegroundColor Gray
[Console]::ReadLine() | Out-Null
