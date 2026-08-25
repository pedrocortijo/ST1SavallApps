<#
.SYNOPSIS
    Script para crear y configurar el sitio web de IIS para ST1Savall.API en el puerto 4040.
.DESCRIPTION
    - Crea el Application Pool con "Sin código administrado" (No Managed Code) para ASP.NET Core 9.
    - Crea el Sitio Web en el puerto 4040 apuntando a e:\St1Savall.
    - Configura permisos NTFS necesarios (IIS_IUSRS, IUSR, IIS AppPool\ST1Savall.API).
    - Habilita la regla del Firewall de Windows para el puerto 4040.
#>

param(
    [string]$SiteName = "ST1Savall.API",
    [string]$AppPoolName = "ST1Savall.API",
    [string]$PhysicalPath = "e:\St1Savall",
    [int]$Port = 4040
)

# 1. Comprobar privilegios de administrador
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "Elevando privilegios a Administrador..." -ForegroundColor Yellow
    Start-Process powershell.exe -Verb RunAs -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    exit
}

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  Creación de Sitio IIS: $SiteName (Puerto $Port)     " -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan

Import-Module WebAdministration -ErrorAction SilentlyContinue

# 2. Verificar existencia de la ruta física
if (-not (Test-Path $PhysicalPath)) {
    Write-Host "Creando directorio físico: $PhysicalPath..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
}

# 3. Configurar permisos de seguridad NTFS
Write-Host "`n[1/5] Configurando permisos NTFS en $PhysicalPath..." -ForegroundColor Green
try {
    # Otorgar permisos a IIS_IUSRS y IUSR usando icacls para máxima fiabilidad
    & icacls "$PhysicalPath" /grant "IIS_IUSRS:(OI)(CI)RX" /t /c /q | Out-Null
    & icacls "$PhysicalPath" /grant "IUSR:(OI)(CI)RX" /t /c /q | Out-Null
    & icacls "$PhysicalPath" /grant "IIS AppPool\${AppPoolName}:(OI)(CI)RX" /t /c /q | Out-Null
    Write-Host "Permisos NTFS configurados correctamente." -ForegroundColor Green
} catch {
    Write-Host "Advertencia al configurar permisos: $_" -ForegroundColor Yellow
}

# 4. Crear o configurar el Application Pool
Write-Host "`n[2/5] Configurando Application Pool: $AppPoolName..." -ForegroundColor Green
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "El AppPool '$AppPoolName' ya existe. Actualizando configuración..." -ForegroundColor Yellow
} else {
    New-WebAppPool -Name $AppPoolName | Out-Null
    Write-Host "AppPool '$AppPoolName' creado." -ForegroundColor Green
}

# ASP.NET Core debe ejecutarse con managedRuntimeVersion = "" (Sin código administrado)
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "enable32BitAppOnWin64" -Value $false
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "startMode" -Value "AlwaysRunning"

# 5. Crear o configurar el Sitio Web
Write-Host "`n[3/5] Configurando Sitio Web: $SiteName..." -ForegroundColor Green
if (Test-Path "IIS:\Sites\$SiteName") {
    Write-Host "El Sitio '$SiteName' ya existe. Actualizando..." -ForegroundColor Yellow
    Stop-WebSite -Name $SiteName -ErrorAction SilentlyContinue
    Remove-WebSite -Name $SiteName
}

New-WebSite -Name $SiteName -Port $Port -PhysicalPath $PhysicalPath -ApplicationPool $AppPoolName | Out-Null
Write-Host "Sitio web '$SiteName' creado en el puerto $Port." -ForegroundColor Green

# 6. Configurar regla de Firewall de Windows
Write-Host "`n[4/5] Configurando regla de Firewall para puerto $Port..." -ForegroundColor Green
$fwRuleName = "ST1Savall API IIS (Puerto $Port)"
$existingRule = Get-NetFirewallRule -DisplayName $fwRuleName -ErrorAction SilentlyContinue
if (-not $existingRule) {
    New-NetFirewallRule -DisplayName $fwRuleName -Direction Inbound -Protocol TCP -LocalPort $Port -Action Allow | Out-Null
    Write-Host "Regla de firewall creada para permitir tráfico TCP entrante en puerto $Port." -ForegroundColor Green
} else {
    Write-Host "La regla de firewall ya existe." -ForegroundColor Green
}

# 7. Iniciar AppPool y Sitio Web
Write-Host "`n[5/5] Iniciando AppPool y Sitio..." -ForegroundColor Green
Start-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
Start-WebSite -Name $SiteName -ErrorAction SilentlyContinue

Write-Host "`n=====================================================" -ForegroundColor Green
Write-Host "  ¡Sitio '$SiteName' configurado con éxito!" -ForegroundColor Green
Write-Host "  - URL: http://localhost:$Port" -ForegroundColor Green
Write-Host "  - Carpeta: $PhysicalPath" -ForegroundColor Green
Write-Host "  - AppPool: $AppPoolName (No Managed Code)" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor Green

Write-Host "`nPresiona Enter para cerrar esta ventana..." -ForegroundColor Gray
[Console]::ReadLine() | Out-Null
