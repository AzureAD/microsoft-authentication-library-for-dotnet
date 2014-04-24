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

namespace Test.ADAL.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Automation;
    using Test.ADAL.WinRT;

    public enum PageType
    {
        Dashboard,
        DashboardResponse,
        Wab,
        WabError,
        Unknown
    }

    internal class AuthenticationContextProxy
    {
        private static readonly CommandProxy CommandProxy = new CommandProxy();
        private static AutomationElement appWindow;
        private static AutomationElement executeButton;
        private static AutomationElement parametersTextBox;
        private static AutomationElement resultTextBox;
        private static AutomationElement signInButton;
        private static AutomationElement userIdTextBox;
        private static AutomationElement passwordTextBox;
        private static AutomationElement upButton;

        private static string userName;
        private static string password;

        public AuthenticationContextProxy(string authority)
        {
            CommandProxy.AddCommand(new AuthenticationContextCommand(
                CommandType.CreateContextA,
                new CommandArguments { Authority = authority }));
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority)
        {
            CommandProxy.AddCommand(new AuthenticationContextCommand(
                CommandType.CreateContextAV,
                new CommandArguments { Authority = authority, ValidateAuthority = validateAuthority }));
        }

        public AuthenticationContextProxy(string authority, bool validateAuthority, TokenCacheStoreType tokenCacheStoreType)
        {
            CommandProxy.AddCommand(new AuthenticationContextCommand(
                CommandType.CreateContextAVC,
                new CommandArguments { Authority = authority, ValidateAuthority = validateAuthority, TokenCacheStoreType = tokenCacheStoreType }));
        }

        public static void InitializeTest()
        {
            CommandProxy.Commands.Clear();
            ClearDefaultCache();
        }

        public static void ClearDefaultCache()
        {
            CommandProxy.AddCommand(new AuthenticationContextCommand(CommandType.ClearDefaultTokenCache, null));
        }

        public static void SetEnvironmentVariable(string environmentVariable, string environmentVariableValue)
        {
            CommandProxy.AddCommand(new AuthenticationContextCommand(
                CommandType.SetEnvironmentVariable,
                new CommandArguments { EnvironmentVariable = environmentVariable, EnvironmentVariableValue = environmentVariableValue }));
        }

        public static void SetCredentials(string userNameIn, string passwordIn)
        {
            userName = userNameIn;
            password = passwordIn;
        }

        public static void Delay(int sleepMilliSeconds)
        {
            Task.Run(() => Thread.Sleep(sleepMilliSeconds)).Wait();
        }

        public void SetCorrelationId(Guid correlationId)
        {
            CommandProxy.AddCommand(new AuthenticationContextCommand(
                CommandType.SetCorrelationId,
                new CommandArguments { CorrelationId = correlationId }));
        }

        public async Task<AuthenticationResultProxy> SetDefaultToSSOMode(bool defaultToSSOMode)
        {
            return await AddCommandAndRunAsync(defaultToSSOMode ? CommandType.SetDefaultToSSOMode : CommandType.ClearDefaultToSSOMode, null);
        }

        public async Task<AuthenticationResultProxy> SetUseCorporateNetwork(bool useCorporateNetwork)
        {
            return await AddCommandAndRunAsync(useCorporateNetwork ? CommandType.SetUseCorporateNetwork : CommandType.ClearUseCorporateNetwork, null);
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, X509CertificateCredentialProxy credential)
        {
            throw new NotImplementedException("ADAL WinRT does not support X509 Certificate Credential");
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, ClientAssertionProxy credential)
        {
            throw new NotImplementedException("ADAL WinRT does not support Assertion Certificate Credential");
        }

        public async Task<AuthenticationResultProxy> AcquireTokenAsync(string resource, string clientId, UserCredentialProxy credential)
        {
            return await AddCommandAndRunAsync(
                CommandType.AquireTokenAsyncRCUP,
                new CommandArguments { Resource = resource, ClientId = clientId, UserId = credential.UserId, Password = credential.Password });
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId)
        {
            return RunAsyncTask(AddCommandAndRunAsync(CommandType.AquireTokenAsyncRC, new CommandArguments { Resource = resource, ClientId = clientId }));
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri)
        {
            return RunAsyncTask(AddCommandAndRunAsync(
                CommandType.AquireTokenAsyncRCR,
                new CommandArguments { Resource = resource, ClientId = clientId, RedirectUri = redirectUri }));
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, string userId)
        {
            return RunAsyncTask(AddCommandAndRunAsync(
                CommandType.AquireTokenAsyncRCRU,
                new CommandArguments { Resource = resource, ClientId = clientId, RedirectUri = redirectUri, UserId = userId }));
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, string userId, string extraQueryParameters)
        {
            return RunAsyncTask(AddCommandAndRunAsync(
                CommandType.AquireTokenAsyncRCRUX,
                new CommandArguments { Resource = resource, ClientId = clientId, RedirectUri = redirectUri, UserId = userId, Extra = extraQueryParameters }));
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehavior)
        {
            return RunAsyncTask(AddCommandAndRunAsync(
                CommandType.AquireTokenAsyncRCRP,
                new CommandArguments { Resource = resource, ClientId = clientId, RedirectUri = redirectUri, PromptBehavior = promptBehavior }));
        }

        public AuthenticationResultProxy AcquireToken(string resource, string clientId, Uri redirectUri, PromptBehaviorProxy promptBehavior, string userId)
        {
            return RunAsyncTask(AddCommandAndRunAsync(
                CommandType.AquireTokenAsyncRCRPU,
                new CommandArguments { Resource = resource, ClientId = clientId, RedirectUri = redirectUri, PromptBehavior = promptBehavior, UserId = userId }));
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId)
        {
            return await AddCommandAndRunAsync(
                CommandType.AcquireTokenByRefreshTokenAsyncRC,
                new CommandArguments { RefreshToken = refreshToken, ClientId = clientId });
        }

        public async Task<AuthenticationResultProxy> AcquireTokenByRefreshTokenAsync(string refreshToken, string clientId, string resource)
        {
            return await AddCommandAndRunAsync(
                CommandType.AcquireTokenByRefreshTokenAsyncRCR,
                new CommandArguments { RefreshToken = refreshToken, ClientId = clientId, Resource = resource });
        }

        private static AuthenticationResultProxy RunAsyncTask(Task<AuthenticationResultProxy> task)
        {
            try
            {
                task.Wait();
                return task.Result;
            }
            catch (AggregateException ae)
            {
                throw ae.InnerExceptions[0];
            }
        }

        private static bool FindDashboardApp()
        {
            AutomationElement desktop = AutomationElement.RootElement;
            appWindow = desktop.FindFirst(
                TreeScope.Children,
                new PropertyCondition(
                    AutomationElement.NameProperty,
                    "ADAL WinRT Dashboard Client",
                    PropertyConditionFlags.IgnoreCase));

            return (appWindow != null);
        }

        private static async Task<PageType> WaitForPage(List<PageType> pageType, int maxSecondsToWait)
        {
            const int SleepMilliSeconds = 500;
            int count = maxSecondsToWait * 1000 / SleepMilliSeconds;
            bool ready = false;
            PageType actualPageType;
            do
            {
                actualPageType = await DetectPageTypeAsync();
                if (!pageType.Contains(actualPageType))
                {
                    Delay(SleepMilliSeconds);
                    count--;
                }
                else
                {
                    ready = true;
                }
            }
            while (!ready && count > 0);
            return actualPageType;
        }

        private static async Task<PageType> DetectPageTypeAsync()
        {
            Delay(100);

            PageType pageType = PageType.Unknown;
            if (FindAutomationElement("Popup Window") != null)
            {
                upButton = FindAutomationElement("UpButton");
                if (FindAutomationElement("service_exception_message") != null)
                {
                    pageType = PageType.WabError;
                }
                else if (FindErrorElement() != null)
                {
                    pageType = PageType.WabError;
                }
                else
                {
                    var title = FindAutomationElement("TitleBar");
                    if (title != null)
                    {
                        if (title.Current.Name.Contains("Can't connect to the service"))
                        {
                            return PageType.WabError;
                        }
                    }

                    pageType = PageType.Wab;
                    userIdTextBox = FindAutomationElement("cred_userid_inputtext");
                    if (userIdTextBox == null)
                    {
                        userIdTextBox = FindAutomationElement("userNameInput");
                    }

                    passwordTextBox = FindAutomationElement("cred_password_inputtext");
                    if (passwordTextBox == null)
                    {
                        passwordTextBox = FindAutomationElement("passwordInput");                        
                    }

                    signInButton = FindAutomationElement("cred_sign_in_button");
                    if (signInButton == null)
                    {
                        signInButton = FindAutomationElement("submitButton");
                    }

                    if (userIdTextBox == null || passwordTextBox == null || signInButton == null)
                    {
                        return PageType.Unknown;
                    }
                }
            }
            else
            {
                parametersTextBox = FindAutomationElement("Parameters");
                executeButton = FindAutomationElement("Execute");
                resultTextBox = FindAutomationElement("Result");
                if (parametersTextBox == null || executeButton == null || resultTextBox == null)
                {
                    return PageType.Unknown;
                }

                pageType = (string.IsNullOrWhiteSpace(ReadValue(resultTextBox)))
                    ? PageType.Dashboard
                    : PageType.DashboardResponse;
            }

            return pageType;
        }

        private static async Task<AutomationElement> FindAutomationElementWithRetryAsync(
            string id,
            int maxSecondsToWait)
        {
            const int SleepMilliSeconds = 100;
            int count = maxSecondsToWait * 1000 / SleepMilliSeconds;
            AutomationElement element = null;
            do
            {
                element = appWindow.FindFirst(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, id, PropertyConditionFlags.IgnoreCase));
                if (element == null)
                {
                    Delay(SleepMilliSeconds);
                    count--;
                }
            }
            while (element == null && count > 0);

            return element;
        }

        private static AutomationElement FindAutomationElement(string id)
        {
            return appWindow.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, id, PropertyConditionFlags.IgnoreCase));
        }

        private static AutomationElement FindErrorElement()
        {
            var allDescendents = appWindow.FindAll(TreeScope.Descendants, PropertyCondition.TrueCondition);
            foreach (AutomationElement element in allDescendents)
            {
                if (element.Current.Name.Contains("error occurred"))
                {
                    return element;
                }
            }

            return null;
        }

        private static void SendParameters(string parameters)
        {
            SetTextBoxValue(parametersTextBox, parameters);
            SetTextBoxValue(resultTextBox, string.Empty);
        }

        private static void Execute()
        {
            ClickXamlButton(executeButton);
        }

        private static async Task EnterCredentialAsync(string userId, string password)
        {
            SetTextBoxValue(userIdTextBox, userId);
            SetTextBoxValue(passwordTextBox, password);

            // If userId is entered, submit button needs to be clicked twice. 
            await ClickBrowserButtonAsync(signInButton, (userId != null));
        }

        private static string ReadValue(AutomationElement element)
        {
            var valuePattern = (ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern);
            return valuePattern.Current.Value;
        }

        private static void SetTextBoxValue(AutomationElement textBox, string value)
        {
            if (value != null)
            {
                var valuePattern = (ValuePattern)textBox.GetCurrentPattern(ValuePattern.Pattern);
                valuePattern.SetValue(value);
            }
        }

        private static void ClickXamlButton(AutomationElement button)
        {
            var invokePattern = (InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern);
            invokePattern.Invoke();
        }

        private static async Task ClickBrowserButtonAsync(AutomationElement button, bool clickTwice = false)
        {
            MouseHelper.MoveTo(button);
            MouseHelper.LeftClick();

            if (clickTwice)
            {
                Delay(200);
                MouseHelper.LeftClick();
            }
        }

        internal static async Task<AuthenticationResultProxy> AddCommandAndRunAsync(
            CommandType commandType,
            CommandArguments commandArguments)
        {
            CommandProxy.AddCommand(new AuthenticationContextCommand(commandType, commandArguments));
            return await RunCommandsAsync();
        }

        private static async Task<AuthenticationResultProxy> RunCommandsAsync()
        {
            FindDashboardApp();
            PageType pageType =
                await
                    WaitForPage(
                        new List<PageType>
                        {
                            PageType.Wab,
                            PageType.WabError,
                            PageType.Dashboard,
                            PageType.DashboardResponse
                        },
                        5);
            Delay(500);
            if (pageType == PageType.Wab || pageType == PageType.WabError)
            {
                ClickXamlButton(upButton);
                await WaitForPage(new List<PageType> { PageType.DashboardResponse }, 10);
            }

            Delay(500);
            SendParameters(CommandProxy.Serialize());
            Execute();
            Delay(500);
            pageType =
                await WaitForPage(new List<PageType> { PageType.Wab, PageType.WabError, PageType.DashboardResponse }, 5);
            if (pageType == PageType.Wab)
            {
                await EnterCredentialAsync(userName, password);
            }

            Delay(500);
            pageType = await WaitForPage(new List<PageType> { PageType.WabError, PageType.DashboardResponse }, 10);
            if (pageType == PageType.WabError)
            {
                ClickXamlButton(upButton);
                await WaitForPage(new List<PageType> { PageType.DashboardResponse }, 10);
            }

            Delay(500);
            CommandProxy.Commands.Clear();

            return AuthenticationResultProxy.Deserialize(ReadValue(resultTextBox));
        }
    }
}