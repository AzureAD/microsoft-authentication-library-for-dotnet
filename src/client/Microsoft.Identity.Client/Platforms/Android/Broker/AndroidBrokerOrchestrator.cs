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
    internal class AndroidBrokerOrchestrator
    {
        private readonly CoreUIParent _uIParent;
        private readonly ICoreLogger _logger;
        private readonly AndroidBrokerHelper _brokerHelper;
        private static bool s_contentProviderIsAvailable;

        public AndroidBrokerOrchestrator(CoreUIParent uiParent, ICoreLogger logger)
        {
            _uIParent = uiParent;
            _logger = logger;
            _brokerHelper = new AndroidBrokerHelper(Application.Context, _logger);
        }

        private async Task<IBroker> GetInstalledBrokerAsync()
        {
            if (_brokerHelper.IsBrokerInstalledAndInvokable())
            {
                try
                {
                    var broker = new AndroidContentProviderBroker(_uIParent, _logger);

                    if (!s_contentProviderIsAvailable)
                    {
                        broker.InitiateBrokerHandShakeAsync();
                        s_contentProviderIsAvailable = true;
                    }
                    return broker;
                }
                catch (Exception exContentProvider)
                {
                    _logger.Error("Unable to communicate with the broker via Content Provider. Attempting to fall back to account manager communication.");
                    _logger.Error(exContentProvider.Message);

                    try
                    {
                        var broker = new AndroidAccountManagerBroker(_uIParent, _logger);
                        await broker.InitiateBrokerHandshakeAsync().ConfigureAwait(false);
                        return broker;
                    }
                    catch (Exception exAccountManager)
                    {
                        _logger.Error("Unable to communicate with the broker via the Account manager.");
                        _logger.Error(exAccountManager.Message);
                    }
                }
            }

            // Return a default broker in case no broker is installed to handle install URL
            return new AndroidContentProviderBroker(_uIParent, _logger);
        }

        public IBroker GetBroker()
        {
            return Task.Run(async () => await GetInstalledBrokerAsync().ConfigureAwait(false)).Result;
        }
    }
}
