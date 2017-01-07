
$webpi_download_path = Join-Path $env:TEMP "arr.msi"
$url = "http://download.microsoft.com/download/C/F/F/CFF3A0B8-99D4-41A2-AE1A-496C08BEB904/WebPlatformInstaller_amd64_en-US.msi"

wget -TimeoutSec ([int]::MaxValue) -OutFile "$webpi_download_path" "$url" -UseBasicParsing

import-module Dism

Write-Verbose "Installing IIS"
Enable-windowsoptionalfeature -All -Online -FeatureName IIS-HttpRedirect, IIS-ASPNET45, IIS-WebServerManagementTools, IIS-HttpTracing, IIS-WindowsAuthentication, IIS-NetFxExtensibility45, IIS-ApplicationDevelopment

Import-Module WebAdministration

Write-Verbose "Installing Arr"
Start-Process $webpi_download_path '/qn' -PassThru | Wait-Process
cd 'C:/Program Files/Microsoft/Web Platform Installer'; .\WebpiCmd.exe /Install /Products:'UrlRewrite2,ARRv3_0' /AcceptEULA
Set-WebConfiguration system.webServer/proxy -value @{ enabled = "true" }

Get-WebConfiguration `
 -pspath 'MACHINE/WEBROOT/APPHOST' `
 -filter "system.webServer/modules/add" -recurse | `
 where {$_.PSPath -eq 'MACHINE/WEBROOT/APPHOST' -and $_.Type -eq ''} `
 | foreach {         
     $filter = "system.webServer/modules/add[@name='" + $_.Name + "']"     
     Remove-WebConfigurationLock  -filter $filter -verbose
 }

& $env:windir\system32\inetsrv\appcmd unlock config /section:windowsAuthentication
& $env:windir\system32\inetsrv\appcmd unlock config /section:anonymousAuthentication