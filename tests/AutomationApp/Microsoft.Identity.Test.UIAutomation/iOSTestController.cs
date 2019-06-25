// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Microsoft.Identity.Test.UIAutomation
{
    public class iOSTestController : XamarinUITestControllerBase
    {
        public iOSTestController()
        {
            Platform = Platform.iOS;
        }

        protected override void Tap(string elementID, XamarinSelector xamarinSelector, TimeSpan timeout)
        {
            WaitForElement(elementID, xamarinSelector, timeout);

            switch (xamarinSelector)
            {
                case XamarinSelector.ByAutomationId:
                    Application.Tap(x => x.Marked(elementID));
                    break;
                case XamarinSelector.ByHtmlIdAttribute:
                    Application.Query(InvokeTapByCssId(elementID));
                    break;
                case XamarinSelector.ByHtmlValue:
                    Application.Query(InvokeTapByHtmlElementValue(elementID));
                    break;
                default:
                    throw new NotImplementedException("Invalid enum value " + xamarinSelector);
            }
        }

        protected override void EnterText(string elementID, string text, XamarinSelector xamarinSelector, TimeSpan timeout)
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
                    Application.EnterText(QueryByHtmlElementValue(elementID), text);
                    break;
                case XamarinSelector.ByHtmlValue:
                    throw new InvalidOperationException("Test error - you can't input text in an html element that has a value");
                default:
                    throw new NotImplementedException("Invalid enum value " + xamarinSelector);
            }

            DismissKeyboard();
        }

        private static Func<AppQuery, InvokeJSAppQuery> InvokeTapByCssId(string elementID)
        {
            return c => c.Class("WKWebView").InvokeJS(string.Format(CultureInfo.InvariantCulture, "document.getElementById('{0}').click()", elementID));
        }

        private static Func<AppQuery, InvokeJSAppQuery> InvokeTapByHtmlElementValue(string elementID)
        {
            return c => c.Class("WKWebView").InvokeJS(string.Format(CultureInfo.InvariantCulture, "document.evaluate('//*[text()=\"{0}\"]', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue.click()", elementID));
        }

        protected override Func<AppQuery, AppWebQuery> QueryByCssId(string elementID)
        {
            return c => c.Class("WKWebView").Css(string.Format(CultureInfo.InvariantCulture, "#{0}", elementID));
        }

        protected override Func<AppQuery, AppWebQuery> QueryByHtmlElementValue(string elementID)
        {
            return c => c.Class("WKWebView").Css(string.Format(CultureInfo.InvariantCulture, "#{0}", elementID));
        }
    }
}
