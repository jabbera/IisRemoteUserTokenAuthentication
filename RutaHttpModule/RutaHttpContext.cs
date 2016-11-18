using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RutaHttpModule
{
    [ExcludeFromCodeCoverage]
    internal class RutaHttpContext : IRutaHttpContext
    {
        private readonly HttpContext httpContext;

        internal RutaHttpContext(HttpContext httpContext) => this.httpContext = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

        public string DomainUserName => this.httpContext.User?.Identity?.Name;
        public bool IsAuthenticated => this.httpContext.User?.Identity?.IsAuthenticated ?? false;

        public void RemoveRequestHeader(string header) => this.httpContext.Request.Headers.Remove(header);
        public void AddRequestHeader(string header, string value) => this.httpContext.Request.Headers.Add(header, value);
    }
}
