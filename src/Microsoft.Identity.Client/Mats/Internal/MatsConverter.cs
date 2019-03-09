// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Identity.Client.AppConfig;

namespace Microsoft.Identity.Client.Mats.Internal
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

        public static string AsString(MatsAudienceType audience)
        {
            switch (audience)
            {
            case MatsAudienceType.PreProduction:
                return "preproduction";

            case MatsAudienceType.Production:
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
    }
}
