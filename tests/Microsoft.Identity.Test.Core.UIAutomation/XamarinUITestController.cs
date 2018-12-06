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
using System.Globalization;
using System.Linq;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Microsoft.Identity.Test.UIAutomation.infrastructure
{
    public enum XamarinSelector
    {
        ByAutomationId,
        ByHtmlIdAttribute,
        ByHtmlValue
    }

    public class XamarinUITestController : ITestController
    {
        TimeSpan defaultSearchTimeout;
        TimeSpan defaultRetryFrequency;
        TimeSpan defaultPostTimeout;
        const int defaultSearchTimeoutSec = 30;
        const int defaultRetryFrequencySec = 1;
        const int defaultPostTimeoutSec = 1;
        const string CSSIDSelector = "[id|={0}]";
        const string XpathSelector = "//*[text()=\"{0}\"]";

        public IApp Application { get; set; }

        public XamarinUITestController()
        {
            this.defaultSearchTimeout = new TimeSpan(0, 0, defaultSearchTimeoutSec);
            this.defaultRetryFrequency = new TimeSpan(0, 0, defaultRetryFrequencySec);
            this.defaultPostTimeout = new TimeSpan(0, 0, defaultPostTimeoutSec);
        }

        public void Tap(string elementID)
        {
            Tap(elementID, XamarinSelector.ByAutomationId, defaultSearchTimeout);
        }

        public void Tap(string elementID, XamarinSelector xamarinSelector)
        {
            Tap(elementID, xamarinSelector, defaultSearchTimeout);
        }

        public void Tap(string elementID, int waitTime, XamarinSelector xamarinSelector)
        {
            Tap(elementID, xamarinSelector, new TimeSpan(0, 0, waitTime));
        }

        public void EnterText(string elementID, string text, XamarinSelector xamarinSelector)
        {
            EnterText(elementID, text, xamarinSelector, defaultSearchTimeout);
        }

        public void EnterText(string elementID, int waitTime, string text, XamarinSelector xamarinSelector)
        {
            EnterText(elementID, text, xamarinSelector, new TimeSpan(0, 0, waitTime));
        }

        public AppWebResult[] WaitForWebElementByCssId(string elementID, TimeSpan? timeout = null)
        {

            if (timeout == null)
            {
                timeout = defaultSearchTimeout;
            }

            return Application.WaitForElement(
                QueryByCssId(elementID),
                "Timeout waiting for web element with css id: " + elementID,
                defaultSearchTimeout,
                defaultRetryFrequency,
                defaultPostTimeout);
        }

        /// <summary>
        /// Searches for an HTML element having a given text. CSS selectors are uanble to do this, 
        /// so an XPath strategy is needed.
        /// </summary>
        public AppWebResult[] WaitForWebElementByText(string text, TimeSpan? timeout = null)
        {

            if (timeout == null)
            {
                timeout = defaultSearchTimeout;
            }

            return Application.WaitForElement(
                QueryByHtmlElementValue(text),
                "Timeout waiting for web element with css id: " + text,
                defaultSearchTimeout,
                defaultRetryFrequency,
                defaultPostTimeout);
        }

        public AppResult[] WaitForXamlElement(string elementID, TimeSpan? timeout = null)
        {
            if (timeout == null)
            {
                timeout = defaultSearchTimeout;
            }

            return Application.WaitForElement(
                elementID,
                "Timeout waiting for xaml element with automation id: " + elementID,
                timeout,
                defaultRetryFrequency,
                defaultPostTimeout);
        }
        
        public object[] WaitForElement(string selector, XamarinSelector xamarinSelector, TimeSpan? timeout)
        {
            if (timeout == null)
            {
                timeout = defaultSearchTimeout;
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

        private void Tap(string elementID, XamarinSelector xamarinSelector, TimeSpan timeout)
        {
            WaitForElement(elementID, xamarinSelector, timeout);

            switch (xamarinSelector)
            {
                case XamarinSelector.ByAutomationId:
                    Application.Tap(x => x.Marked(elementID));
                    break;
                case XamarinSelector.ByHtmlIdAttribute:
                    Application.Tap(QueryByCssId(elementID));
                    break;
                case XamarinSelector.ByHtmlValue:
                    Application.Tap(QueryByHtmlElementValue(elementID));
                    break;
                default:
                    throw new NotImplementedException("Invalid enum value " + xamarinSelector);
            }
        }

        private void EnterText(string elementID, string text, XamarinSelector xamarinSelector, TimeSpan timeout)
        {
            WaitForElement(elementID, xamarinSelector, timeout);

            switch (xamarinSelector)
            {
                case XamarinSelector.ByAutomationId:
                    Application.Tap(x => x.Marked(elementID));
                    Application.ClearText();
                    Application.EnterText(x => x.Marked(elementID), text);
                    break;
                case XamarinSelector.ByHtmlIdAttribute:
                    Application.EnterText(QueryByCssId(elementID), text);
                    break;
                case XamarinSelector.ByHtmlValue:
                    throw new InvalidOperationException("Test error - you can't input text in an html element that has a value");
                default:
                    throw new NotImplementedException("Invalid enum value " + xamarinSelector);
            }

            DismissKeyboard();
        }
       
        public void DismissKeyboard()
        {
            Application.DismissKeyboard();
        }

        public string GetText(string elementID)
        {
            Application.WaitForElement(elementID, "Could not find element", defaultSearchTimeout, defaultRetryFrequency, defaultPostTimeout);
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

        private static Func<AppQuery, AppWebQuery> QueryByHtmlElementValue(string text)
        {
            string xpath = String.Format(CultureInfo.InvariantCulture, XpathSelector, text);
            return c => c.XPath(xpath);
        }

        private static Func<AppQuery, AppWebQuery> QueryByCssId(string elementID)
        {
            return c => c.Css(String.Format(CultureInfo.InvariantCulture, CSSIDSelector, elementID));
        }

    }
}
