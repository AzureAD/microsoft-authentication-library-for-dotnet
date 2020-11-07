// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.ApiConfig.Executors
{
    internal static class ClientExecutorFactory
    {
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

        private static bool IsMatsEnabled(ClientApplicationBase clientApplicationBase)
        {
            return clientApplicationBase.ServiceBundle.Mats != null;
        }
    }
}
