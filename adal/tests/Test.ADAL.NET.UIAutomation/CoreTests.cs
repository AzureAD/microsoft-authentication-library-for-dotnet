using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Test.ADAL.NET.UIAutomation.Infrastructure;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Test.ADAL.NET.UIAutomation
{
	public class CoreMobileTests
	{
        public static void AcquireTokenTest(ITestController controller)
		{
            var user = controller.GetUser(
                new UserQueryParameters
                {
                    IsMamUser = false,
                    IsMfaUser = false,
                    IsFederatedUser = false
                });

            controller.Tap("secondPage");
            controller.Tap("acquireToken");
            controller.EnterText("i0116", user.Upn, true);
            controller.Tap("idSIButton9", true);
            controller.EnterText("i0118", ((LabUser)user).GetPassword(), true);
            controller.Tap("idSIButton9", true);

            Assert.IsTrue(controller.GetResultText("testResult") == "Success: True");
        }
	}
}
