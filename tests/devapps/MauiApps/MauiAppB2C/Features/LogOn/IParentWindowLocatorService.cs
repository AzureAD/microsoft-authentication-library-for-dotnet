using System;
using System.Collections.Generic;
using System.Text;

namespace UserDetailsClient.Core.Features.LogOn
{
    /// <summary>
    /// Simple platform specific service that is responsible for obtaining current window.
    /// </summary>
    public interface IParentWindowLocatorService
    {
       object GetCurrentParentWindow();
    }
}
