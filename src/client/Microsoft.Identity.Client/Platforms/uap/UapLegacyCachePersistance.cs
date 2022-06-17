// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.Identity.Client.Cache;
using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using Windows.Security.Cryptography.DataProtection;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Microsoft.Identity.Client.Platforms.uap
{
    /// <summary>
    /// Important: this uses ApplicationDataContainer which is thread safe, but it has limitations 
    /// related to the key data size, which is why MSAL caching does not use it.
    /// </summary>
    internal class UapLegacyCachePersistence : ILegacyCachePersistence
    {
        private const string LocalSettingsContainerName = "ActiveDirectoryAuthenticationLibrary";
        private const string ProtectionDescriptor = "LOCAL=user";

        private const string CacheValue = "CacheValue";
        private const string CacheValueSegmentCount = "CacheValueSegmentCount";
        private const string CacheValueLength = "CacheValueLength";
        private const int MaxCompositeValueLength = 1024;

        private readonly ICryptographyManager _cryptographyManager;
        private readonly ILoggerAdapter _logger;

        public UapLegacyCachePersistence(ILoggerAdapter logger, ICryptographyManager cryptographyManager)
        {
            _logger = logger;
            _cryptographyManager = cryptographyManager;
        }

        byte[] ILegacyCachePersistence.LoadCache()
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.CreateContainer(LocalSettingsContainerName,
                    ApplicationDataCreateDisposition.Always);
                return GetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values);
            }
            catch (Exception ex)
            {
                _logger.WarningPiiWithPrefix(ex, "Failed to load adal cache: ");
                // Ignore as the cache seems to be corrupt
            }

            return null;
        }

        void ILegacyCachePersistence.WriteCache(byte[] serializedCache)
        {
            try
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                SetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values, serializedCache);

            }
            catch (Exception ex)
            {
                _logger.WarningPiiWithPrefix(ex, "Failed to save adal cache: ");
            }
        }

        internal void SetCacheValue(IPropertySet containerValues, byte[] value)
        {
            byte[] encryptedValue = Encrypt(value);
            containerValues[CacheValueLength] = encryptedValue.Length;
            if (encryptedValue.Length == 0)
            {
                containerValues[CacheValueSegmentCount] = 1;
                containerValues[CacheValue + 0] = null;
            }
            else
            {
                int segmentCount = (encryptedValue.Length / MaxCompositeValueLength) + ((encryptedValue.Length % MaxCompositeValueLength == 0) ? 0 : 1);
                byte[] subValue = new byte[MaxCompositeValueLength];
                for (int i = 0; i < segmentCount - 1; i++)
                {
                    Array.Copy(encryptedValue, i * MaxCompositeValueLength, subValue, 0, MaxCompositeValueLength);
                    containerValues[CacheValue + i] = subValue;
                }

                int copiedLength = (segmentCount - 1) * MaxCompositeValueLength;
                Array.Copy(encryptedValue, copiedLength, subValue, 0, encryptedValue.Length - copiedLength);
                containerValues[CacheValue + (segmentCount - 1)] = subValue;
                containerValues[CacheValueSegmentCount] = segmentCount;
            }
        }

        internal byte[] GetCacheValue(IPropertySet containerValues)
        {
            if (!containerValues.ContainsKey(CacheValueLength))
            {
                return null;
            }

            int encryptedValueLength = (int)containerValues[CacheValueLength];
            int segmentCount = (int)containerValues[CacheValueSegmentCount];

            byte[] encryptedValue = new byte[encryptedValueLength];
            if (segmentCount == 1)
            {
                encryptedValue = (byte[])containerValues[CacheValue + 0];
            }
            else
            {
                for (int i = 0; i < segmentCount - 1; i++)
                {
                    Array.Copy((byte[])containerValues[CacheValue + i], 0, encryptedValue, i * MaxCompositeValueLength, MaxCompositeValueLength);
                }
            }

            Array.Copy((byte[])containerValues[CacheValue + (segmentCount - 1)], 0, encryptedValue, (segmentCount - 1) * MaxCompositeValueLength, encryptedValueLength - (segmentCount - 1) * MaxCompositeValueLength);
            return Decrypt(encryptedValue);
        }

        private byte[] Encrypt(byte[] message)
        {
            if (message == null)
            {
                return new byte[] { };
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer protectedBuffer = dataProtectionProvider.ProtectAsync(message.AsBuffer()).AsTask().GetAwaiter().GetResult();
            return protectedBuffer.ToArray(0, (int)protectedBuffer.Length);
        }

        private byte[] Decrypt(byte[] encryptedMessage)
        {
            if (encryptedMessage == null)
            {
                return null;
            }

            DataProtectionProvider dataProtectionProvider = new DataProtectionProvider(ProtectionDescriptor);
            IBuffer buffer = dataProtectionProvider.UnprotectAsync(encryptedMessage.AsBuffer()).AsTask().GetAwaiter().GetResult();
            return buffer.ToArray(0, (int)buffer.Length);
        }
    }
}
