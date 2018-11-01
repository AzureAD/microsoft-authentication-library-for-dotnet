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
using NUnit.Framework;
using System;
using System.Linq;
using Xamarin.UITest;
using System.Globalization;

namespace Test.Microsoft.Identity.Core.UIAutomation
{
    public class XamarinUITestController : ITestController
    {
        TimeSpan defaultSearchTimeout;
        TimeSpan defaultRetryFrequency;
        TimeSpan defaultPostTimeout;
        const int defaultSearchTimeoutSec = 30;
        const int defaultRetryFrequencySec = 1;
        const int defaultPostTimeoutSec = 1;
        const string CSSIDSelector = "[id|={0}]";
        private ILabService _labService;

        public IApp Application { get; set; }

        public XamarinUITestController()
        {
            this.defaultSearchTimeout = new TimeSpan(0, 0, defaultSearchTimeoutSec);
            this.defaultRetryFrequency = new TimeSpan(0, 0, defaultRetryFrequencySec);
            this.defaultPostTimeout = new TimeSpan(0, 0, defaultPostTimeoutSec);
            _labService = new LabServiceApi(new KeyVaultSecretsProvider());
        }

        public void Tap(string elementID)
        {
            Tap(elementID, false, defaultSearchTimeout);
        }

        public void Tap(string elementID, bool isWebElement)
        {
            Tap(elementID, isWebElement, defaultSearchTimeout);
        }

        public void Tap(string elementID, int waitTime, bool isWebElement)
        {
            Tap(elementID, isWebElement, new TimeSpan(0, 0, waitTime));
        }

        public void EnterText(string elementID, string text, bool isWebElement)
        {
            EnterText(elementID, text, isWebElement, defaultSearchTimeout);
        }

        public void EnterText(string elementID, int waitTime, string text, bool isWebElement)
        {
            EnterText(elementID, text, isWebElement, new TimeSpan(0, 0, waitTime));
        }

        public object[] WaitForElement(string elementID, bool isWebElement)
        {
            if (isWebElement)
            {
                return Application.WaitForElement(c => c.Css(String.Format(CultureInfo.InvariantCulture, CSSIDSelector, elementID)), "Could not find element", defaultSearchTimeout, defaultRetryFrequency, defaultPostTimeout);
            }
            else
            {
                return Application.WaitForElement(elementID, "Could not find element", defaultSearchTimeout, defaultRetryFrequency, defaultPostTimeout);
            }
        }

        private void Tap(string elementID, bool isWebElement, TimeSpan timeout)
        {
            if (isWebElement)
            {
                Application.WaitForElement(c => c.Css(String.Format(CultureInfo.InvariantCulture, CSSIDSelector, elementID)), "Could not find element", timeout, defaultRetryFrequency, defaultPostTimeout);
                Application.Tap(c => c.Css(String.Format(CultureInfo.InvariantCulture, CSSIDSelector, elementID)));
            }
            else
            {
                Application.WaitForElement(elementID, "Could not find element", timeout, defaultRetryFrequency, defaultPostTimeout);
                Application.Tap(x => x.Marked(elementID));
            }
        }

        private void EnterText(string elementID, string text, bool isWebElement, TimeSpan timeout)
        {
            if (isWebElement)
            {
                Application.WaitForElement(c => c.Css(String.Format(CultureInfo.InvariantCulture, CSSIDSelector, elementID)), "Could not find element", timeout, defaultRetryFrequency, defaultPostTimeout);
                Application.EnterText(c => c.Css(String.Format(CultureInfo.InvariantCulture, CSSIDSelector, elementID)), text);
            }
            else
            {
                Application.WaitForElement(elementID, "Could not find element", timeout, defaultRetryFrequency, defaultPostTimeout);
                Application.Tap(x => x.Marked(elementID));
                Application.ClearText(); 
                Application.EnterText(x => x.Marked(elementID), text);
            }
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

        public IUser GetUser(UserQueryParameters query)
        {
            var user = _labService.GetUser(query);
            Assert.True(user != null, "Found no users for the given query.");
            return user;
        }
    }
}
