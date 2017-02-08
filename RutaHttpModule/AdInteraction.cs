namespace RutaHttpModule
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;

    internal class AdInteraction : IAdInteraction
    {        
        private readonly ISettings settings;

        [ExcludeFromCodeCoverage]
        internal AdInteraction()
            : this(new SettingsWrapper())
        {
        }

        internal AdInteraction(ISettings settings) => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public (string login, string name, string email, IEnumerable<string> groups) GetUserInformation(string domainUsername)
        {
            if (string.IsNullOrWhiteSpace(domainUsername)) throw new ArgumentNullException(nameof(domainUsername));

            string usernameOnly = domainUsername.RemoveDomain();

            string login = null, name = null, email = null;
            string[] groups = null;

            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            using (UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, usernameOnly))
            {
                if (user?.DistinguishedName.EndsWith(this.settings.AdUserBaseDn, StringComparison.OrdinalIgnoreCase) != true)
                {
                    return (login, name, email, groups);
                }

                login = usernameOnly;
                name = user.Name;
                email = user.EmailAddress;
                groups = user.GetGroupsFast(this.settings.AdUserBaseDn, this.settings.AdGroupBaseDn).ToArray();
            }

            return (login, name, email, groups);
        }
    }
}