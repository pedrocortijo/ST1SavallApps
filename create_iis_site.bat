@echo off
title Configurar Sitio IIS ST1Savall.API (Puerto 4040)
echo Solicitando permisos de administrador...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell.exe -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0create_iis_site.ps1\"'"
