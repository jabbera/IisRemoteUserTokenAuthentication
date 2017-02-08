namespace RutaHttpModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Principal;
    using System.Web;

    [ExcludeFromCodeCoverage]
    internal class SonarAuthPassthroughHttpContext : ISonarAuthPassthroughHttpContext
    {
        private const string AUTHORIZATION_HEADER_NAME = "Authorization";
        private const string BASIC_PREAMBLE = "Basic ";
        private const string USER_AGENT_HEADER_NAME = "User-Agent";

        private static readonly string[] PASS_THRU_AGENT_NAMES = {  };

        private readonly HttpContext httpContext;

        internal SonarAuthPassthroughHttpContext(HttpContext httpContext) => this.httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

        public IPrincipal User
        {
            get { return this.httpContext.User; }
            set { this.httpContext.User = value; }
        }

        public bool HasTokenHeader => httpContext.Request?.Headers[AUTHORIZATION_HEADER_NAME]?.StartsWith(BASIC_PREAMBLE) ?? false;

        public string UserAgent => httpContext.Request?.Headers["User-Agent"];
        public bool SkipAuthorization
        {
            get { return httpContext.SkipAuthorization; }
            set { httpContext.SkipAuthorization = value; }
        }
    }
}
