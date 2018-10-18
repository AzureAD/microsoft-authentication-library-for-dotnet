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

using Test.Microsoft.Identity.LabInfrastructure;
using Xamarin.UITest;

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
        /// <param name="elementID">ID of the element to tap</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void Tap(string elementID, bool isWebElement);

        /// <summary>
        /// Taps an element on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to tap</param>
        /// <param name="waitTime">The time in seconds in which the test controller will search for the element before it times out</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void Tap(string elementID, int waitTime, bool isWebElement);

        /// <summary>
        /// enters text into a text field on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to enter text into</param>
        /// <param name="text">The text to be entered into the element</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void EnterText(string elementID, string text, bool isWebElement);

        /// <summary>
        /// enters text into a text field on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to enter text into</param>
        /// <param name="waitTime">The time in seconds in which the test controller will search for the element before it times out</param>
        /// <param name="text">The text to be entered into the element</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        void EnterText(string elementID, int waitTime, string text, bool isWebElement);

        /// <summary>
        /// Dismisses the keyboard. This should be called after each text input operation with the keyboard because the keyboard can stay active and hide
        /// buttons from the Ui Automation framework.
        /// </summary>
        void DismissKeyboard();

        /// <summary>
        /// Querys the Ui until an element is found on the UI
        /// </summary>
        /// <param name="automationID">ID of the element to look for</param>
        /// <param name="isWebElement">Set to true if the element to be tapped is within a web view</param>
        /// <returns>return details of the object that is being searched for.</returns>
        object[] WaitForElement(string automationID, bool isWebElement);

        /// <summary>
        /// Returns the text of an element wisible on the UI
        /// </summary>
        /// <param name="elementID">ID of the element to get text from</param>
        /// <returns>The text value of the element</returns>
        string GetText(string elementID);

        /// <summary>
        /// Returns a test user account for use in testing.
        /// An exception is thrown if no matching user is found.
        /// </summary>
        /// <param name="query">Any and all parameters that the returned user should satisfy.</param>
        /// <returns>A single user that matches the given query parameters.</returns>
        IUser GetUser(UserQueryParameters query);

        IApp Application { get; set; }
    }
}
