using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.UI;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.WebUITests
{
    [TestClass]
    public class AuthorizationResultTests
    {
        [TestMethod]
        public void MyTestMethod()
        {
            Uri uri = new Uri("http://some_url.com?q=p");
            UriBuilder errorUri = new UriBuilder(TestConstants.AuthorityHomeTenant)
            {
                Query = string.Format(
                           CultureInfo.InvariantCulture,
                           "error={0}&error_description={1}",
                           MsalError.NonHttpsRedirectNotSupported,
                           MsalErrorMessage.NonHttpsRedirectNotSupported + " - " + CoreHelpers.UrlEncode(uri.AbsoluteUri))
            };

            var result = AuthorizationResult.FromUri(errorUri.Uri.AbsoluteUri);
            Assert.AreEqual(MsalErrorMessage.NonHttpsRedirectNotSupported + " - " + uri.AbsoluteUri, result.ErrorDescription);
            Assert.AreEqual(MsalError.NonHttpsRedirectNotSupported, result.Error);
        }
    }
}
