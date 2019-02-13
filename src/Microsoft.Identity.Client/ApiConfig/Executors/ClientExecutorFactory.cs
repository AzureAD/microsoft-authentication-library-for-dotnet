// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal static class ClientExecutorFactory
    {
        public static IPublicClientApplicationExecutor CreatePublicClientExecutor(
            PublicClientApplication publicClientApplication)
        {
            var executor = new PublicClientExecutor(publicClientApplication.ServiceBundle, publicClientApplication);

            // TODO: wrap in proxy object to handle mats actions
            //if (publicClientApplication.AppConfig.IsMatsEnabled)
            //{
            //    executor = new MatsPublicClientExecutor(executor, publicClientApplication.Mats);
            //}

            return executor;
        }

#if !ANDROID_BUILDTIME && !iOS_BUILDTIME && !WINDOWS_APP_BUILDTIME && !MAC_BUILDTIME // Hide confidential client on mobile platforms
        public static IConfidentialClientApplicationExecutor CreateConfidentialClientExecutor(
            ConfidentialClientApplication confidentialClientApplication)
        {
            var executor = new ConfidentialClientExecutor(confidentialClientApplication.ServiceBundle, confidentialClientApplication);

            // TODO: wrap in proxy object to handle mats actions
            //if (publicClientApplication.AppConfig.IsMatsEnabled)
            //{
            //    executor = new MatsPublicClientExecutor(executor, publicClientApplication.Mats);
            //}

            return executor;
        }
#endif

        public static IClientApplicationBaseExecutor CreateClientApplicationBaseExecutor(
            ClientApplicationBase clientApplicationBase)
        {
            var executor = new ClientApplicationBaseExecutor(clientApplicationBase.ServiceBundle, clientApplicationBase);

            // TODO: wrap in proxy object to handle mats actions
            //if (publicClientApplication.AppConfig.IsMatsEnabled)
            //{
            //    executor = new MatsPublicClientExecutor(executor, publicClientApplication.Mats);
            //}

            return executor;
        }

    }
}
