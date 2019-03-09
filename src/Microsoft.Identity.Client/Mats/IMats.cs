// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats
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

    internal interface IMats : IDisposable
    {
        void ProcessTelemetryBlob(Dictionary<string, string> blob);

        IScenarioHandle CreateScenario();

        IActionHandle StartAction(
            IScenarioHandle scenario, 
            string correlationId);

        IActionHandle StartActionWithResource(
            IScenarioHandle scenario, 
            string correlationId, 
            string resource);

        void EndAction(
            IActionHandle action,
            AuthenticationResult authenticationResult);

        void EndAction(
            IActionHandle action,
            Exception ex);

        void EndAction(
            IActionHandle action, 
            AuthOutcome outcome, 
            ErrorSource errorSource, 
            string error, 
            string errorDescription);
    }
}
