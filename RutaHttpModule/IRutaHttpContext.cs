namespace RutaHttpModule
{
    internal interface IRutaHttpContext
    {
        bool IsWindowsUser { get; }
        bool IsAuthenticated { get; }
        string DomainUserName { get; }
        void RemoveRequestHeader(string header);
        void AddRequestHeader(string header, string value);
    }
}