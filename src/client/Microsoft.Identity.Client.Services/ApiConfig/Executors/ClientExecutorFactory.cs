// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal static class ClientExecutorFactory
    {
        public static IPublicClientApplicationExecutor CreatePublicClientExecutor(
            PublicClientApplication publicClientApplication)
        {
            IPublicClientApplicationExecutor executor = new PublicClientExecutor(
                publicClientApplication.ServiceBundle,
                publicClientApplication);

            return executor;
        }

#if !SUPPORTS_CONFIDENTIAL_CLIENT
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]  // hide confidential client on mobile
#endif
        public static IConfidentialClientApplicationExecutor CreateConfidentialClientExecutor(
            ConfidentialClientApplication confidentialClientApplication)
        {
            ClientApplicationBase.GuardMobileFrameworks();

            IConfidentialClientApplicationExecutor executor = new ConfidentialClientExecutor(
                confidentialClientApplication.ServiceBundle,
                confidentialClientApplication);

            return executor;
        }

        public static IClientApplicationBaseExecutor CreateClientApplicationBaseExecutor(
            ClientApplicationBase clientApplicationBase)
        {
            IClientApplicationBaseExecutor executor = new ClientApplicationBaseExecutor(
                clientApplicationBase.ServiceBundle,
                clientApplicationBase);

            return executor;
        }

    }
}
