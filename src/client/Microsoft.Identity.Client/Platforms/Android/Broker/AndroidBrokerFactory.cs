// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Android.App;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.UI;
using System;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Platforms.Android.Broker
{
    internal class AndroidBrokerFactory
    {
        private static BrokerType s_installedBroker;

        private enum BrokerType 
        {
            NoneOrUnknown,
            AccountManager,
            ContentProvider
        }

        private static async Task<IBroker> GetInstalledBrokerAsync(CoreUIParent uIParent, IMsalLogger logger)
        {
            AndroidBrokerHelper brokerHelper = new AndroidBrokerHelper(Application.Context, logger);

            if (brokerHelper.IsBrokerInstalledAndInvokable(AuthorityType.Aad)) //authorityType is actually not used by the brokerHelper.IsBrokerInstalledAndInvokable
            {
                try
                {
                    var broker = new AndroidContentProviderBroker(uIParent, logger);
                    await broker.InitiateBrokerHandShakeAsync().ConfigureAwait(false);
                    s_installedBroker = BrokerType.ContentProvider;
                    logger.Info("[Android broker] Content provider broker is available and will be used.");
                    return broker;
                }
                catch (Exception exContentProvider)
                {
                    logger.Error("[Android broker] Unable to communicate with the broker via Content Provider. Attempting to fall back to account manager communication.");
                    logger.Error(exContentProvider.Message);

                    try
                    {
                        var broker = new AndroidAccountManagerBroker(uIParent, logger);
                        await broker.InitiateBrokerHandshakeAsync().ConfigureAwait(false);
                        s_installedBroker = BrokerType.AccountManager;
                        logger.Info("[Android broker] Account manager broker is available and will be used.");
                        return broker;
                    }
                    catch (Exception exAccountManager)
                    {
                        logger.Error("[Android broker] Unable to communicate with the broker via the Account manager.");
                        logger.Error(exAccountManager.Message);
                    }
                }
            }

            // Return a default broker in case no broker is installed to handle install URL
            return new AndroidContentProviderBroker(uIParent, logger);
        }

        public static IBroker CreateBroker(CoreUIParent uIParent, IMsalLogger logger)
        {
            if (s_installedBroker == BrokerType.NoneOrUnknown)
            {
                return Task.Run(async () => await GetInstalledBrokerAsync(uIParent, logger).ConfigureAwait(false)).GetAwaiter().GetResult();
            }
            
            if (s_installedBroker == BrokerType.AccountManager)
            {
                return new AndroidAccountManagerBroker(uIParent, logger);
            }
            else
            {
                return new AndroidContentProviderBroker(uIParent, logger);
            }
        }
    }
}
