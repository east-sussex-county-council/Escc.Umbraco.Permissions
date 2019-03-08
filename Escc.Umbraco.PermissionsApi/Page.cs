using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Escc.Umbraco.PermissionsApi
{
    [ExcludeFromCodeCoverage]
    public class Page
    {
        public int NodeId { get; set; }
        public Uri Url { get; set; }
        public string Name { get; set; }

        public IEnumerable<User> UsersWithPermissions = new List<User>();

        public DateTimeOffset LastEditedDate { get; set; }

        public User LastEditedBy { get; set; }
    }
}