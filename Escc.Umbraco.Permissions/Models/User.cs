using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Escc.Umbraco.Permissions.Models
{
    [ExcludeFromCodeCoverage]
    public class User
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public Uri UserProfileUrl { get; set; }

        public IEnumerable<string> GroupNames { get; set; } = new List<string>();
    }
}