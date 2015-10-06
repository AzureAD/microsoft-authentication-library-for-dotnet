using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    public static class AdalErrorEx
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
    }

    internal static class AdalErrorMessageEx
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
    }

    internal static class BrokerResponseCode
    {
        public const int UserCancelled = 2004;
        public const int ResponseReceived = 2001;
    }
}