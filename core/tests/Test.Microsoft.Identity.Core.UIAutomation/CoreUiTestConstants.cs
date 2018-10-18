using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    public static class CoreUiTestConstants
    {
        //Applications
        public const string UiAutomationTestClientId = "3c1e0e0d-b742-45ba-a35e-01c664e14b16";
        public const string MSIDLAB4ClientId = "4b0db8c2-9f26-4417-8bde-3f0e3656f8e0";
        public const string UIAutomationAppV2 = "1e245a30-49aa-43eb-b9c1-c11b072cc92b";

        //Resources
        public const string MSGraph = "https://graph.microsoft.com";
        public const string Exchange = "https://outlook.office365.com/";
        public const string UiAutomationTestResource = "ae55a6cc-da5e-42f8-b75d-c37e41a1a0d9";

        //ADAL & MSAL test app
        public const string AcquireTokenID = "acquireToken";
        public const string AcquireTokenSilentID = "acquireTokenSilent";
        public const string ClientIdEntryID = "clientIdEntry";
        public const string ResourceEntryID = "resourceEntry";
        public const string SecondPageID = "secondPage";
        public const string ClearCacheID = "clearCache";
        public const string SaveID = "saveButton";
        public const string WebUPNInputID = "i0116";
        public const string AdfsV4WebPasswordID = "passwordInput";
        public const string AdfsV4WebSubmitID = "submitButton";
        public const string WebPasswordID = "i0118";
        public const string WebSubmitID = "idSIButton9";
        public const string TestResultID = "testResult";
        public const string TestResultSuccsesfulMessage = "Result: Success";
        public const string TestResultFailureMessage = "Result: Failure";

        //MSAL test app
        public const string DefaultScope = "User.Read";
        public const string AcquirePageID = "Acquire";
        public const string CachePageID = "Cache";
        public const string SettignsPageID = "Settigns";
        public const string ScopesEntryID = "scopesList";

        //Test Constants
        public const int ResultCheckPolliInterval = 1000;
        public const int maximumResultCheckRetryAttempts = 20;
    }
}
