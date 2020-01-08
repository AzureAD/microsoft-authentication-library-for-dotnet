// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.Platforms.iOS
{
    internal class iOSBrokerConstants
    {
        public const string ChallengeResponseHeader = "Authorization";
        public const string ChallengeResponseType = "PKeyAuth";
        public const string ChallengeResponseToken = "AuthToken";
        public const string ChallengeResponseContext = "Context";
        public const string ChallengeResponseVersion = "Version";
        public const string BrowserExtPrefix = "browser://";
        public const string BrowserExtInstallPrefix = "msauth://";
        public const string DeviceAuthChallengeRedirect = "urn:http-auth:PKeyAuth";
        public const string ChallengeHeaderKey = "x-ms-PKeyAuth";
        public const string ChallengeHeaderValue = "1.0";

        public const string BrokerKey = "broker_key";
        public const string MsgProtocolVer = "msg_protocol_ver";
        public const string Claims = "claims";
        public const string SkipCache = "skip_cache";
        public const string AppLink = "app_link";
        public const string InvokeBroker = "msauthv2://broker?";
        public const string Code = "code";
        public const string BrokerError = "broker_error";
        public const string Error = "error";
        public const string ErrorDescription = "error_description";
        public const string ExpectedHash = "hash";
        public const string EncryptedResponsed = "response";
        public const string BrokerNonce = "broker_nonce";

        public const string BrokerKeyService = "Broker Key Service";
        public const string BrokerKeyAccount = "Broker Key Account";
        public const string BrokerKeyLabel = "Broker Key Label";
        public const string BrokerKeyComment = "Broker Key Comment";
        public const string BrokerKeyStorageDescription = "Storage for broker key";
        public const string LocalSettingsContainerName = "MicrosoftIdentityClient";

        // broker related log messages
        public const string InvokeIosBrokerAppLink = "Invoking the iOS broker app link";
        public const string InvokeTheIosBroker = "Invoking the iOS broker";
        public const string FailedToSaveBrokerKey = "Failed to save broker key. Security Keychain Status code: ";
        public const string UiParentIsNullCannotInvokeBroker = "UIParent is null cannot invoke broker. ";
        public const string CallerViewControllerIsNullCannotInvokeBroker = "The CallerViewController is null. See https://aka.ms/msal-net-ios-Broker for details. ";
        public const string CanInvokeBroker = "Can invoke broker? ";
        public const string CanInvokeBrokerReturnsFalseMessage = "Cannot invoke the iOS broker. Returned from CanOpenUrl. msauthv2 and msauthv3 need to be included " +
                    "in LSApplicationQueriesSchemes in Info.plist. See aka.ms/iosBroker for more information. ";
        public const string iOSBrokerv2Installed = "iOS broker msauthv2:// installed on device: ";
        public const string iOSBrokerv3Installed = "iOS broker msauthv3:// installed on device: ";
        public const string BrokerPayloadContainsInstallUrl = "iOS Broker - broker payload contains install url. ";
        public const string StartingActionViewActivity = "iOS Broker - Starting ActionView activity to ";
        public const string BrokerResponseContainsError = "Broker response contains an error. ";
        public const string ProcessBrokerResponse = "Processed iOS Broker response. Response Dictionary count: ";
        public const string BrokerPayloadPii = "iOS Broker Payload: ";
        public const string BrokerPayloadNoPii = "iOS Broker Payload Count: ";
        public const string BrokerResponseValuesPii = "iOS Broker Response Values: ";
        public const string AttemptToSaveBrokerApplicationToken = "Attempt to save iOS broker application token resulted in: ";
        public const string SecStatusCodeFromTryGetBrokerApplicationToken = "The SecStatusCode from trying to get the broker application token is: ";
        public const string iOSBroker = "iOS_broker";
        public const string ApplicationToken = "application_token"; // included in request and response with v3 iOS broker
    }
}
