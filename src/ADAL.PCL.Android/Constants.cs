//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

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
        public const string MissingDigestShaAlgorithm = "Digest SHA algorithm does not exist";
        public const string SignatureVerificationFailed = "Error in verifying broker app's signature";
        public const string NoBrokerAccountFound = "No account found in broker app";
        public const string BrokerApplicationRequired = "Broker application must be installed to continue authentication";
    }

    internal static class BrokerResponseCode
    {
        public const int UserCancelled = 2001;
        public const int BrowserCodeError = 2002;
        public const int ResponseReceived = 2004;
    }
}
