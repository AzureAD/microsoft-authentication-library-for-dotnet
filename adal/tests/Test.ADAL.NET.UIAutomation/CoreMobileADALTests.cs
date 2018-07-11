using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Test.Microsoft.Identity.Core.UIAutomation.infrastructure;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Test.ADAL.NET.UIAutomation
{
    /// <summary>
    /// Contains the core test functionality that will be used by Android and iOS tests
    /// </summary>
	public class CoreMobileADALTests
    {
        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        /// <param name="controller">The test framework that will execute the test interaction</param>
        public static void AcquireTokenTest(ITestController controller)
		{
            //Get User from Lab
            var user = controller.GetUser(
                new UserQueryParameters
                {
                    IsMamUser = false,
                    IsMfaUser = false,
                    IsFederatedUser = false
                });

            controller.Tap("secondPage");

            //Clear Cache
            controller.Tap("clearCache");

            //Acquire token flow
            controller.Tap("acquireToken");
            //i0116 = UPN text field on AAD sign in endpoint
            controller.EnterText("i0116", user.Upn, true);
            //idSIButton9 = Sign in button
            controller.Tap("idSIButton9", true);
            //i0118 = password text field
            controller.EnterText("i0118", ((LabUser)user).GetPassword(), true);
            controller.Tap("idSIButton9", true);

            //Verify result. Test results are put into a label
            Assert.IsTrue(controller.GetText("testResult") == "Success: True");
        }
	}
}
