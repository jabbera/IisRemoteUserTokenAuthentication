using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RutaHttpModule
{
    internal class AdInteraction : IAdInteraction
    {
        private readonly ISettings settings;

        [ExcludeFromCodeCoverage]
        internal AdInteraction()
            : this(new SettingsWrapper())
        {
        }

        internal AdInteraction(ISettings settings) => this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

        public (string login, string name, string email, string[] groups) GetUserInformation(string domainUsername)
        {
            if (string.IsNullOrWhiteSpace(domainUsername)) throw new ArgumentNullException(nameof(domainUsername));

            string usernameOnly = RemoveDomain(domainUsername);

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
                groups = user.GetAuthorizationGroups()?.Where(FilterGroup).Select(x => x.Name).ToArray();
            }
                return (login, name, email, groups);
        }

        private bool FilterGroup(Principal principal) => principal.DistinguishedName?.EndsWith(this.settings.AdGroupBaseDn, StringComparison.OrdinalIgnoreCase) ?? false;

        private static string RemoveDomain(string domainUsername)
        {
            string[] parts = domainUsername.Split('\\');
            if (parts.Length != 2)
            {
                return domainUsername;
            }

            return parts[1];
        }
    }
}