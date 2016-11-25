# IisRemoteUserTokenAuthentication
A Custom Http Handler that implements RUTA for: https://jira.sonarsource.com/browse/SONAR-5430, This will allow single sign on for windows active directory users. Note: This was previously supported by: https://github.com/SonarQubeCommunity/sonar-activedirectory however immediatly after development, that plugin was abandoned. 

# Installation

SonarQube scanners DO NOT support anything other then basic\token based authentication. Because of that you will need to setup 2 websites. The first is for the browser and supports single sign on. [WWWROOT_BROWSER] You will also need an unauthenticated one for supporting scanners [WWWROOT_SCANNER]. Please setup these websites ahead of time (ssl required) and make sure you can access index.html. DO NOT USE AN SNI based website for WWWROOT_SCANNER. Run it on a different port then 443. There is a bug that makes it unsupported. 

Configure sonarqube for RUTA per: https://jira.sonarsource.com/browse/SONAR-5430 (If default settins are used all you should need to do is add: sonar.sso.enable=true to the sonar.properties file and restart sonarqube.)

1) Download the current release and extract to [EXTRACT_FOLDER]
2) Run: ConfigureServer.ps1
`Note: This installs the following windows features: IIS-HttpRedirect, IIS-ASPNET45, IIS-WebServerManagementTools, IIS-HttpTracing, IIS-WindowsAuthentication, IIS-NetFxExtensibility45, IIS-ApplicationDevelopment. It also unlocks the IIS module ordering system wide as well as the authentication module configuration.`
3) Copy: [EXTRACT_FOLDER]\web-user.config to: [WWWROOT_BROWSER]\web.config
Copy: [EXTRACT_FOLDER]\*.dll to: [WWWROOT_BROWSER]\web.config
4) Browse to https://[WWWROOT_BROWSER_URL] You should hopefully be signed in.
`Note: This configuration assumes sonarqube is running on the same server as IIS on port 9000. If this is not correct you will need to edit the reverse proxy rules in the web.config file to match your configuration.`
5) Once you have the SSO working the only thing left is to configure the reverse proxy on [WWWROOT_SCANNER].
6) Copy:[EXTRACT_FOLDER]\web-scanner.config  to: [WWWROOT_SCANNER]\web.config
7) You should now be able to run a scanner configured to point at: https://[WWWROOT_SCANNER_URL] with token based authentication.