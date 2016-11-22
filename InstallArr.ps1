param
(
    [string] $webpi_download_path
)

import-module Dism

Write-Verbose "Installing IIS redirect"
Enable-windowsoptionalfeature -All -Online -FeatureName IIS-HttpRedirect, IIS-ASPNET45, IIS-WebServerManagementTools, IIS-HttpTracing, IIS-WindowsAuthentication

Import-Module WebAdministration

 Get-WebConfiguration `
 -pspath 'MACHINE/WEBROOT/APPHOST' `
 -filter "system.webServer/modules/add" -recurse | `
 where {$_.PSPath -eq 'MACHINE/WEBROOT/APPHOST' -and $_.Type -eq ''} `
 | foreach {         
     $filter = "system.webServer/modules/add[@name='" + $_.Name + "']"     
     Remove-WebConfigurationLock  -filter $filter -verbose
 }

Write-Verbose "Installing Arr"
Start-Process $webpi_download_path '/qn' -PassThru | Wait-Process
cd 'C:/Program Files/Microsoft/Web Platform Installer'; .\WebpiCmd.exe /Install /Products:'UrlRewrite2,ARRv3_0' /AcceptEULA
Set-WebConfiguration system.webServer/proxy -value @{ enabled = "true" }