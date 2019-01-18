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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Microsoft.Identity.Test.UIAutomation.Infrastructure
{
    public enum XamarinSelector
    {
        ByAutomationId,
        ByHtmlIdAttribute,
        ByHtmlValue
    }

    public abstract class XamarinUiTestControllerBase : ITestController
    {
        protected readonly TimeSpan _defaultSearchTimeout;
        protected readonly TimeSpan _defaultRetryFrequency;
        protected readonly TimeSpan _defaultPostTimeout;
        protected const int DefaultSearchTimeoutSec = 30;
        protected const int DefaultRetryFrequencySec = 1;
        protected const int DefaultPostTimeoutSec = 1;
        protected const string CssidSelector = "[id|={0}]";
        protected const string XpathSelector = "//*[text()=\"{0}\"]";

        public IApp Application { get; set; }

        public Platform Platform { get; set; }

        protected XamarinUiTestControllerBase()
        {
            _defaultSearchTimeout = new TimeSpan(0, 0, DefaultSearchTimeoutSec);
            _defaultRetryFrequency = new TimeSpan(0, 0, DefaultRetryFrequencySec);
            _defaultPostTimeout = new TimeSpan(0, 0, DefaultPostTimeoutSec);
        }

        public void Tap(string elementID)
        {
            Tap(elementID, XamarinSelector.ByAutomationId, _defaultSearchTimeout);
        }

        public void Tap(string elementID, XamarinSelector xamarinSelector)
        {
            Tap(elementID, xamarinSelector, _defaultSearchTimeout);
        }

        public void Tap(string elementID, int waitTime, XamarinSelector xamarinSelector)
        {
            Tap(elementID, xamarinSelector, new TimeSpan(0, 0, waitTime));
        }

        protected abstract void Tap(string elementID, XamarinSelector xamarinSelector, TimeSpan timeout);

        public void EnterText(string elementID, string text, XamarinSelector xamarinSelector)
        {
            EnterText(elementID, text, xamarinSelector, _defaultSearchTimeout);
        }

        public void EnterText(string elementID, int waitTime, string text, XamarinSelector xamarinSelector)
        {
            EnterText(elementID, text, xamarinSelector, new TimeSpan(0, 0, waitTime));
        }

        protected abstract void EnterText(string elementID, string text, XamarinSelector xamarinSelector, TimeSpan timeout);

        public AppWebResult[] WaitForWebElementByCssId(string elementID, TimeSpan? timeout = null)
        {

            if (timeout == null)
            {
                timeout = _defaultSearchTimeout;
            }

            return Application.WaitForElement(
                QueryByCssId(elementID),
                "Timeout waiting for web element with css id: " + elementID,
                _defaultSearchTimeout,
                _defaultRetryFrequency,
                _defaultPostTimeout);
        }

        /// <summary>
        /// Searches for an HTML element having a given text. CSS selectors are uanble to do this,
        /// so an XPath strategy is needed.
        /// </summary>
        public AppWebResult[] WaitForWebElementByText(string text, TimeSpan? timeout = null)
        {

            if (timeout == null)
            {
                timeout = _defaultSearchTimeout;
            }

            return Application.WaitForElement(
                QueryByHtmlElementValue(text),
                "Timeout waiting for web element with css id: " + text,
                _defaultSearchTimeout,
                _defaultRetryFrequency,
                _defaultPostTimeout);
        }

        public AppResult[] WaitForXamlElement(string elementID, TimeSpan? timeout = null)
        {
            if (timeout == null)
            {
                timeout = _defaultSearchTimeout;
            }

            return Application.WaitForElement(
                elementID,
                "Timeout waiting for xaml element with automation id: " + elementID,
                timeout,
                _defaultRetryFrequency,
                _defaultPostTimeout);
        }

        public object[] WaitForElement(string selector, XamarinSelector xamarinSelector, TimeSpan? timeout)
        {
            if (timeout == null)
            {
                timeout = _defaultSearchTimeout;
            }

            switch (xamarinSelector)
            {
                case XamarinSelector.ByAutomationId:
                    return WaitForXamlElement(selector, timeout);
                case XamarinSelector.ByHtmlIdAttribute:
                    return WaitForWebElementByCssId(selector, timeout);
                case XamarinSelector.ByHtmlValue:
                    return WaitForWebElementByText(selector, timeout);
                default:
                    throw new NotImplementedException("Invalid enum value " + xamarinSelector);
            }
        }

        public void DismissKeyboard()
        {
            Application.DismissKeyboard();
        }

        public string GetText(string elementID)
        {
            Application.WaitForElement(elementID, "Could not find element", _defaultSearchTimeout, _defaultRetryFrequency, _defaultPostTimeout);
            return Application.Query(x => x.Marked(elementID)).FirstOrDefault().Text;
        }

        /// <summary>
        /// Checks if a switch has changed state
        /// </summary>
        /// <param name="automationID"></param>
        public void SetSwitchState(string automationID)
        {
            if (Application.Query(c => c.Marked(automationID).Invoke("isChecked").Value<bool>()).First() == false)
            {
                Tap(automationID);
                Application.WaitFor(() =>
                {
                    return Application.Query(c => c.Marked(automationID).Invoke("isChecked").Value<bool>()).First() == true;
                });
            }
        }

        protected abstract Func<AppQuery, AppWebQuery> QueryByCssId(string elementID);

        protected abstract Func<AppQuery, AppWebQuery> QueryByHtmlElementValue(string text);
    }
}