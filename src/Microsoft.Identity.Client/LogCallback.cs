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
    /// <param name="containsPii">Indicates if the log message contains Organizational Identifiable Information (OII)
    /// or Personally Identifiable Information (PII) nor not. 
    /// If <see cref="Logger.PiiLoggingEnabled"/> is set to <c>false</c> then this value is always false.
    /// Otherwise it will be <c>true</c> when the message contains PII.</param>
    /// <seealso cref="Logger"/>
    public delegate void LogCallback(LogLevel level, string message, bool containsPii);
}
