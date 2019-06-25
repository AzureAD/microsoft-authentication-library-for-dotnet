// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Xamarin.UITest.Queries;

namespace Microsoft.Identity.Test.UIAutomation
{
    public class AndroidTestController : XamarinUITestControllerBase
    {
        public AndroidTestController()
        {
            Platform = Xamarin.UITest.Platform.Android;
        }

        protected override void Tap(string elementID, XamarinSelector xamarinSelector, TimeSpan timeout)
        {
            switch (xamarinSelector)
            {
                case XamarinSelector.ByAutomationId:
                    try
                    {
                        WaitForElement(elementID, xamarinSelector, timeout);
                        Application.Tap(x => x.Marked(elementID));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine("Failed waiting for element. Attempting to tap regardless...");
                        Application.Tap(x => x.Marked(elementID));
                    }

                    break;
                case XamarinSelector.ByHtmlIdAttribute:
                    WaitForElement(elementID, xamarinSelector, timeout);
                    Application.Tap(QueryByCssId(elementID));
                    break;
                case XamarinSelector.ByHtmlValue:
                    WaitForElement(elementID, xamarinSelector, timeout);
                    Application.Tap(QueryByHtmlElementValue(elementID));
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
                    Application.EnterText(QueryByCssId(elementID), text);
                    break;
                case XamarinSelector.ByHtmlValue:
                    throw new InvalidOperationException("Test error - you can't input text in an html element that has a value");
                default:
                    throw new NotImplementedException("Invalid enum value " + xamarinSelector);
            }

            DismissKeyboard();
        }

        protected override Func<AppQuery, AppWebQuery> QueryByCssId(string elementID)
        {
            return c => c.Css(string.Format(CultureInfo.InvariantCulture, CssidSelector, elementID));
        }

        protected override Func<AppQuery, AppWebQuery> QueryByHtmlElementValue(string text)
        {
            string xpath = string.Format(CultureInfo.InvariantCulture, XpathSelector, text);
            return c => c.XPath(xpath);
        }
    }
}
