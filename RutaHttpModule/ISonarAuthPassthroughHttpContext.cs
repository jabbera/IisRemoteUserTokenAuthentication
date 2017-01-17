namespace RutaHttpModule
{
    using System.Security.Principal;

    internal interface ISonarAuthPassthroughHttpContext
    {
        IPrincipal User { get; set; }
        bool HasTokenHeader { get; }
        string UserAgent { get; }
        bool SkipAuthorization { get; set; }
    }
}