// ------------------------------------------------------------------------------
// 
// Copyright (c) Microsoft Corporation.
// All rights reserved.
// 
// This code is licensed under the MIT License.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Identity.Client.Http;

namespace Microsoft.Identity.Client.Config
{
    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class AbstractConfigurationBuilder<T>
        where T : AbstractConfigurationBuilder<T>
    {
        internal AbstractConfigurationBuilder(ApplicationConfiguration configuration)
        {
            Config = configuration;
        }

        internal ApplicationConfiguration Config { get; }

        /// <summary>
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <returns></returns>
        public T WithHttpClientFactory(IMsalHttpClientFactory httpClientFactory)
        {
            Config.HttpClientFactory = httpClientFactory;
            return (T)this;
        }

        internal T WithHttpManager(IHttpManager httpManager)
        {
            Config.HttpManager = httpManager;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T WithLoggingCallback(LogCallback loggingCallback)
        {
            Config.LoggingCallback = loggingCallback;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T WithTelemetryCallback(ITelemetryReceiver telemetryReceiver)
        {
            Config.TelemetryReceiver = telemetryReceiver;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="onlySendFailureTelemetryData"></param>
        /// <returns></returns>
        public T WithOnlySendFailureTelemetryData(bool onlySendFailureTelemetryData)
        {
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public T WithClientId(string clientId)
        {
            if (!string.IsNullOrWhiteSpace(clientId))
            {
                Config.ClientId = clientId;
            }

            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="redirectUri"></param>
        /// <returns></returns>
        public T WithRedirectUri(string redirectUri)
        {
            Config.RedirectUri = GetValueIfNotEmpty(Config.RedirectUri, redirectUri);
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="tenant"></param>
        /// <returns></returns>
        public T WithTenant(string tenant)
        {
            Config.Tenant = GetValueIfNotEmpty(Config.Tenant, tenant);
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="enablePiiLogging"></param>
        /// <returns></returns>
        public T WithEnablePiiLogging(bool enablePiiLogging)
        {
            Config.EnablePiiLogging = enablePiiLogging;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public T WithLoggingLevel(LogLevel logLevel)
        {
            Config.LogLevel = logLevel;
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        /// <returns></returns>
        public T WithDefaultPlatformLoggingEnabled(bool enabled)
        {
            Config.IsDefaultPlatformLoggingEnabled = enabled;
            return (T)this;
        }

        /// <summary>
        /// </summary>
        /// <param name="applicationOptions"></param>
        /// <returns></returns>
        public T WithOptions(ApplicationOptions applicationOptions)
        {
            WithClientId(applicationOptions.ClientId);
            WithRedirectUri(applicationOptions.RedirectUri);
            WithTenant(applicationOptions.Tenant);
            WithLoggingLevel(applicationOptions.LogLevel);
            WithComponent(applicationOptions.Component);
            WithEnablePiiLogging(applicationOptions.EnablePiiLogging);
            WithDefaultPlatformLoggingEnabled(applicationOptions.IsDefaultPlatformLoggingEnabled);
            WithSliceParameters(applicationOptions.SliceParameters);

            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="component"></param>
        /// <returns></returns>
        public T WithComponent(string component)
        {
            Config.Component = GetValueIfNotEmpty(Config.Component, component);
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sliceParameters"></param>
        /// <returns></returns>
        public T WithSliceParameters(string sliceParameters)
        {
            Config.SliceParameters = GetValueIfNotEmpty(Config.SliceParameters, sliceParameters);
            return (T)this;
        }

        internal ApplicationConfiguration BuildConfiguration()
        {
            // todo: final validation/sanity checks here...

            // validate that we only have ONE default authority
            if (Config.Authorities.Where(x => x.IsDefault).ToList().Count > 1)
            {
                throw new InvalidOperationException("More than one default authority was configured.");
            }
            if (!Config.Authorities.Any())
            {
                throw new InvalidOperationException("No authorities were configured.");
            }

            return Config;
        }

        /// <summary>
        /// Note, we'll probably want authority improvements, but bootstrapping with this...
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="validateAuthority"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T WithAuthority(string authorityUri, bool validateAuthority, bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(AuthorityInfo.FromAuthorityUri(authorityUri, validateAuthority, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="validateAuthority"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T WithAadAuthority(string authorityUri, bool validateAuthority, bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Aad, authorityUri, validateAuthority, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorityAudience"></param>
        /// <param name="validateAuthority"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T WithAadAuthority(AadAuthorityAudience authorityAudience, bool validateAuthority, bool isDefaultAuthority)
        {
            string authorityUri = AuthorityUriFromAadAuthorityAudience(authorityAudience);
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Aad, authorityUri, validateAuthority, isDefaultAuthority));
            return (T)this;
        }

        internal static string AuthorityUriFromAadAuthorityAudience(AadAuthorityAudience authorityAudience)
        {
            switch (authorityAudience)
            {
            case AadAuthorityAudience.AzureAdAndPersonalMicrosoftAccount:
                return "https://login.microsoftonline.com/common/";
            case AadAuthorityAudience.AzureAdOnly:
                return "https://login.microsoftonline.com/organizations/";
            case AadAuthorityAudience.MicrosoftAccountOnly:
                return "https://login.microsoftonline.com/consumers/";
            case AadAuthorityAudience.None:
            default:
                throw new ArgumentException(nameof(authorityAudience));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="validateAuthority"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T WithAdfsAuthority(string authorityUri, bool validateAuthority, bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.Adfs, authorityUri, validateAuthority, isDefaultAuthority));
            return (T)this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authorityUri"></param>
        /// <param name="isDefaultAuthority"></param>
        /// <returns></returns>
        public T WithB2CAuthority(string authorityUri, bool isDefaultAuthority)
        {
            Config.AddAuthorityInfo(new AuthorityInfo(AuthorityType.B2C, authorityUri, false, isDefaultAuthority));
            return (T)this;
        }

        private static string GetValueIfNotEmpty(string original, string value)
        {
            return string.IsNullOrWhiteSpace(value) ? original : value;
        }
    }
}