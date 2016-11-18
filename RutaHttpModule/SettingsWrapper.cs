using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutaHttpModule
{
    [ExcludeFromCodeCoverage]
    internal class SettingsWrapper : ISettings
    {
        public string AdGroupBaseDn => Properties.Settings.Default.AdGroupBaseDn;
        public string AdUserBaseDn => Properties.Settings.Default.AdUserBaseDn;
        public bool Downcase => Properties.Settings.Default.Downcase;
        public string EmailHeader => Properties.Settings.Default.EmailHeader;
        public string GroupsHeader => Properties.Settings.Default.GroupsHeader;
        public string LoginHeader => Properties.Settings.Default.LoginHeader;
        public string NameHeader => Properties.Settings.Default.NameHeader;
    }
}
