// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

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
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This method has been moved to the Microsoft.Identity.Client.Extensibility namespace", false)]
        public static T WithExtraHttpHeaders<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder, 
            IDictionary<string, string> extraHttpHeaders)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {
            return Microsoft.Identity.Client.Extensibility.AcquireTokenParameterBuilderExtensions.WithExtraHttpHeaders(builder, extraHttpHeaders);
        }
    }
}

// Extensibility (new surface for WithExtraHttpHeaders)
namespace Microsoft.Identity.Client.Extensibility
{
    /// <summary>
    /// Extensibility helpers for acquire token parameter builders.
    /// </summary>
    public static class AcquireTokenParameterBuilderExtensions
    {
        /// <summary>Adds additional HTTP headers to the token request.</summary>
        /// <param name="builder">Parameter builder for acquiring tokens.</param>
        /// <param name="extraHttpHeaders">Additional HTTP headers to add to the token request.</param>
        public static T WithExtraHttpHeaders<T>(
            this AbstractAcquireTokenParameterBuilder<T> builder,
            IDictionary<string, string> extraHttpHeaders)
            where T : AbstractAcquireTokenParameterBuilder<T>
        {            
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return (T)builder;
        }
    }
}
