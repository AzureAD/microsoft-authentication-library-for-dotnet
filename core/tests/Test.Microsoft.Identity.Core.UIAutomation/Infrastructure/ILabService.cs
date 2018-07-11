using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation.infrastructure
{
    public interface ILabService
    {
        IEnumerable<IUser> GetUsers(UserQueryParameters query);
    }
}
