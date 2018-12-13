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

using System;

namespace Microsoft.Identity.Client
{
    // TODO: update documentation...
    ///// <summary>
    ///// Callback delegate that allows application developers to consume logs, and handle them in a custom manner. This
    ///// callback is set on the <see cref="Logger.LogCallback"/> member of the <see cref="Logger"/> static class.
    ///// If <see cref="Logger.PiiLoggingEnabled"/> is set to <c>true</c>, this method will receive the messages twice: 
    ///// once with the <c>containsPii</c> parameter equals <c>false</c> and the message without PII, 
    ///// and a second time with the <c>containsPii</c> parameter equals to <c>true</c> and the message might contain PII. 
    ///// In some cases (when the message does not contain PII), the message will be the same.
    ///// For details see https://aka.ms/msal-net-logging
    ///// </summary>
    ///// <param name="level">Log level of the log message to process</param>
    ///// <param name="message">Pre-formatted log message</param>
    ///// <param name="containsPii">Indicates if the log message contains Organizational Identifiable Information (OII)
    ///// or Personally Identifiable Information (PII) nor not. 
    ///// If <see cref="Logger.PiiLoggingEnabled"/> is set to <c>false</c> then this value is always false.
    ///// Otherwise it will be <c>true</c> when the message contains PII.</param>
    ///// <seealso cref="Logger"/>
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <param name="message"></param>
    /// <param name="containsPii"></param>
    public delegate void LogCallback(LogLevel level, string message, bool containsPii);

    /// <summary>
    /// Level of the log messages.
    /// For details see https://aka.ms/msal-net-logging
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Error Log level
        /// </summary>
        Error = 0,

        /// <summary>
        /// Warning Log level
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Information Log level
        /// </summary>
        Info = 2,

        /// <summary>
        /// Verbose Log level
        /// </summary>
        Verbose = 3
    }

    // TODO: CAN WE DEPRECATE THIS NOW?
    ///// <summary>
    ///// Static class that allows application developers to set a callback to handle logs, specify the level
    ///// of logs desired and if they accept to log Personally Identifiable Information (PII) or not
    ///// </summary>
    ///// <example>
    ///// <code>
    ///// private static void Log(LogLevel level, string message, bool containsPii)
    ///// {
    /////  if (containsPii)
    /////  {
    /////   Console.ForegroundColor = ConsoleColor.Red;
    /////  }
    /////   Console.WriteLine($"{level} {message}");
    /////   Console.ResetColor();
    /////  }
    /////
    ///// private async Task CallProtectedApiWithLoggingAsync(string[] args)
    ///// {
    /////  PublicClientApplication application = new PublicClientApplication(clientID);
    /////  Logger.LogCallback = Log;
    /////  Logger.Level = LogLevel.Info;
    /////  Logger.PiiLoggingEnabled = true;
    /////  AuthenticationResult result = await application.AcquireTokenAsync(
    /////                                             new string[] { "User.Read" });
    /////  ...
    ///// }
    ///// </code>
    ///// </example>
    //public sealed class Logger
    //{
    //    internal static readonly object LockObj = new object();

    //    private static volatile LogCallback _logCallback;
    //    /// <summary>
    //    /// Callback instance that you can set in your app to consume and publish logs in a custom manner. 
    //    /// If <see cref="Logger.PiiLoggingEnabled"/> is set to <c>true</c>, this method will receive the messages twice: 
    //    /// once with the <c>containsPii</c> parameter equals <c>false</c> and the message without PII, 
    //    /// and a second time with the <c>containsPii</c> parameter equals to <c>true</c> and the message might contain PII. 
    //    /// In some cases (when the message does not contain PII), the message will be the same.
    //    /// <para/>
    //    /// For details see https://aka.ms/msal-net-logging
    //    /// </summary>
    //    /// <exception cref="ArgumentException">will be thrown if the LogCallback was already set</exception>
    //    public static LogCallback LogCallback
    //    {
    //        set
    //        {
    //            lock (LockObj)
    //            {
    //                _logCallback = value;
    //            }
    //        }

    //        internal get
    //        {
    //            lock (LockObj)
    //            {
    //                return _logCallback;
    //            }
    //        }
    //    }

    //    /// <summary>
    //    /// Enables you to configure the level of logging you want. The default value is <see cref="LogLevel.Info"/>. Setting it to <see cref="LogLevel.Error"/> will only get errors
    //    /// Setting it to <see cref="LogLevel.Warning"/> will get errors and warning, etc..
    //    /// </summary>
    //    public static LogLevel Level { get; set; } = LogLevel.Info;

    //    /// <summary>
    //    /// Flag to enable/disable logging of Personally Identifiable data (PII) data. 
    //    /// PII logs are never written to default outputs like Console, Logcat or NSLog
    //    /// Default is set to <c>false</c>, which ensures that your application is compliant with GDPR. You can set
    //    /// it to <c>true</c> for advanced debugging requiring PII
    //    /// </summary>
    //    /// <seealso cref="DefaultLoggingEnabled"/>
    //    public static bool PiiLoggingEnabled { get; set; } = false;

    //    /// <summary>
    //    /// Flag to enable/disable logging to platform defaults. In Desktop/UWP, Event Tracing is used. In iOS, NSLog is used.
    //    /// In android, logcat is used. The default value is <c>false</c>
    //    /// </summary>
    //    public static bool DefaultLoggingEnabled { get; set; } = false;
    //}
}