namespace RutaHttpModule
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Principal;
    using System.Web;

    [ExcludeFromCodeCoverage]
    internal class RutaHttpContext : IRutaHttpContext
    {
        private readonly HttpContext httpContext;

        internal RutaHttpContext(HttpContext httpContext) => this.httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

        public bool IsWindowsUser => this.httpContext.User is WindowsPrincipal;
        public string DomainUserName => this.httpContext.User?.Identity?.Name;
        public bool IsAuthenticated => this.httpContext.User?.Identity?.IsAuthenticated ?? false;

        public void RemoveRequestHeader(string header) => this.httpContext.Request.Headers.Remove(header);
        public void AddRequestHeader(string header, string value) => this.httpContext.Request.Headers.Add(header, value);
    }
}
