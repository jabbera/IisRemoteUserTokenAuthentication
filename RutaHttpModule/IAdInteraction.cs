using System.Collections.Generic;

namespace RutaHttpModule
{
    internal interface IAdInteraction
    {
        (string login, string name, string email, IEnumerable<string> groups) GetUserInformation(string domainUsername);
    }
}