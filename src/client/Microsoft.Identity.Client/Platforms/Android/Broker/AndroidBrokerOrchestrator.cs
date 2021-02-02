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
        CoreUIParent _uIParent;
        ICoreLogger _logger;
        AndroidBrokerHelper _brokerHelper;
        static bool s_contentProviderIsAvailable;

        public AndroidBrokerOrchestrator(CoreUIParent uiParent, ICoreLogger logger)
        {
            _uIParent = uiParent;
            _logger = logger;
            _brokerHelper = new AndroidBrokerHelper(Application.Context, _logger);
        }

        private async Task<IBroker> GetInstalledBrokerAsync()
        {
            if (IsBrokerInstalledAndInvokable())
            {
                try
                {
                    var broker = new AndroidContentProviderBroker(_uIParent, _logger);

                    if (!s_contentProviderIsAvailable)
                    {
                        broker.InitiateBrokerHandshakeAsync();
                        s_contentProviderIsAvailable = true;
                    }
                }
                catch
                {
                    _logger.Info("Unable to handshake with Content Provider Broker. Attempting handshake with Account Manager Broker.");

                    try
                    {
                        var broker = new AndroidBroker(_uIParent, _logger);
                        await broker.InitiateBrokerHandshakeAsync(_uIParent.CallerActivity).ConfigureAwait(false);
                    }
                    catch
                    {
                        _logger.Info("Unable to connect to any of the brokers.");
                    }
                }
            }

            // Return a default broker in case no broker is installed to handle install url
            return new AndroidContentProviderBroker(_uIParent, _logger);
        }

        internal IBroker GetBroker()
        {
            return Task.Run(async () => await GetInstalledBrokerAsync().ConfigureAwait(false)).Result;
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            return _brokerHelper.IsBrokerInstalledAndInvokable();
        }

        internal static void SetBrokerResult(Intent data, int resultCode, ICoreLogger unreliableLogger)
        {
            AndroidBrokerHelper.SetBrokerResult(data, resultCode, unreliableLogger);
        }
    }
}
