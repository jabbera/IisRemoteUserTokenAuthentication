# Iis Remote User Token Authentication aka: Single Sign using HTTP headers.

A Custom Http Handler that implements RUTA for: https://jira.sonarsource.com/browse/SONAR-5430, This will allow single sign on for windows active directory users. Note: This was previously supported by: https://github.com/SonarQubeCommunity/sonar-activedirectory however immediatly after development, that plugin was abandoned. 

[![Build status](https://ci.appveyor.com/api/projects/status/n3cgxias5t3mfybr?svg=true)](https://ci.appveyor.com/project/jabbera/iisremoteusertokenauthentication)

# Administrivia

SonarQube scanners DO NOT support anything other then basic\token based authentication. I've created a module that attempts to detect when the connecting application is a scanner or includes a token. When a scanner is detected the module will then bypass the windows authentication process. Right now the bypass conditions are:
*  If there is an Authorization header with Basic auth
    * this indicates a token is present
*  If the user agent of the request starts with any of the agent strings listed in the web.config setting: PassThruAgents
    *  Initial configuration: sonar.scanner.app, SonarQubeScanner

I've only tested this with the MsBuild scanner so the agent list may need to be expanded. 

Previously the only way to enable single sign on was to have 2 websites, one for windows authentication, and one for token based authentication. The bypass module SHOULD remove the need for that second site. Until there is more exhaustive testing I will still include the multi-site installation instructions and artifacts. My plan is to remove those once the single site method is proven stable. 



# Installation (Single Site)

These are the prefered installation directions. 

`Note: This configuration assumes sonarqube is running on the same server as IIS on port 9000. If this is not correct you will need to edit the reverse proxy rules in the web.config file to match your configuration.`

1) Configure sonarqube for RUTA per: https://jira.sonarsource.com/browse/SONAR-5430 (If default settings are used all you should need to do is add: sonar.web.sso.enable=true to the sonar.properties file and restart sonarqube.)

2) Download the current release and extract to [EXTRACT_FOLDER]

3) Run: ConfigureServer.ps1
`Note: This installs the following windows features: IIS-HttpRedirect, IIS-ASPNET45, IIS-WebServerManagementTools, IIS-HttpTracing, IIS-WindowsAuthentication, IIS-NetFxExtensibility45, IIS-ApplicationDevelopment. It unlocks the IIS module ordering system wide as well as the authentication module configuration. It also installs ARR and UrlRewrite server wide.`

4) Create a website [WWWROOT], ssl required, pointing to a directory [WWWROOT_DIRECTORY] with a test file in it. Make sure you can browse to that file via your browser.

5) Copy: [EXTRACT_FOLDER]\inetpub-user to: [WWWROOT_DIRECTORY]

6) Browse to https://[WWWROOT] You should hopefully be signed in.

7) Test a scanner run with the url https://[WWWROOT]

# Installation (Multi Site)

These directions are only if you run into trouble with the single site method.

The first site is for the browser and supports single sign on. [WWWROOT_USER] You will also need an unauthenticated one for supporting scanners [WWWROOT_SCANNER]. Please setup these websites ahead of time (ssl required) and make sure you can access index.html. DO NOT USE AN SNI based website for WWWROOT_SCANNER. Run it on a different port then 443. There is a bug that makes it unsupported. 

1) Configure sonarqube for RUTA per: https://jira.sonarsource.com/browse/SONAR-5430 (If default settings are used all you should need to do is add: sonar.web.sso.enable=true to the sonar.properties file and restart sonarqube.)

2) Download the current release and extract to [EXTRACT_FOLDER]

3) Run: ConfigureServer.ps1
`Note: This installs the following windows features: IIS-HttpRedirect, IIS-ASPNET45, IIS-WebServerManagementTools, IIS-HttpTracing, IIS-WindowsAuthentication, IIS-NetFxExtensibility45, IIS-ApplicationDevelopment. It unlocks the IIS module ordering system wide as well as the authentication module configuration. It also installs ARR and UrlRewrite server wide.`

4) Copy: [EXTRACT_FOLDER]\inetpub-user to: [WWWROOT_USER]

5) Remove the line: <add name="SonarAuthPassthroughModule" type="RutaHttpModule.SonarAuthPassthroughModule" preCondition="runtimeVersionv4.0" /> from the web.config file.

6) Browse to https://[WWWROOT_USER_URL] You should hopefully be signed in.

`Note: This configuration assumes sonarqube is running on the same server as IIS on port 9000. If this is not correct you will need to edit the reverse proxy rules in the web.config file to match your configuration.`

7) Once you have the SSO working the only thing left is to configure the reverse proxy on [WWWROOT_SCANNER].

8) Copy:[EXTRACT_FOLDER]\inetpub-scanner  to: [WWWROOT_SCANNER]

8) You should now be able to run a scanner configured to point at: https://[WWWROOT_SCANNER_URL] with token based authentication.

# Configuration Options

While this should work fine out of the box there are a few options that can be used all of which are configured by editing the applicationSettings section of the web.config file.

1) All of the the header names are configurable via the following settings that should be self explanitory: LoginHeader, NameHeader, EmailHeader, and GroupsHeader. Be sure the values match what SonarQube is expecting. (The defaults work if nothing is changed.)

2) DowncaseUsers - This will downcase all the usernames reguardless of what is in AD.

3) DowncaseGroups - This will downcase all the group names reguardless of what is in AD. (Useful for people migrating from the AD plugin who used the setting.)

4) AppendString - This will append whatever value of text you want to the end of each login and group. (Useful for people migrating from the AD plugin to append @domain)

5) AdUserBaseDsn - Only search for users under this specified OU

6) AdGroupBaseDsn - For users, only return groups that are in the following OU.

7) PassThruAgents - If you discover new user agent strings that are not bypassing windows authentication add them to this list. (Please open an issue or pull request also.)

Note: For large AD trees setting AdUserBaseDsn and AdGroupBaseDsn can greatly improve performance.

