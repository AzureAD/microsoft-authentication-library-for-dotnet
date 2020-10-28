// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Callback delegate that allows application developers to consume logs, and handle them in a custom manner. This
    /// callback is set using <see cref="AbstractApplicationBuilder{T}.WithLogging(LogCallback, LogLevel?, bool?, bool?)"/>.
    /// If <c>PiiLoggingEnabled</c> is set to <c>true</c>, when registering the callback this method will receive the messages twice:
    /// once with the <c>containsPii</c> parameter equals <c>false</c> and the message without PII,
    /// and a second time with the <c>containsPii</c> parameter equals to <c>true</c> and the message might contain PII.
    /// In some cases (when the message does not contain PII), the message will be the same.
    /// For details see https://aka.ms/msal-net-logging
    /// </summary>
    /// <param name="level">Log level of the log message to process</param>
    /// <param name="message">Pre-formatted log message</param>
    /// <param name="containsPii">Indicates if the log message contains Organizational Identifiable Information (OII)    or Personally Identifiable Information (PII) nor not.</param>
    public delegate void LogCallback(LogLevel level, string message, bool containsPii);
}
