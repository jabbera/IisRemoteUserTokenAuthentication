using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RutaHttpModule
{
    /// <summary>
    /// The goal of this class is to determine which traffic is from a sonar scanner\lint and allow it to pass through since it's likely 
    /// using token based auth and\or accesssing an unrestricted endpoint.
    /// </summary>
    public sealed class SonarAuthPassthroughModule : IHttpModule
    {
        private static readonly IPrincipal passThruUser = new PassThruUser();

        private readonly ISettings settings;
        private readonly ITraceSource traceSource;

        /// <summary>
        /// Initializes a new instance of the a <see cref="SonarAuthPassthroughModule"/> object
        /// </summary>
        [ExcludeFromCodeCoverage]
        public SonarAuthPassthroughModule()
            : this(new SettingsWrapper(), new RutaTraceSource(nameof(SonarAuthPassthroughModule)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the a <see cref="SonarAuthPassthroughModule"/> object
        /// </summary>
        /// <param name="traceSource">Reference to the tracing object.</param>
        internal SonarAuthPassthroughModule(ISettings settings, ITraceSource traceSource)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.traceSource = traceSource ?? throw new ArgumentNullException(nameof(traceSource));
        }

        /// <summary>
        /// Initializes a module and prepares it to handle requests (part of the <see cref="IHttpHandler"/> interface).
        /// </summary>
        /// <param name="application">An <see cref="HttpApplication"/> that provides access to the methods, properties, 
        /// and events common to all application objects within an ASP.NET application</param>       
        [ExcludeFromCodeCoverage]
        public void Init(HttpApplication application) => application.AuthenticateRequest += AuthenticateRequest;

        [ExcludeFromCodeCoverage]
        public void Dispose() { }

        [ExcludeFromCodeCoverage]
        public string ModuleName => nameof(SonarAuthPassthroughModule);

        /// <summary>
        /// Handler executed when a security module has verified before authentication. During execution of the handler the
        /// httpContext will be changed.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">An object that contains no event data.</param>
        [ExcludeFromCodeCoverage]
        private void AuthenticateRequest(object source, EventArgs e)
        {
            var application = source as HttpApplication;            
            HandleAuthenticateRequest(new SonarAuthPassthroughHttpContext(application.Context));
        }

        /// <summary>
        /// Handle an authenticateRequest using the <paramref name="context"/>.
        /// </summary>
        /// <param name="context">The context to be used when handling an AuthorizeRequest</param>
        internal void HandleAuthenticateRequest(ISonarAuthPassthroughHttpContext context)
        {
            try
            {
                traceSource.TraceEvent(TraceEventType.Start, 0, "START AuthenticateRequest");
                HandleAuthenticateRequestRequestInternal(context);
            }
            catch (Exception ex)
            {
                traceSource.TraceEvent(TraceEventType.Error, 0, $"ERROR AuthenticateRequest: ExceptionData: '{ex}' ");
                throw;
            }
            finally
            {
                traceSource.TraceEvent(TraceEventType.Stop, 0, "END AuthenticateRequest");
            }
        }

        private void HandleAuthenticateRequestRequestInternal(ISonarAuthPassthroughHttpContext context)
        {
            if (context.User != null)
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "Already authenticated");
                return;
            }            

            // This is most efficent.
            if (context.HasTokenHeader)
            {
                traceSource.TraceEvent(TraceEventType.Information, 0, "Found token.");
                AssignPassThruUser(context);
                return;
            }

            // If we have no agent, or the agent does not match any of our pass thrus
            string userAgent = context.UserAgent;
            traceSource.TraceEvent(TraceEventType.Information, 0, $"UserAgent: '{userAgent}'");

            if (string.IsNullOrWhiteSpace(userAgent) || this.settings.PassThruUserAgents.Any(userAgent.StartsWith))
            {
                AssignPassThruUser(context);
                return;
            }            
        }

        private void AssignPassThruUser(ISonarAuthPassthroughHttpContext context)
        {
            traceSource.TraceEvent(TraceEventType.Information, 0, "Assigning token user.");

            context.User = passThruUser;
            context.SkipAuthorization = true;
        }

        /// <summary>
        /// If the caller passes a token, create a fake authentication user so the call can just pass through to sonarqube.
        /// If the <see cref="HttpApplication.Context.User"/> is set when the WindowsAuthentication module is called, it will
        /// just skip.
        /// </summary>
        private sealed class PassThruUser : IPrincipal
        {
            private class PassThruUserIdentity : IIdentity
            {
                public string AuthenticationType => "PassThru";

                public bool IsAuthenticated => true;

                public string Name => "Doesntmatter";
            }

            public IIdentity Identity => new PassThruUserIdentity();

            public bool IsInRole(string role)
            {
                throw new NotImplementedException();
            }
        }
    }

    
}
