// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal enum OsPlatform
    {
        Win32,
        Android,
        Ios,
        Mac,
        Winrt
    }

    internal static class MatsConverter
    {
        public static string AsString(OsPlatform osPlatform)
        {
            switch (osPlatform)
            {
                case OsPlatform.Win32:
                    return "win32";

                case OsPlatform.Android:
                    return "android";

                case OsPlatform.Ios:
                    return "ios";

                case OsPlatform.Mac:
                    return "mac";

                case OsPlatform.Winrt:
                    return "winrt";

                default:
                    return "unknown";
            }
        }

        public static int AsInt(OsPlatform osPlatform)
        {
            return Convert.ToInt32(osPlatform, CultureInfo.InvariantCulture);
        }

        public static string AsString(ApiTelemetryFeature apiTelemetryFeature)
        {
            switch (apiTelemetryFeature)
            {
                case ApiTelemetryFeature.WithAccount:
                    return ApiTelemetryFeatureKey.WithAccount;
                case ApiTelemetryFeature.WithRedirectUri:
                    return ApiTelemetryFeatureKey.WithRedirectUri;
                case ApiTelemetryFeature.WithForceRefresh:
                    return ApiTelemetryFeatureKey.WithForceRefresh;
                case ApiTelemetryFeature.WithLoginHint:
                    return ApiTelemetryFeatureKey.WithLoginHint;
                case ApiTelemetryFeature.WithExtraScopesToConsent:
                    return ApiTelemetryFeatureKey.WithExtraScopesToConsent;
                case ApiTelemetryFeature.WithUserAssertion:
                    return ApiTelemetryFeatureKey.WithUserAssertion;
                case ApiTelemetryFeature.WithSendX5C:
                    return ApiTelemetryFeatureKey.WithSendX5C;
                case ApiTelemetryFeature.WithCurrentSynchronizationContext:
                    return ApiTelemetryFeatureKey.WithCurrentSynchronizationContext;
                case ApiTelemetryFeature.WithEmbeddedWebView:
                    return ApiTelemetryFeatureKey.WithEmbeddedWebView;
                case ApiTelemetryFeature.WithParent:
                    return ApiTelemetryFeatureKey.WithParent;
                case ApiTelemetryFeature.WithPrompt:
                    return ApiTelemetryFeatureKey.WithPrompt;
                case ApiTelemetryFeature.WithUsername:
                    return ApiTelemetryFeatureKey.WithUsername;
                case ApiTelemetryFeature.WithClaims:
                    return ApiTelemetryFeatureKey.WithClaims;
                case ApiTelemetryFeature.WithExtraQueryParameters:
                    return ApiTelemetryFeatureKey.WithExtraQueryParameters;
                case ApiTelemetryFeature.WithAuthority:
                    return ApiTelemetryFeatureKey.WithAuthority;
                case ApiTelemetryFeature.WithValidateAuthority:
                    return ApiTelemetryFeatureKey.WithValidateAuthority;
                case ApiTelemetryFeature.WithAdfsAuthority:
                    return ApiTelemetryFeatureKey.WithAdfsAuthority;
                case ApiTelemetryFeature.WithB2CAuthority:
                    return ApiTelemetryFeatureKey.WithB2CAuthority;
                case ApiTelemetryFeature.WithCustomWebUi:
                    return ApiTelemetryFeatureKey.WithCustomWebUi;
                default:
                    return ApiTelemetryFeatureKey.Unknown;
            }
        }
    }
}
