using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation.infrastructure
{
    public interface IUser
    {
        Guid ObjectId { get; }
        UserType UserType { get; }
        string Upn { get; }
        string CredentialUrl { get; }
        IUser HomeUser { get; }
        bool IsExternal { get; }
        bool IsMfa { get; }
        bool IsMam { get; }
        ISet<string> Licenses { get; }
        bool IsFederated { get; }
        FederationProvider FederationProvider { get; }
        string CurrentTenantId { get; }
        string HomeTenantId { get; }
    }
}
