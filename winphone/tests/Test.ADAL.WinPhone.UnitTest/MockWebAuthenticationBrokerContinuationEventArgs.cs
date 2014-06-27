using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Collections;
using Windows.Security.Authentication.Web;

namespace Test.ADAL.WinPhone.UnitTest
{
    class MockWebAuthenticationBrokerContinuationEventArgs : IWebAuthenticationBrokerContinuationEventArgs
    {
        public ActivationKind Kind { get; set; }
        public ApplicationExecutionState PreviousExecutionState { get; private set; }
        public SplashScreen SplashScreen { get; private set; }
        public ValueSet ContinuationData { get; set; }
        public WebAuthenticationResult WebAuthenticationResult { get; private set; }
    }
}
