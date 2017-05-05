namespace RutaHttpModule
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.DirectoryServices.ActiveDirectory;

    /// This monstrosity exists because <see cref="UserPrincipal.GetAuthorizationGroups"/> is so slow. (20+ seconds)
    internal static class LdapExtensions
    {
        private const string MembershipFilterFormatStringAllGroups = "(&(|(samAccountType=268435456)(samAccountType=268435457)(samAccountType=536870912)(samAccountType=536870913))(member:1.2.840.113556.1.4.1941:={0}))";
        private const string ADAttribute_CommonName = "cn";
        private const string LDAPPathPrefix = "LDAP://";

        internal static IEnumerable<string> GetGroupsFast(this UserPrincipal user, string groupsContainer)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            using (var groupsDirectoryEntry = BindToContainer(groupsContainer))
            {
                return SearchForUsersGroupCommonNames(groupsDirectoryEntry, user.DistinguishedName);
            }
        }

        internal static string RemoveDomain(this string domainUsername)
        {
            string[] parts = domainUsername.Split('\\');
            if (parts.Length != 2)
            {
                return domainUsername;
            }

            return parts[1];
        }

        private static IEnumerable<string> SearchForUsersGroupCommonNames(DirectoryEntry groupContainer, string userDistinguishedName)
        {
            if (userDistinguishedName == null) throw new ArgumentNullException(nameof(userDistinguishedName));

            using (var groupSearcher = new DirectorySearcher(groupContainer))
            {
                groupSearcher.Filter = string.Format(MembershipFilterFormatStringAllGroups, userDistinguishedName);
                groupSearcher.SearchRoot = groupContainer;
                groupSearcher.PropertiesToLoad.AddRange(new[] { ADAttribute_CommonName });
                using (var searchResultCollection = groupSearcher.FindAll())
                {
                    var groupNames = new List<string>(searchResultCollection.Count);
                    foreach (SearchResult searchResult in searchResultCollection)
                    {
                        var groupName = searchResult.Properties[ADAttribute_CommonName][0] as string;
                        groupNames.Add(groupName);
                    }
                    return groupNames;
                }
            }
        }
        
        private static DirectoryEntry BindToContainer(string container)
        {
            string path = null;
            if (!string.IsNullOrWhiteSpace(container))
            {
                path = LDAPPathPrefix + container;
            }

            return new DirectoryEntry(path, null, null,
                           AuthenticationTypes.FastBind
                           | AuthenticationTypes.Sealing
                           | AuthenticationTypes.ReadonlyServer // request closest read-only directory service
                           | AuthenticationTypes.Signing);
        }
    }
}
