// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Microsoft.Identity.Client.TelemetryCore.Internal.Constants;
using Microsoft.Identity.Client.TelemetryCore.Internal.Events;

namespace Microsoft.Identity.Client.TelemetryCore.Internal
{
    internal enum AccountType
    {
        MSA,
        AAD,
        B2C
    }

    internal enum IdentityService
    {
        AAD,
        MSA
    }

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
        public static string AsString(AccountType accountType)
        {
            switch (accountType)
            {
            case AccountType.MSA:
                return "msa";

            case AccountType.AAD:
                return "aad";

            case AccountType.B2C:
                return "b2c";

            default:
                return "unknown";
            }
        }

        public static string AsString(ActionType actionType)
        {
            switch (actionType)
            {
            case ActionType.Adal:
                return "adal";

            case ActionType.CustomInteractive:
                return "custominteractive";

            case ActionType.MsaInteractive:
                return "msainteractive";

            case ActionType.MsaNonInteractive:
                return "msanoninteractive";

            case ActionType.Wam:
                return "wam";

            case ActionType.Msal:
                return "msal";

            default:
                return "unknown";
            }
        }

        public static string AsString(AuthOutcome outcome)
        {
            switch (outcome)
            {
            case AuthOutcome.Cancelled:
                return "canceled";

            case AuthOutcome.Failed:
                return "failed";

            case AuthOutcome.Incomplete:
                return "incomplete";

            case AuthOutcome.Succeeded:
                return "succeeded";

            default:
                return "unknown";
            }
        }

        public static string AsString(ErrorSource errorSource)
        {
            switch (errorSource)
            {
            case ErrorSource.AuthSdk:
                return "authsdk";

            case ErrorSource.Client:
                return "client";

            case ErrorSource.None:
                return "none";

            case ErrorSource.Service:
                return "service";

            default:
                return "unknown";
            }
        }

        public static string AsString(EventType eventType)
        {
            switch (eventType)
            {
            case EventType.Scenario:
                return "scenario";

            case EventType.Action:
                return "action";

            case EventType.LibraryError:
                return "error";

            default:
                return "unknown";
            }
        }

        public static string AsString(IdentityService service)
        {
            switch (service)
            {
            case IdentityService.AAD:
                return "aad";

            case IdentityService.MSA:
                return "msa";

            default:
                return "unknown";
            }
        }

        public static string AsString(TelemetryAudienceType audience)
        {
            switch (audience)
            {
            case TelemetryAudienceType.PreProduction:
                return "preproduction";

            case TelemetryAudienceType.Production:
                return "production";

            default:
                return "unknown";
            }
        }

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
