# this works around https://jira.sonarsource.com/browse/SONARMSBRU-288 Remove this file once it's fixed


New-Item "$env:localAppData\Microsoft\MSBuild\15.0\Microsoft.Common.targets\ImportBefore" -type directory -Force

Copy-Item ".\SonarQubeScanner\Targets\SonarQube.Integration.ImportBefore.targets" "$env:localAppData\Microsoft\MSBuild\15.0\Microsoft.Common.targets\ImportBefore\SonarQube.Integration.ImportBefore.targets" -Force -Recurse