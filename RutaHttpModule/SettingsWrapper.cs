using System.Diagnostics.CodeAnalysis;

namespace RutaHttpModule
{
    [ExcludeFromCodeCoverage]
    internal class SettingsWrapper : ISettings
    {
        public string AdGroupBaseDn => Properties.Settings.Default.AdGroupBaseDn;
        public string AdUserBaseDn => Properties.Settings.Default.AdUserBaseDn;
        public bool DowncaseUsers => Properties.Settings.Default.DowncaseUsers;
        public bool DowncaseGroups => Properties.Settings.Default.DowncaseGroups;
        public string EmailHeader => Properties.Settings.Default.EmailHeader;
        public string GroupsHeader => Properties.Settings.Default.GroupsHeader;
        public string LoginHeader => Properties.Settings.Default.LoginHeader;
        public string NameHeader => Properties.Settings.Default.NameHeader;
        public string AppendString => Properties.Settings.Default.AppendString;
    }
}
