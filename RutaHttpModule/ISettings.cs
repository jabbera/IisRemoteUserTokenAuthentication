namespace RutaHttpModule
{
    internal interface ISettings
    {
        string LoginHeader { get; }
        string NameHeader { get; }
        string EmailHeader { get; }
        string GroupsHeader { get; }
        bool DowncaseUsers { get; }
        bool DowncaseGroups { get; }
        string AppendString { get; }
        string AdUserBaseDn { get; }
        string AdGroupBaseDn { get; }
        string[] PassThruUserAgents { get; }
    }
}