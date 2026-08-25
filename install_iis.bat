@echo off
title Instalador de IIS y ASP.NET Core Hosting Bundle
echo Solicitando permisos de administrador...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process powershell.exe -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0install_iis.ps1\"'"
