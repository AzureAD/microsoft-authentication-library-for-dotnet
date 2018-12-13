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

using System.Collections.Generic;
using Microsoft.Identity.Client.TelemetryCore;

namespace Microsoft.Identity.Client
{
    // TODO: obsolete this entire class.  Developer needs to implement ITelemetryReceiver and send it into the config builder to get telemetry events now.
   // /// <summary>
   // ///     Handler enabling your application to send telemetry to your telemetry service or subscription (for instance
   // ///     Microsoft Application Insights).
   // ///     To enable telemetry in your application, you get the singleton instance of <c>Telemetry</c> by using
   // ///     <see cref="Telemetry.GetInstance()" />, you set the delegate that will
   // ///     process the telemetry events by calling <see cref="RegisterReceiver(Telemetry.Receiver)" />, and you decide if you
   // ///     want to receive telemetry
   // ///     events only in case of failure or all the time, by setting the <see cref="TelemetryOnFailureOnly" /> boolean.
   // /// </summary>
   // public class Telemetry : ITelemetryReceiver
   // {
   //     /// <summary>
   //     ///     Delegate telling the signature of your callbacks that will send telemetry information to your telemetry service
   //     /// </summary>
   //     /// <param name="events">Dictionary of key/values pair</param>
   //     public delegate void Receiver(List<Dictionary<string, string>> events);

   //     private static readonly Telemetry Instance = new Telemetry();
   //     private Receiver _receiver;

   //     // This is an internal constructor to build isolated unit test instance
   //     internal Telemetry()
   //     {
   //     }

   //     /// <summary>
   //     ///     Get the instance of the Telemetry helper for MSAL.NET
   //     /// </summary>
   //     /// <returns>a unique instance of <see cref="Telemetry" /></returns>
   //     public static Telemetry GetInstance()
   //     {
   //         return Instance;
   //     }

   //     /// <summary>
   //     ///     Gets or sets a boolean that indicates if telemetry should be generated on failures only (<c>true</c>) or
   //     ///     all the time (<c>false</c>)
   //     /// </summary>
   //     public bool TelemetryOnFailureOnly { get; set; }

   //     /// <summary>
   //     ///     Registers one delegate that will send telemetry information to your telemetry service
   //     /// </summary>
   //     /// <param name="r">Receiver delegate. See <see cref="Receiver" /></param>
   //     public void RegisterReceiver(Receiver r)
   //     {
   //         _receiver = r;
   //     }

   //     void ITelemetryReceiver.HandleTelemetryEvents(List<Dictionary<string, string>> events)
   //     {
   //         _receiver?.Invoke(events);
   //     }

   //     bool ITelemetryReceiver.OnlySendFailureTelemetry
   //     {
   //         get => TelemetryOnFailureOnly;
   //         set => TelemetryOnFailureOnly = value;
   //     }
   //}
}