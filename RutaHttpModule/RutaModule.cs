using System.Collections.Concurrent;

namespace RutaHttpModule
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Web;

    /// <summary>
    /// This module changes adds all header information needed for interaction with SonarQube and 
    /// provide a SSO experience. An existing Authorization header will be removed.
    /// </summary>
    /// <remarks>
    /// See also https://www.iis.net/learn/develop/runtime-extensibility/how-to-add-tracing-to-iis-managed-modules
    /// </remarks>
    public sealed class RutaModule : IHttpModule
    {
        /// <summary>
        /// Reference to the implementation needed to query the AD.
        /// </summary>
        private readonly IAdInteraction adInteraction;

        /// <summary>
        /// Reference to the settings needed for constructing the right header.
        /// </summary>
        private readonly ISettings settings;

        /// <summary>
        /// Reference to the tracing object.
        /// </summary>
        private readonly ITraceSource traceSource;

        private readonly ConcurrentDictionary<string, (string login, string name, string email, string[] groups)> cache =
            new ConcurrentDictionary<string, (string login, string name, string email, string[] groups)>();

        /// <summary>
        /// Initializes a new instance of the a <see cref="RutaModule"/> object
        /// </summary>
        [ExcludeFromCodeCoverage]
        public RutaModule()
            : this(new AdInteraction(), new SettingsWrapper(), new RutaTraceSource(nameof(RutaHttpModule)))
        {            
        }

        /// <summary>
        /// Initializes a new instance of the a <see cref="RutaModule"/> object
        /// </summary>
        /// <param name="adInteraction">Reference to the implementation needed to query the AD.</param>
        /// <param name="settings">Reference to the settings needed for constructing the right header.</param>
        /// <param name="traceSource">Reference to the tracing object.</param>
        internal RutaModule(IAdInteraction adInteraction, ISettings settings, ITraceSource traceSource)
        {
            this.adInteraction = adInteraction ?? throw new ArgumentNullException(nameof(adInteraction));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.traceSource = traceSource ?? throw new ArgumentNullException(nameof(traceSource));
        }

        /// <summary>
        /// The name of the module
        /// </summary>
        public string ModuleName => nameof(RutaModule);

        /// <summary>
        /// Initializes a module and prepares it to handle requests (part of the <see cref="IHttpHandler"/> interface).
        /// </summary>
        /// <param name="application">An <see cref="HttpApplication"/> that provides access to the methods, properties, 
        /// and events common to all application objects within an ASP.NET application</param>       
        public void Init(HttpApplication application) => application.AuthorizeRequest += AuthorizeRequest;

        /// <summary>
        /// Disposes of the resources (other than memory) used this module  (part of the <see cref="IHttpHandler"/> interface).
        /// </summary>
        public void Dispose() { }

        /// <summary>
        /// Handler executed when a security module has verified user authorization. During execution of the handler the
        /// httpContext will be changed.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">An object that contains no event data.</param>
        [ExcludeFromCodeCoverage]
        private void AuthorizeRequest(object source, EventArgs e)
        {
            var application = source as HttpApplication;
            HandleAuthorizeRequest(new RutaHttpContext(application.Context));
        }

        /// <summary>
        /// Handle an authorizeRequest using the <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The context to be used when handling an AuthorizeRequest</param>
        internal void HandleAuthorizeRequest(IRutaHttpContext context)
        {
            try
            {
                traceSource.TraceEvent(TraceEventType.Start, 0, "START AuthorizeRequest");
                HandleAuthorizeRequestInternal(context);
            }
            catch (Exception ex)
            {
                traceSource.TraceEvent(TraceEventType.Error, 0, $"ERROR AuthorizeRequest: ExceptionData: '{ex}' ");
                throw;
            }
            finally
            {
                traceSource.TraceEvent(TraceEventType.Stop, 0, "END AuthorizeRequest");
            }
        }

        /// <summary>
        /// Replace the current context with a version that can be processed by SonarQube. That is done only 
        /// if:<br/>
        /// <ul>
        ///   <li>The user is authenticated.</li>
        ///   <li>The context contains a DomainUserName.</li>
        ///   <li>The Ldap information could be extracted.</li>
        ///   <li>The user information can be retrieved from AD.</li>
        /// </ul>
        /// </summary>
        /// <param name="context">The context on which the action should be performed.</param>
        private void HandleAuthorizeRequestInternal(IRutaHttpContext context)
        {
            if (!context.IsAuthenticated)
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "Not authenticated");
                return;
            }

            if (!context.IsWindowsUser)
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "Not a windows user");
                return;
            }

            string userName = context.DomainUserName;
            if (string.IsNullOrWhiteSpace(userName))
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "No username in context");
                return;
            }

            (string loginToSend, string name, string email, string[] groupsToSend) = this.GetUserInformation(userName);
            if (loginToSend == null || name == null || email == null)
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "No data available");
                return;
            }

            if (groupsToSend == null)
            {
                groupsToSend = new string[0];
            }

            traceSource.TraceEvent(TraceEventType.Information, 0, "Set headers");

            context.RemoveRequestHeader("Authorization"); // Remove the authorzation header since we are in charge of authentication
            context.AddRequestHeader(this.settings.LoginHeader, loginToSend);
            context.AddRequestHeader(this.settings.NameHeader, name);
            if (!string.IsNullOrWhiteSpace(email))
            {
                context.AddRequestHeader(this.settings.EmailHeader, email);
            }
            
            if (groupsToSend?.Length > 0)
            {
                context.AddRequestHeader(this.settings.GroupsHeader, string.Join(",", groupsToSend));
            }
        }

        private (string login, string name, string email, string[] groups) GetUserInformation(string userName)
        {
            if (this.cache.TryGetValue(userName, out var cachedValues))
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "Returning cached data.");
                return cachedValues;
            }

            var userInformation = this.adInteraction.GetUserInformation(userName);
            if (string.IsNullOrWhiteSpace(userInformation.login))
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "No user information in context");
                return (null, null, null, null);
            }

            traceSource.TraceEvent(TraceEventType.Information, 0, "Bulding header results and caching");

            string loginToSend = ApplyUserSettings(userInformation.login);
            string[] groupsToSend = userInformation.groups.Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(ApplyGroupSettings)
                .ToArray();

            this.cache.TryAdd(userName, (loginToSend, userInformation.name, userInformation.email, groupsToSend));

            return (loginToSend, userInformation.name, userInformation.email, groupsToSend);
        }
        
        /// <summary>
        /// Returns a copy of the <paramref name="group"/> object adjusted as necessary based on 
        /// the settings (DowncaseGroups and AppendString).
        /// </summary>
        /// <param name="group">A <see cref="String"/> on which the action should be performed.</param>
        /// <returns>The modified version of <paramref name="group"/>.</returns>
        private string ApplyGroupSettings(string group) => AppendIfNeeded(LowercaseIfNeeded(group, this.settings.DowncaseGroups));

        /// <summary>
        /// Returns a copy of the <paramref name="user"/> object adjusted as necessary based on 
        /// the settings (DowncaseUsers and AppendString).
        /// </summary>
        /// <param name="user">A <see cref="String"/> on which the action should be performed.</param>
        /// <returns>The modified version of <paramref name="user"/>.</returns>
        private string ApplyUserSettings(string user) => AppendIfNeeded(LowercaseIfNeeded(user, this.settings.DowncaseUsers));

        /// <summary>
        /// Returns a copy of the <paramref name="source"/> object appended with the contents of the
        /// AppendString settings. The action will not be performed if <paramref name="source"/> or
        /// AppendString are null or containing only whitespace. In all other cases <paramref name="source"/> 
        /// will be returned.
        /// </summary>
        /// <param name="source">A <see cref="String"/> on which the action should be performed.</param>
        /// <returns>The modified version of <paramref name="source"/>.</returns>
        private string AppendIfNeeded(string source) => string.IsNullOrWhiteSpace(this.settings.AppendString) ? source : $"{source}{this.settings.AppendString}";

        /// <summary>
        /// Returns a copy of the <paramref name="source"/> object converted to lowercase 
        /// using the casing rules of the invariant culture if indicated by <paramref name="applyLowercase"/>. 
        /// In all other cases <paramref name="source"/> will be returned.
        /// </summary>
        /// <param name="source">A <see cref="String"/> on which the action should be performed.</param>
        /// <param name="applyLowercase">Should the method return a lowercase version of <paramref name="source"/>?</param>
        /// <returns>The modified version of <paramref name="source"/>.</returns>
        private static string LowercaseIfNeeded(string source, bool applyLowercase) => applyLowercase ? source.ToLowerInvariant() : source;
    }    
}
