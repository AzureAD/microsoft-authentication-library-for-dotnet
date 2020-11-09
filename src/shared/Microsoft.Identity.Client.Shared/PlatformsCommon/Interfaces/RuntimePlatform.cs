using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Shared.PlatformsCommon.Interfaces
{
    /// <summary>
    /// The target platform under which MSAL is running. NetStandard is not an option, as it isn't a platform on its own.
    /// </summary>
    internal enum RuntimePlatform
    {
        NetCore,
        NetFx,
        UWP,
        Android,
        iOS, 
    }
}
