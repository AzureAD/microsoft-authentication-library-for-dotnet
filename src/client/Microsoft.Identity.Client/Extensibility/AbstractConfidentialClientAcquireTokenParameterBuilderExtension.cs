// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensions for <see cref="AcquireTokenForClientParameterBuilder"/>
    /// </summary>
    public static class AbstractConfidentialClientAcquireTokenParameterBuilderExtension
    {
        /// <summary>
        /// Overrides the client credentials parameters (e.g. client_assertion) and optionally ties the 
        /// resulting token to a key id, similar to POP tokens.
        /// </summary>
        /// <param name="clientAssertionDelegate">A lambda which receives a string representing the value of the token endpoint and which needs to return key value pairs to be added to the POST payload</param>
        /// <param name="builder">Builder to chain config options to</param>
        /// <returns>The builder</returns>
        public static AbstractConfidentialClientAcquireTokenParameterBuilder<T> WithClientAssertion<T>
            (this AbstractConfidentialClientAcquireTokenParameterBuilder<T> builder,
            Func<string, IReadOnlyList<KeyValuePair<string, string>>> clientAssertionDelegate)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.ValidateUseOfExperimentalFeature();
            builder.CommonParameters.ClientAssertionOverride = clientAssertionDelegate;

            return builder;
        }
    }
}
