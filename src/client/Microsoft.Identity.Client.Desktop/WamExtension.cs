using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Internal.Logger;

namespace Microsoft.Identity.Client.Desktop
{
    /// <summary>
    /// </summary>
    public static class WamExtension
    {
        /// <summary>
        /// Enables Windows broker flows on older platforms, such as .NET framework, where these are not available in the box with Microsoft.Identity.Client
        /// For details about Windows broker, see https://aka.ms/msal-net-wam
        /// </summary>
        public static PublicClientApplicationBuilder WithWindowsBroker(this PublicClientApplicationBuilder builder, bool enableBroker = true)
        {
#if NETSTANDARD
            throw new PlatformNotSupportedException(
                "The Windows Broker is not available on NetStandard " +
                "Please reference Microsoft.Identity.Client and Microsot.Identity.Client.Desktop directly " +
                "from your application");
#else
            if (!builder.Config.ExperimentalFeaturesEnabled)
            {
                throw new MsalClientException(
                    MsalError.ExperimentalFeature,
                    MsalErrorMessage.ExperimentalFeature(nameof(WithWindowsBroker)));
            }

            builder.Config.IsBrokerEnabled = enableBroker;

            builder.Config.BrokerCreatorFunc =
                (uiParent, logger) => new Platforms.Features.WamBroker.WamBroker(uiParent, logger);
            return builder;
#endif
        }

       
    }

    
}
