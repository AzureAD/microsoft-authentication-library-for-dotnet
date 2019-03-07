// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Client.Mats.Internal
{
    internal enum WamIdentityService
    {
        Msa,
        Aad
    }

    internal enum WamApi
    {
        RequestToken,
        GetTokenSilently,
        Other
    }

    internal enum ActionType
    {
        Adal,
        CustomInteractive,
        MsaInteractive,
        MsaNonInteractive,
        Wam
    }

    internal interface IActionStore
    {
        /** ADAL action */
        AdalAction StartAdalAction(Scenario scenario, string correlationId, string resource);

        void ProcessAdalTelemetryBlob(IDictionary<string, string> blob);

        void EndAdalAction(AdalAction action, AuthOutcome outcome, ErrorSource errorSource, string error, string errorDescription);

        /** MSA interactive action  */
        InteractiveMsaAction StartInteractiveMsaAction(Scenario scenario, bool isBlockingUi, bool asksForCredentials, string correlationId, InteractiveAuthContainerType interactiveAuthContainerType, string scope);

        void EndInteractiveMsaActionWithSignin(InteractiveMsaAction action, string accountCid);

        void EndInteractiveMsaActionWithCancellation(InteractiveMsaAction action, string accountCid);

        void EndInteractiveMsaActionWithFailure(InteractiveMsaAction action, ErrorSource errorSource, string error, string errorDescription, string accountCid);

        /** Custom interactive action */
        CustomInteractiveAction StartCustomInteractiveAction(Scenario scenario, bool isBlockingUi, bool asksForCredentials, string correlationId, InteractiveAuthContainerType interactiveAuthContainerType, CustomIdentityService identityService);

        void EndCustomInteractiveActionWithSuccess(CustomInteractiveAction action);

        void EndCustomInteractiveActionWithCancellation(CustomInteractiveAction action);

        void EndCustomInteractiveActionWithFailure(CustomInteractiveAction action, ErrorSource errorSource, string error, string errorDescription);

        /** MSA non-interactive action */
        NonInteractiveMsaAction StartNonInteractiveMsaAction(Scenario scenario, string correlationId, string scope);

        void EndNonInteractiveMsaActionWithTokenRetrieval(NonInteractiveMsaAction action, string accountCid);

        void EndNonInteractiveMsaActionWithFailure(NonInteractiveMsaAction action, ErrorSource errorSource, string error, string errorDescription, string accountCid);

        WamAction StartWamAction(Scenario scenario, string correlationId, bool forcePrompt, WamIdentityService identityService, WamApi wamApi, string scope, string resource);

        void EndWamActionWithSuccess(WamAction action, string accountId, string tenantId, string wamTelemetryBatch);

        void EndWamActionWithCancellation(WamAction action, string wamTelemetryBatch);

        void EndWamActionWithFailure(WamAction action, ErrorSource errorSource, string error, string errorDescription, string accountId, string tenantId, string wamTelemetryBatch);

        IEnumerable<IPropertyBag> GetEventsForUpload();
    }
}
