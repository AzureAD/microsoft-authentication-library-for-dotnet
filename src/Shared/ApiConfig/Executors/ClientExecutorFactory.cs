// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal static class ClientExecutorFactory
    {
        private static bool IsMatsEnabled(ClientApplicationBase clientApplicationBase)
        {
            return clientApplicationBase.ServiceBundle.Mats != null;
        }

        public static IPublicClientApplicationExecutor CreatePublicClientExecutor(
            PublicClientApplication publicClientApplication)
        {
            IPublicClientApplicationExecutor executor = new PublicClientExecutor(
                publicClientApplication.ServiceBundle,
                publicClientApplication);

            if (IsMatsEnabled(publicClientApplication))
            {
                executor = new TelemetryPublicClientExecutor(executor, publicClientApplication.ServiceBundle.Mats);
            }

            return executor;
        }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
        public static IConfidentialClientApplicationExecutor CreateConfidentialClientExecutor(
            ConfidentialClientApplication confidentialClientApplication)
        {
            IConfidentialClientApplicationExecutor executor = new ConfidentialClientExecutor(
                confidentialClientApplication.ServiceBundle,
                confidentialClientApplication);

            if (IsMatsEnabled(confidentialClientApplication))
            {
                executor = new TelemetryConfidentialClientExecutor(executor, confidentialClientApplication.ServiceBundle.Mats);
            }

            return executor;
        }
#endif

        public static IClientApplicationBaseExecutor CreateClientApplicationBaseExecutor(
            ClientApplicationBase clientApplicationBase)
        {
            IClientApplicationBaseExecutor executor = new ClientApplicationBaseExecutor(
                clientApplicationBase.ServiceBundle,
                clientApplicationBase);

            if (IsMatsEnabled(clientApplicationBase))
            {
                executor = new TelemetryClientApplicationBaseExecutor(executor, clientApplicationBase.ServiceBundle.Mats);
            }

            return executor;
        }

    }
}
