using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutaHttpModule
{
    internal interface ISettings
    {
        string LoginHeader { get; }
        string NameHeader { get; }
        string EmailHeader { get; }
        string GroupsHeader { get; }
        bool Downcase { get; }
        string AdUserBaseDn { get; }
        string AdGroupBaseDn { get; }
    }
}
