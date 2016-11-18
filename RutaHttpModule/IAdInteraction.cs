using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutaHttpModule
{
    internal interface IAdInteraction
    {
        (string login, string name, string email, string[] groups) GetUserInformation(string domainUsername);
    }
}
