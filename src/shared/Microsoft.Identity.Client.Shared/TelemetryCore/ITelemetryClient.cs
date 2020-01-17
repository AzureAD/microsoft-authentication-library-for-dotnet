// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore.Internal;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client.TelemetryCore
{
    internal enum AuthOutcome
    {
        Succeeded,
        Cancelled,
        Failed,
        Incomplete
    }

    internal enum ErrorSource
    {
        None,
        Service,
        AuthSdk,
        Client
    }

    internal enum InteractiveAuthContainerType
    {
        Embedded,
        CompanyPortal,
        Wam,
        Authenticator,
        SystemWebView,
        Browser
    }

    internal enum CustomIdentityService
    {
        EmailHrd,
        Basic,
        Fba,
        Kerberos,
        OnPremUnspecified
    }

    // This interface defines the root infrastructure for MATS.
    internal interface ITelemetryClient : IDisposable
    {
        ITelemetryManager TelemetryManager { get; }

        MatsScenario CreateScenario();

        MatsAction StartAction(
            MatsScenario scenario, 
            string correlationId);

        MatsAction StartActionWithScopes(
            MatsScenario scenario, 
            string correlationId, 
            IEnumerable<string> scopes);

        void EndAction(
            MatsAction action,
            AuthenticationResult authenticationResult);

        void EndAction(
            MatsAction action,
            Exception ex);

        void EndAction(
            MatsAction action, 
            AuthOutcome outcome, 
            ErrorSource errorSource, 
            string error, 
            string errorDescription);
    }
}
