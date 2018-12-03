//----------------------------------------------------------------------
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
using Test.Microsoft.Identity.LabInfrastructure;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    public interface ITestController
    {
        /// <summary>
        /// Taps an element on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to tap</param>
        void Tap(string elementID);

        /// <summary>
        /// Taps an element on the UI
        /// </summary>
        /// <param name="textOrId">ID or text of the element to tap</param>
        /// <param name="xamarinSelector">Strategy for finding the element</param>
        void Tap(string textOrId, XamarinSelector xamarinSelector);

        /// <summary>
        /// Taps an element on the UI
        /// </summary>
        /// <param name="textOrId">ID or text of the element to tap</param>
        /// <param name="waitTime">The time in seconds in which the test controller will search for the element before it times out</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void Tap(string elementID, int waitTime, XamarinSelector xamarinSelector);

        /// <summary>
        /// enters text into a text field on the UI
        /// </summary>
        /// <param name="textOrId">ID or text of the element to tap</param>
        /// <param name="text">The text to be entered into the element</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void EnterText(string elementID, string text, XamarinSelector xamarinSelector);

        /// <summary>
        /// enters text into a text field on the UI
        /// </summary>
        /// <param name="textOrId">ID or text of the element to tap</param>
        /// <param name="waitTime">The time in seconds in which the test controller will search for the element before it times out</param>
        /// <param name="text">The text to be entered into the element</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void EnterText(string textOrId, int waitTime, string text, XamarinSelector xamarinSelector);

        /// <summary>
        /// Dismisses the keyboard. This should be called after each text input operation with the keyboard because the keyboard can stay active and hide
        /// buttons from the Ui Automation framework.
        /// </summary>
        void DismissKeyboard();

        /// <summary>
        /// Returns the text of an element visible on the UI
        /// </summary>
        /// <param name="textOrId">ID of the element to get text from</param>
        /// <returns>The text value of the element</returns>
        string GetText(string textOrId);

        /// <summary>
        /// Checks if a switch has changed state
        /// </summary>
        /// <param name="automationID">ID of the element to tap</param>
        void SetSwitchState(string automationID);

        IApp Application { get; set; }

        /// <summary>
        /// Waits for an html element to be present and returns it.
        /// </summary>
        /// <param name="resourceId">Id of the html element, exposed as "resource-id" by the UI Automation Viewer tool</param>
        /// <returns></returns>
        AppWebResult[] WaitForWebElementByCssId(string resourceId, TimeSpan? timeout = null);

        /// <summary>
        /// Waits for a native XAML widget element to be present and returns it.
        /// </summary>
        /// <param name="elementID">An automation id</param>
        /// <returns></returns>
        AppResult[] WaitForXamlElement(string elementID, TimeSpan? timeout = null);

        /// <summary>
        /// Waits for an HTML element to be present and returns it. Useful when the WebUI does not have elements with IDs.
        /// </summary>
        /// <param name="text">The text of the element, e.g. foo will find the element div in <div>foo</div>
        /// </param>
        AppWebResult[] WaitForWebElementByText(string text, TimeSpan? timeout = null);
    }
}
