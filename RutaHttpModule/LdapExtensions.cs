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
        private const string ADAttribute_DistinguishedName = "distinguishedName";
        private const string LDAPPathPrefix = "LDAP://";
        private const string UserNameSearchFilterFormatString = "(&(samAccountType=805306368)(|(userPrincipalName={0})(samAccountName={0})))";

        internal static IEnumerable<string> GetGroupsFast(this UserPrincipal user, string userContainer, string groupsContainer)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            using (var groupsDirectoryEntry = BindToContainer(groupsContainer))
            using (var userDirectoryEntry = BindToContainer(userContainer))
            {
                return GetGroupNamesForUser(user.SamAccountName, userDirectoryEntry, groupsDirectoryEntry);
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

        private static IEnumerable<string> GetGroupNamesForUser(string userName, DirectoryEntry userDirectoryEntry, DirectoryEntry groupsDirectoryEntry)
        {
            if (userName == null) throw new ArgumentNullException(nameof(userName));
            if (userDirectoryEntry == null) throw new ArgumentNullException(nameof(userDirectoryEntry));

            var user = SearchForUser(userName, userDirectoryEntry, new[] { ADAttribute_DistinguishedName });
            if (user == null) return null;

            var userDistinguishedName = ExtractUserDistinguishedName(userName, user);

            var groupContainer = groupsDirectoryEntry; // search group context

            // ReSharper disable once ExpressionIsAlwaysNull
            return SearchForUsersGroupCommonNames(groupContainer, userDistinguishedName);
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

        /// <summary>
        /// Searches for the specified user in the specified user container,
        /// and returns a <c>SearchResult</c> representing the user that
        /// contains the Active Directory properties specified in <c>adPropertiesToLoad</c>.
        /// </summary>
        private static SearchResult SearchForUser(string userName, DirectoryEntry userContainer, string[] adPropertiesToLoad)
        {
            if (userName == null) throw new ArgumentNullException(nameof(userName));
            if (userContainer == null) throw new ArgumentNullException(nameof(userContainer));

            using (var userSearcher = new DirectorySearcher(userContainer))
            {
                userSearcher.Filter = string.Format(UserNameSearchFilterFormatString, userName);
                userSearcher.PropertiesToLoad.AddRange(adPropertiesToLoad);

                var user = userSearcher.FindOne();

                return user;
            }
        }

        private static string ExtractUserDistinguishedName(string userName, SearchResult user)
        {
            var userDnValueCollection = user.Properties[ADAttribute_DistinguishedName];

            if (userDnValueCollection == null)
            {
                var message = string.Format("Could not find the {1} property in the specified user ({0}).", userName, ADAttribute_DistinguishedName);
                throw new ArgumentException(message);
            }
            var userDistinguishedName = userDnValueCollection[0] as string;
            if (userDistinguishedName == null)
            {
                var message = $"Active Directory is broken.  Retrieved user {userName} with an empty collection of {ADAttribute_DistinguishedName} values.";
                throw new ActiveDirectoryOperationException(message);
            }
            return userDistinguishedName;
        }
    }
}
