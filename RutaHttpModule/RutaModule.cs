using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;

namespace RutaHttpModule
{
    public sealed class RutaModule : IHttpModule
    {
        private readonly IAdInteraction adInteraction;
        private readonly ISettings settings;

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
        }

        [ExcludeFromCodeCoverage]
        public void Dispose() { }

        [ExcludeFromCodeCoverage]
        private void AuthorizeRequest(object source, EventArgs e)
        {
            HttpApplication application = (HttpApplication)source;
            AuthorizeRequest(new RutaHttpContext(application.Context));
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

            context.AddRequestHeader(this.settings.LoginHeader, userInformation.login);
            context.AddRequestHeader(this.settings.NameHeader, userInformation.name);
            context.AddRequestHeader(this.settings.EmailHeader, userInformation.email);
            context.AddRequestHeader(this.settings.GroupsHeader, string.Join(",", userInformation.groups));
        }
    }
}
