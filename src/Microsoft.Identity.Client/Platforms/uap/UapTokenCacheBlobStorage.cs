using Microsoft.Identity.Client.Core;
using Microsoft.Identity.Client.PlatformsCommon.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Microsoft.Identity.Client.Platforms.uap
{
    internal class UapTokenCacheBlobStorage : ITokenCacheBlobStorage
    {
        private const string LocalSettingsContainerName = "MicrosoftAuthenticationLibrary3";

        private const string CacheValue = "CacheValue";
        private const string CacheValueSegmentCount = "CacheValueSegmentCount";
        private const string CacheValueLength = "CacheValueLength";
        private const int MaxCompositeValueLength = 1024;
        private readonly ICryptographyManager _cryptographyManager;

        public UapTokenCacheBlobStorage(ICryptographyManager cryptographyManager, ICoreLogger logger)
        {
            _cryptographyManager = cryptographyManager;
        }

        public void OnBeforeWrite(TokenCacheNotificationArgs args)
        {
            // NO OP
        }

        public void OnBeforeAccess(TokenCacheNotificationArgs args)
        {
            if (args?.TokenCache != null)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                byte[] state = GetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values);
                if (state != null)
                {
                    args.TokenCache.Deserialize(state);
                }
            }
        }

        public void OnAfterAccess(TokenCacheNotificationArgs args)
        {
            if (args?.TokenCache != null && args.HasStateChanged)
            {
                var localSettings = ApplicationData.Current.LocalSettings;
                localSettings.CreateContainer(LocalSettingsContainerName, ApplicationDataCreateDisposition.Always);
                SetCacheValue(localSettings.Containers[LocalSettingsContainerName].Values, args.TokenCache.Serialize());
            }
        }

        // TODO bogavril: consolidate with similar methods from UapLegacyCachePersistance
        internal void SetCacheValue(IPropertySet containerValues, byte[] value)
        {
            byte[] encryptedValue = _cryptographyManager.Encrypt(value);
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

            int encyptedValueLength = (int)containerValues[CacheValueLength];
            int segmentCount = (int)containerValues[CacheValueSegmentCount];

            byte[] encryptedValue = new byte[encyptedValueLength];
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

            Array.Copy((byte[])containerValues[CacheValue + (segmentCount - 1)], 0, encryptedValue, (segmentCount - 1) * MaxCompositeValueLength, encyptedValueLength - (segmentCount - 1) * MaxCompositeValueLength);
            return _cryptographyManager.Decrypt(encryptedValue);
        }



    }
}
