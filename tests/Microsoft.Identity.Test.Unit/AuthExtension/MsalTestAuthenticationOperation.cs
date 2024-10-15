// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AuthScheme;

namespace Microsoft.Identity.Test.Unit.AuthExtension
{
    internal class MsalTestAuthenticationOperation : IAuthenticationOperation
    {
        public int TelemetryTokenType => 1;

        public string AuthorizationHeaderPrefix => "someHeader";

        public string KeyId => "someKeyId";

        public string AccessTokenType => "someAccessTokenType";

        public void FormatResult(AuthenticationResult authenticationResult)
        {
            authenticationResult.AccessToken = authenticationResult.AccessToken 
                                                + "AccessTokenModifier" 
                                                + authenticationResult.AdditionalResponseParameters["additional_param1"]
                                                + authenticationResult.AdditionalResponseParameters["additional_param2"];
        }

        public IReadOnlyDictionary<string, string> GetTokenRequestParams()
        {
            IDictionary<string, string> requestParams = new Dictionary<string, string>();
            requestParams.Add("key1", "value1");
            requestParams.Add("key2", "value2");

            return (IReadOnlyDictionary<string, string>)requestParams;
        }
    }
}
