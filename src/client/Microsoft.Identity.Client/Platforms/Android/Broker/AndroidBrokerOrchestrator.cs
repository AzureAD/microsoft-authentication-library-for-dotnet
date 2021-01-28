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
        static IBroker s_broker;

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
                    _brokerHelper.InitiateCRBrokerHandshakeAsync(_uIParent.Activity);
                    s_broker = new AndroidContentProviderBroker(_uIParent, _logger);
                }
                catch
                {
                    _logger.Info("Unable to handshake with Content Provider Broker. Attempting handshake with Account Manager Broker.");

                    try
                    {
                        await _brokerHelper.InitiateBrokerHandshakeAsync(null).ConfigureAwait(false);
                        s_broker = new AndroidBroker(_uIParent, _logger);
                    }
                    catch
                    {
                        _logger.Info("Unable to connect to any of the brokers.");
                    }
                }
            }

            return s_broker;
        }

        internal IBroker GetBroker()
        {
            return s_broker ?? Task.Run(async () => await GetInstalledBrokerAsync().ConfigureAwait(false)).Result;
        }

        public bool IsBrokerInstalledAndInvokable()
        {
            using (_logger.LogMethodDuration())
            {
                bool canInvoke = _brokerHelper.CanSwitchToBroker();
                _logger.Verbose("Can invoke broker? " + canInvoke);

                return canInvoke;
            }
        }

        internal static void SetBrokerResult(Intent data, int resultCode, ICoreLogger unreliableLogger)
        {
            AndroidBrokerHelper.SetBrokerResult(data, resultCode, unreliableLogger);
        }
    }
}
