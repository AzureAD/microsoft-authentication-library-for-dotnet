// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Identity.Client.Advanced
{
    /// <summary>
    /// </summary>
    public static class AcquireTokenParameterBuilderExtensions
    {
        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static T WithExtraHttpHeaders<T>(this AbstractAcquireTokenParameterBuilder<T> builder, IDictionary<string, string> extraHttpHeaders)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return (T)builder;
        }
    }
}
