//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    internal static class AdalErrorAndroidEx
    {
        public const string MissingPackagePermission = "missing_package_permission";
        public const string CannotSwitchToBrokerFromThisApp = "cannot_switch_to_broker_from_this_app";
        public const string IncorrectBrokerAccountType = "incorrect_broker_account_type";
        public const string IncorrectBrokerAppSignature = "incorrect_broker_app_signature";
        public const string FailedToGetBrokerAppSignature = "failed_to_get_broker_app_signature";
        public const string MissingBrokerRelatedPackage = "missing_broker_related_package";
        public const string MissingDigestShaAlgorithm = "missing_digest_sha_algorithm";
        public const string SignatureVerificationFailed = "signature_verification_failed";
        public const string NoBrokerAccountFound = "broker_account_not_found";
        public const string BrokerApplicationRequired = "broker_application_required";
    }

    internal static class AdalErrorMessageAndroidEx
    {
        public const string MissingPackagePermissionTemplate = "Permission {0} is missing from package manifest";
        public const string CannotSwitchToBrokerFromThisApp = "Cannot switch to broker from this app";
        public const string IncorrectBrokerAccountType = "Incorrect broker account type";
        public const string IncorrectBrokerAppSignate = "Incorrect broker app signature";
        public const string FailedToGetBrokerAppSignature = "Failed to get broker app signature";
        public const string MissingBrokerRelatedPackage = "Broker related package does not exist";
        public const string MissingDigestShaAlgorithm = "Digest SHA algorithm does not exists";
        public const string SignatureVerificationFailed = "Error in verifying broker app's signature";
        public const string NoBrokerAccountFound = "No account found in broker app";
        public const string BrokerApplicationRequired = "Broker application must be installed to continue authentication";
    }

    internal static class BrokerResponseCode
    {
        public const int UserCancelled = 2004;
        public const int ResponseReceived = 2001;
    }
}