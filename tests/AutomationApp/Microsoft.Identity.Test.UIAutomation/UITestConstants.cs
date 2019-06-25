// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Identity.Test.UIAutomation
{
    public static class UITestConstants
    {
        // Resources
        public const string MSGraph = "https://graph.microsoft.com";
        public const string Exchange = "https://outlook.office365.com/";

        // Test to run
        public const string AcquireTokenInteractive = "Acquire Token Interactive";
        public const string AcquireTokenInteractiveConsentWithSelectAccount = "Prompt Behavior Consent with Select Account";
        public const string AcquireTokenSilent = "Acquire Token Silent";
        public const string ADFSv3Federated = "ADFSv3 Federated";
        public const string ADFSv3NonFederated = "ADFSv3 Non Federated";
        public const string ADFSv4Federated = "ADFSv4 Federated";
        public const string ADFSv4NonFederated = "ADFSv4 Non Federated";
        public const string ADFSv2019Federated = "ADFSv2019 Federated";
        public const string ADFSv2019NonFederated = "ADFSv2019 Non Federated";
        public const string B2CROPC = "B2C ROPC";
        public const string B2CFacebookb2clogin = "B2C Facebook b2clogin.com";
        public const string B2CFacebookb2cloginEditProfile= "B2C Facebook b2clogin.com edit profile";
        public const string B2CFacebookMicrosoftLogin = "B2C Facebook microsoftonline.com";
        public const string B2CLocalb2clogin = "B2C Local b2clogin.com";
        public const string B2CLocalMicrosoftLogin = "B2C Local microsoftonline.com";
        public const string B2CGoogleb2clogin = "B2C Google b2clogin.com";
        public const string B2CGoogleMicrosoftLogin = "B2C Google microsoftonline.com";

        public const string TestsToRunPicker = "uiTestPicker";
        public const string AcquireTokenButtonId = "acquireToken_button";
        public const string AcquireTokenWithPromptBehaviorAlwaysId = "acquireTokenPromptBehaviorAlways";
        public const string AcquireTokenSilentButtonId = "acquireTokenSilent_button";
        public const string ClientIdEntryId = "clientIdEntry";
        public const string ResourceEntryId = "resourceEntry";
        public const string PromptBehaviorEntryId = "promptBehaviorEntry";
        public const string PromptBehaviorAuto = "auto";
        public const string PromptBehaviorAlways = "always";
        public const string SecondPageId = "secondPage";
        public const string ClearCacheId = "clearCache";
        public const string SaveID = "saveButton";
        public const string WebUPNInputId = "i0116";
        public const string AdfsV4UsernameInputdId = "userNameInput";
        public const string AdfsV4WebPasswordId = "passwordInput";
        public const string AdfsV4WebSubmitId = "submitButton";
        public const string WebPasswordId = "i0118";
        public const string WebSubmitId = "idSIButton9";
        public const string TestResultId = "testResult";
        public const string TestResultSuccessfulMessage = "Result: Success";
        public const string TestResultFailureMessage = "Result: Failure";
        public const string ClearAllCacheId = "ClearAllCache";

        public const string DefaultScope = "User.Read";
        public const string AcquirePageId = "AcquirePage";
        public const string CachePageID = "CachePage";
        public const string SettingsPageId = "SettingsPage";
        public const string LogPageId = "LogPage";
        public const string AcquirePageAndroidId = "Acquire";
        public const string CachePageAndroidID = "Cache";
        public const string SettingsPageAndroidId = "Settings";
        public const string LogPageAndroidId = "Log";
        public const string ScopesEntryId = "scopesList";
        public const string UiBehaviorPickerId = "uiBehavior";
        public const string SelectUser = "userList";
        public const string UserNotSelected = "not selected";
        public const string UserMissingFromResponse = "Missing from the token response";
        public const string RedirectUriOnAndroid = "urn:ietf:wg:oauth:2.0:oob";
        public const string RedirectUriEntryId = "redirectUriEntry";
        public const string ConsentAcceptId = "idBtn_Accept";
        public const string PasswordInput = "password";
        public const string AlternativePasswordInput = "Passwd";

        // ADFSv2 fields
        public const string AdfsV2WebUsernameInputId = "ContentPlaceHolder1_UsernameTextBox";
        public const string AdfsV2WebPasswordInputId = "ContentPlaceHolder1_PasswordTextBox";
        public const string AdfsV2WebSubmitButtonId = "ContentPlaceHolder1_SubmitButton";

        //MSAL B2C
        public const string AuthorityPickerId = "b2cAuthorityPicker";
        public const string WebUpnB2CLocalInputId = "logonIdentifier";
        public const string B2CWebSubmitId = "next";
        public const string B2CWebPasswordId = "password";
        public const string B2CLoginAuthority = "b2clogin.com";
        public const string ROPC = "ROPC";
        public const string ROPCUsernameId = "usernameId";
        public const string ROPCPasswordId = "passwordId";
        public const string MicrosoftOnlineAuthority = "login.microsoftonline.com";
        public const string NonB2CAuthority = "non-b2c authority";
        public const string B2CEditProfileAuthority = "Edit profile policy authority";
        public const string FacebookAccountId = "FacebookExchange";
        public const string WebUpnB2CFacebookInputId = "m_login_email";
        public const string B2CWebPasswordFacebookId = "m_login_password";
        public const string B2CFacebookSubmitId = "u_0_5";
        public const string GoogleAccountId = "GoogleExchange";
        public const string WebUpnB2CGoogleInputId = "Email";
        public const string B2CWebPasswordGoogleId = "Passwd";
        public const string B2CGoogleNextId = "next";
        public const string B2CGoogleSignInId = "signIn";
        public const string B2CEditProfileContinueId = "continue";

        // these should match the product enum values
        public const string UiBehaviorConsent = "consent";
        public const string UiBehaviorSelectAccount = "select_account";
        public const string UiBehaviorLogin = "login";
        public const string UiBehaviorNoPrompt = "no_prompt";

        // Test Constants
        public const int ResultCheckPolliInterval = 1000;
        public const int MaximumResultCheckRetryAttempts = 20;
    }
}
