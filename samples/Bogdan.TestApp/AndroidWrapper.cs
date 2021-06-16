using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Com.Microsoft.Identity.Client;
using Com.Microsoft.Identity.Client.Exception;

namespace App1
{
    /// <summary>
    /// This is a wrapper for Android
    /// It can be further extended to support Android only scanarios
    /// </summary>
    public class AndroidWrapper : INativeWrapper
    {
        /// <summary>
        /// This is set programatically for PoC
        /// It can be just get property that will retreive the current activity
        /// </summary>
        public Activity Current { get; set; } 

        // PCA
        IMultipleAccountPublicClientApplication _mpca;

        /// <summary>
        /// Initialize this when PCA is created
        /// </summary>
        /// <param name="mpca"></param>
        public void Init(IMultipleAccountPublicClientApplication mpca)
        {
            _mpca = mpca;
        }

        /// <summary>
        /// Acquires token sugin callback. The callback is encapsulated as asyn method
        /// </summary>
        /// <param name="scopes"></param>
        /// <returns></returns>
        public Task<IAuthenticationResult> AcquireTokenInteractiveAsync(string[] scopes)
        {
            // Create task completion source
            TaskCompletionSource<IAuthenticationResult> tcsResult = new TaskCompletionSource<IAuthenticationResult>();

            // build parameters
            var atpBuilder = new AcquireTokenParameters.Builder();
            atpBuilder.WithScopes(scopes);
            atpBuilder.Build();
            AcquireTokenParameters atp = new AcquireTokenParameters(atpBuilder);

            // create instance of the callback
            var interactiveCallback = new InteractiveAuthCallback(
                onCancelAction: () => {
                    // on cancel, set results to canceled
                    tcsResult.SetCanceled();
                },
                onErrorAction: (ex) =>
                {
                    // throw any excpetion
                    throw ex;
                },
                onSuccessAction: (result) =>
                {
                    // set the result on sucess
                    tcsResult.SetResult(result);
                });

            // now call the method to acquire token
            _mpca.AcquireToken(Current, scopes, interactiveCallback);
            return tcsResult.Task;
        }

        public Task<IAuthenticationResult> AcquireTokenSilentAsync(string[] scopes, IAccount account)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<IAccount>> GetAccountsAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This encapsulates callback method to respond to the callback of acquiring token
        /// </summary>
        public class InteractiveAuthCallback :
            Java.Lang.Object,
            IAuthenticationCallback
        {
            private readonly Action _onCancelAction;
            private readonly Action<IAuthenticationResult> _onSuccessAction;
            private readonly Action<MsalException> _onErrorAction;

            public InteractiveAuthCallback(
                Action onCancelAction,
                Action<IAuthenticationResult> onSuccessAction,
                Action<MsalException> onErrorAction)
            {
                _onCancelAction = onCancelAction;
                _onSuccessAction = onSuccessAction;
                _onErrorAction = onErrorAction;
            }

            public void OnCancel()
            {
                _onCancelAction();
            }

            public void OnError(MsalException p0)
            {
                _onErrorAction(p0);
            }

            public void OnSuccess(IAuthenticationResult p0)
            {
                _onSuccessAction(p0);
            }
        }
    }
}
