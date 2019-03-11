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
using System.Threading.Tasks;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Microsoft.Identity.Client.UI;

namespace Microsoft.Identity.Client.PlatformsCommon.Shared
{
    internal abstract class AbstractPlatformProxy : IPlatformProxy
    {
        private readonly Lazy<string> _callingApplicationName;
        private readonly Lazy<string> _callingApplicationVersion;
        private readonly Lazy<ICryptographyManager> _cryptographyManager;
        private readonly Lazy<string> _deviceId;
        private readonly Lazy<string> _deviceModel;
        private readonly Lazy<string> _operatingSystem;
        private readonly Lazy<IPlatformLogger> _platformLogger;
        private readonly Lazy<string> _processorArchitecture;
        private readonly Lazy<string> _productName;

        protected AbstractPlatformProxy(ICoreLogger logger)
        {
            Logger = logger;
            _deviceModel = new Lazy<string>(InternalGetDeviceModel);
            _operatingSystem = new Lazy<string>(InternalGetOperatingSystem);
            _processorArchitecture = new Lazy<string>(InternalGetProcessorArchitecture);
            _callingApplicationName = new Lazy<string>(InternalGetCallingApplicationName);
            _callingApplicationVersion = new Lazy<string>(InternalGetCallingApplicationVersion);
            _deviceId = new Lazy<string>(InternalGetDeviceId);
            _productName = new Lazy<string>(InternalGetProductName);
            _cryptographyManager = new Lazy<ICryptographyManager>(InternalGetCryptographyManager);
            _platformLogger = new Lazy<IPlatformLogger>(InternalGetPlatformLogger);
        }

        protected IWebUIFactory OverloadWebUiFactory { get; set; }
        protected IFeatureFlags OverloadFeatureFlags { get; set; }

        protected ICoreLogger Logger { get; }

        /// <inheritdoc />
        public IWebUIFactory GetWebUiFactory()
        {
            return OverloadWebUiFactory ?? CreateWebUiFactory();
        }

        /// <inheritdoc />
        public void SetWebUiFactory(IWebUIFactory webUiFactory)
        {
            OverloadWebUiFactory = webUiFactory;
        }

        /// <inheritdoc />
        public string GetDeviceModel()
        {
            return _deviceModel.Value;
        }

        /// <inheritdoc />
        public abstract string GetEnvironmentVariable(string variable);

        /// <inheritdoc />
        public string GetOperatingSystem()
        {
            return _operatingSystem.Value;
        }

        /// <inheritdoc />
        public string GetProcessorArchitecture()
        {
            return _processorArchitecture.Value;
        }

        /// <inheritdoc />
        public abstract Task<string> GetUserPrincipalNameAsync();

        /// <inheritdoc />
        public abstract bool IsDomainJoined();

        /// <inheritdoc />
        public abstract Task<bool> IsUserLocalAsync(RequestContext requestContext);

        /// <inheritdoc />
        public string GetCallingApplicationName()
        {
            return _callingApplicationName.Value;
        }

        /// <inheritdoc />
        public string GetCallingApplicationVersion()
        {
            return _callingApplicationVersion.Value;
        }

        /// <inheritdoc />
        public string GetDeviceId()
        {
            return _deviceId.Value;
        }

        /// <inheritdoc />
        public abstract string GetBrokerOrRedirectUri(Uri redirectUri);

        /// <inheritdoc />
        public abstract string GetDefaultRedirectUri(string clientId);

        /// <inheritdoc />
        public string GetProductName()
        {
            return _productName.Value;
        }

        /// <inheritdoc />
        public abstract ILegacyCachePersistence CreateLegacyCachePersistence();

        /// <inheritdoc />
        public abstract ITokenCacheAccessor CreateTokenCacheAccessor();

        /// <inheritdoc />
        public ICryptographyManager CryptographyManager => _cryptographyManager.Value;

        /// <inheritdoc />
        public IPlatformLogger PlatformLogger => _platformLogger.Value;

        protected abstract IWebUIFactory CreateWebUiFactory();
        protected abstract IFeatureFlags CreateFeatureFlags();
        protected abstract string InternalGetDeviceModel();
        protected abstract string InternalGetOperatingSystem();
        protected abstract string InternalGetProcessorArchitecture();
        protected abstract string InternalGetCallingApplicationName();
        protected abstract string InternalGetCallingApplicationVersion();
        protected abstract string InternalGetDeviceId();
        protected abstract string InternalGetProductName();
        protected abstract ICryptographyManager InternalGetCryptographyManager();
        protected abstract IPlatformLogger InternalGetPlatformLogger();

        public virtual ITokenCacheBlobStorage CreateTokenCacheBlobStorage() 
        {
            return new NullTokenCacheBlobStorage();
        }

        public virtual IFeatureFlags GetFeatureFlags()
        {
            return OverloadFeatureFlags ?? GetFeatureFlags();
        }

        public void SetFeatureFlags(IFeatureFlags featureFlags)
        {
            OverloadFeatureFlags = featureFlags;
        }
    }
}
