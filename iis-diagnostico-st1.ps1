$ErrorActionPreference = 'Stop'
& 'C:\Windows\System32\inetsrv\appcmd.exe' list site /config /xml | Out-File -LiteralPath 'F:\Applications\ST1SavallApps\iis-diagnostico-st1.xml' -Encoding utf8
