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
        public static AcquireTokenInteractiveParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenInteractiveParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenByAuthorizationCodeParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenByAuthorizationCodeParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenByIntegratedWindowsAuthParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenByIntegratedWindowsAuthParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenByRefreshTokenParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenByRefreshTokenParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenByUsernamePasswordParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenByUsernamePasswordParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenForClientParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenForClientParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenOnBehalfOfParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenOnBehalfOfParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenSilentParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenSilentParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }

        /// <summary>
        /// Adds additional Http Headers to the token request.
        /// </summary>
        /// <param name="builder">Parameter builder for a acquiring tokens.</param>
        /// <param name="extraHttpHeaders">additional Http Headers to add to the token request.</param>
        /// <returns></returns>
        public static AcquireTokenWithDeviceCodeParameterBuilder WithExtraHttpHeaders(
            this AcquireTokenWithDeviceCodeParameterBuilder builder,
            IDictionary<string, string> extraHttpHeaders)
        {
            builder.CommonParameters.ExtraHttpHeaders = extraHttpHeaders;
            return builder;
        }
    }
}
