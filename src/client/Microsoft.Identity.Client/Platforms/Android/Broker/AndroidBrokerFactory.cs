// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Android.App;
using Android.Content;
using Microsoft.Identity.Client.ApiConfig.Parameters;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.Internal.Broker;
using Microsoft.Identity.Client.Internal.Requests;
using Microsoft.Identity.Client.OAuth2;
using Microsoft.Identity.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static async Task<IBroker> GetInstalledBrokerAsync(CoreUIParent uIParent, ICoreLogger logger)
        {
            AndroidBrokerHelper brokerHelper = new AndroidBrokerHelper(Application.Context, logger);

            if (brokerHelper.IsBrokerInstalledAndInvokable())
            {
                try
                {
                    var broker = new AndroidContentProviderBroker(uIParent, logger);
                    await broker.InitiateBrokerHandShakeAsync().ConfigureAwait(false);
                    s_installedBroker = BrokerType.ContentProvider;
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

        public static IBroker CreateBroker(CoreUIParent uIParent, ICoreLogger logger)
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
