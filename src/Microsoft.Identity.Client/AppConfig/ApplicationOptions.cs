// ------------------------------------------------------------------------------
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
// ------------------------------------------------------------------------------

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// Base class for options objects with string values loadable from a configuration file
    /// (for instance a JSON file, as in an asp.net configuration scenario)
    /// See https://aka.ms/msal-net-application-configuration
    /// See also derived classes <see cref="PublicClientApplicationOptions"/>
    /// and <see cref="ConfidentialClientApplicationOptions"/>
    /// </summary>
    public abstract class ApplicationOptions
    {
        /// <summary>
        /// Client ID (also known as App ID) of the application as registered in the
        /// application registration portal (https://aka.ms/msal-net-register-app)
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Tenant from which the application will allow users to sign it. This can be:
        /// a domain associated with a tenant, a guid (tenant id), or a meta-tenant (e.g. consumers).
        /// This property is mutually exclusive with <see cref="AadAuthorityAudience"/>. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        /// <remarks>The name of the property was chosen to ensure compatibility with AzureAdOptions
        /// in ASP.NET Core configuration files (even the semantics would be tenant)</remarks>
        public string TenantId { get; set; }

        /// <summary>
        /// Sign-in audience. This property is mutually exclusive with TenantId. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        public AadAuthorityAudience AadAuthorityAudience { get; set; } = AadAuthorityAudience.None;

        /// <summary>
        /// STS instance (for instance https://login.microsoftonline.com for the Azure public cloud).
        /// The name was chosen to ensure compatibility with AzureAdOptions in ASP.NET Core.
        /// This property is mutually exclusive with <see cref="AzureCloudInstance"/>. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        /// Specific instance in the case of Azure Active Directory.
        /// It allows users to use the enum instead of the explicit url.
        /// This property is mutually exclusive with <see cref="Instance"/>. If both
        /// are provided, an exception will be thrown.
        /// </summary>
        public AzureCloudInstance AzureCloudInstance { get; set; } = AzureCloudInstance.None;

        /// <summary>
        /// The redirect URI (also known as Reply URI or Reply URL), is the URI at which Azure AD will contact back the application with the tokens.
        /// This redirect URI needs to be registered in the app registration (https://aka.ms/msal-net-register-app).
        /// In MSAL.NET, <c>IPublicClientApplication</c> defines the following default RedirectUri values:
        /// <list type="bullet">
        /// <item><description><c>urn:ietf:wg:oauth:2.0:oob</c> for desktop (.NET Framework and .NET Core) applications</description></item>
        /// <item><description><c>msal{ClientId}</c> for Xamarin iOS and Xamarin Android without broker (as this will be used by the system web browser by default on these
        /// platforms to call back the application)
        /// </description></item>
        /// </list>
        /// These default URIs could change in the future. 
        /// 
        /// For Web Apps and Web APIs, the redirect URI can be the URL of the application
        /// 
        /// Finally for daemon applications (confidential client applications using only the Client Credential flow
        /// that is calling <c>AcquireTokenForClient</c>), no reply URI is needed.
        /// </summary>
        /// <remarks>This is especially important when you deploy an application that you have initially tested locally;
        /// you then need to add the reply URL of the deployed application in the application registration portal
        /// </remarks>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Enables you to configure the level of logging you want. The default value is <see cref="LogLevel.Info"/>. Setting it to <see cref="LogLevel.Error"/> will only get errors
        /// Setting it to <see cref="LogLevel.Warning"/> will get errors and warning, etc..
        /// See https://aka.ms/msal-net-logging
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// Flag to enable/disable logging of Personally Identifiable Information (PII).
        /// PII logs are never written to default outputs like Console, Logcat or NSLog
        /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR. You can set
        /// it to <c>true</c> for advanced debugging requiring PII. See https://aka.ms/msal-net-logging
        /// </summary>
        /// <seealso cref="IsDefaultPlatformLoggingEnabled"/>
        public bool EnablePiiLogging { get; set; }

        /// <summary>
        /// Flag to enable/disable logging to platform defaults. In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
        /// In Android, logcat is used. The default value is <c>false</c>. See https://aka.ms/msal-net-logging
        /// </summary>
        /// <seealso cref="EnablePiiLogging"/>
        public bool IsDefaultPlatformLoggingEnabled { get; set; }

        /// <summary>
        /// Identifier of the component (libraries/SDK) consuming MSAL.NET.
        /// This will allow for disambiguation between MSAL usage by the app vs MSAL usage by component libraries.
        /// </summary>
        public string Component { get; set; }
    }
}