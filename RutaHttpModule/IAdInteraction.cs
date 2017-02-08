namespace RutaHttpModule
{
    using System.Collections.Generic;

    internal interface IAdInteraction
    {
        (string login, string name, string email, IEnumerable<string> groups) GetUserInformation(string domainUsername);
    }
}