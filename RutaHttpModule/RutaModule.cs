using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace RutaHttpModule
{
    public sealed class RutaModule : IHttpModule
    {
        private readonly IAdInteraction adInteraction;
        private readonly ISettings settings;
        private TraceSource traceSource;


        [ExcludeFromCodeCoverage]
        public RutaModule()
            : this(new AdInteraction(), new SettingsWrapper())
        {                
        }

        internal RutaModule(IAdInteraction adInteraction, ISettings settings)
        {
            this.adInteraction = adInteraction ?? throw new ArgumentNullException(nameof(adInteraction));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        [ExcludeFromCodeCoverage]
        public string ModuleName => "RutaModule";

        // In the Init function, register for HttpApplication 
        // events by adding your handlers.
        [ExcludeFromCodeCoverage]
        public void Init(HttpApplication application)
        {
            application.AuthorizeRequest += AuthorizeRequest;
            this.traceSource = new TraceSource(nameof(RutaHttpModule)); // Do I need to do this here? https://www.iis.net/learn/develop/runtime-extensibility/how-to-add-tracing-to-iis-managed-modules
        }

        [ExcludeFromCodeCoverage]
        public void Dispose() { }

        [ExcludeFromCodeCoverage]
        private void AuthorizeRequest(object source, EventArgs e)
        {
            try
            {
                traceSource.TraceEvent(TraceEventType.Start, 0, $"[{nameof(RutaModule)} MODULE] START AuthorizeRequest");

                HttpApplication application = (HttpApplication)source;
                AuthorizeRequest(new RutaHttpContext(application.Context));
            }
            catch(Exception ex)
            {
                traceSource.TraceEvent(TraceEventType.Error, 0, $"[{nameof(RutaModule)} ERROR AuthorizeRequest: ExceptionData: '{ex}' ");
                throw;
            }
            finally
            {
                traceSource.TraceEvent(TraceEventType.Stop, 0, $"[{nameof(RutaModule)} END AuthorizeRequest");
            }
        }

        // Internal for testing purposes
        internal void AuthorizeRequest(IRutaHttpContext context)
        {
            if (!context.IsAuthenticated)
            {
                return;
            }

            string userName = context.DomainUserName;

            if (string.IsNullOrWhiteSpace(userName))
            {
                return;
            }

            context.RemoveRequestHeader("Authorization"); // Remove the authorzation header since we are in charge of authentication
            var userInformation = this.adInteraction.GetUserInformation(userName);

            if (string.IsNullOrWhiteSpace(userInformation.login))
            {
                return;
            }

            string loginToSend = AppendIfNedded(DowncaseUserIfNeeded(userInformation.login));
            string[] groupsToSend = userInformation.groups.Where(x => x != null).Select(DowncaseGroupIfNeeded).Select(AppendIfNedded).ToArray();

            context.AddRequestHeader(this.settings.LoginHeader, loginToSend);
            context.AddRequestHeader(this.settings.NameHeader, userInformation.name);
            context.AddRequestHeader(this.settings.EmailHeader, userInformation.email);
            context.AddRequestHeader(this.settings.GroupsHeader, string.Join(",", groupsToSend));
        }

        private string DowncaseGroupIfNeeded(string s)
        {
            if (!this.settings.DowncaseGroups)
            {
                return s;
            }

            return s.ToLower();
        }

        private string DowncaseUserIfNeeded(string s)
        {
            if (!this.settings.DowncaseUsers)
            {
                return s;
            }

            return s.ToLower();
        }

        private string AppendIfNedded(string s)
        {
            if (string.IsNullOrWhiteSpace(this.settings.AppendString) || string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            return $"{s}{this.settings.AppendString}";
        }
    }    
}
