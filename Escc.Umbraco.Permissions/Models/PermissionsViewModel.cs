using Escc.EastSussexGovUK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Escc.Umbraco.Permissions.Models
{
    public class PermissionsViewModel : BaseViewModel
    {
        public PermissionsViewModel(IViewModelDefaultValuesProvider defaultValues) : base(defaultValues) { }

        public Page Page { get; set; }
    }
}
