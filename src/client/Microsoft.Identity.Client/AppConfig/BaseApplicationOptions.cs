// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Identity.Client.Kerberos;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Base class for options objects with string values loadable from a configuration file
    /// (for instance a JSON file, as in an asp.net configuration scenario)
    /// See https://aka.ms/msal-net-application-configuration
    /// See also derived classes <see cref="ApplicationOptions"/>
    /// </summary>
    public abstract class BaseApplicationOptions
    {
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
        /// Flag to enable/disable logging to platform defaults. In Desktop, Event Tracing is used. In iOS, NSLog is used.
        /// In Android, logcat is used. The default value is <c>false</c>. See https://aka.ms/msal-net-logging
        /// </summary>
        /// <seealso cref="EnablePiiLogging"/>
        public bool IsDefaultPlatformLoggingEnabled { get; set; }
    }
}
