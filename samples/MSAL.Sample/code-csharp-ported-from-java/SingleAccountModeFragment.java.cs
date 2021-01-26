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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

// mc++ package com.azuresamples.msalandroidapp;

// mc++ import android.os.Bundle;

// mc++ import androidx.annotation.NonNull;
// mc++ import androidx.annotation.Nullable;
// mc++ import androidx.fragment.app.Fragment;

// mc++ import android.util.Log;
// mc++ import android.view.LayoutInflater;
// mc++ import android.view.View;
// mc++ import android.view.ViewGroup;
// mc++ import android.widget.Button;
// mc++ import android.widget.TextView;
// mc++ import android.widget.Toast;
// mc++ 
// mc++ import com.android.volley.Response;
// mc++ import com.android.volley.VolleyError;
// mc++ import com.microsoft.identity.client.AuthenticationCallback;
// mc++ import com.microsoft.identity.client.IAccount;
// mc++ import com.microsoft.identity.client.IAuthenticationResult;
// mc++ import com.microsoft.identity.client.IPublicClientApplication;
// mc++ import com.microsoft.identity.client.ISingleAccountPublicClientApplication;
// mc++ import com.microsoft.identity.client.PublicClientApplication;
// mc++ import com.microsoft.identity.client.SilentAuthenticationCallback;
// mc++ import com.microsoft.identity.client.exception.MsalClientException;
// mc++ import com.microsoft.identity.client.exception.MsalException;
// mc++ import com.microsoft.identity.client.exception.MsalServiceException;
// mc++ import com.microsoft.identity.client.exception.MsalUiRequiredException;

// mc++ import org.json.JSONObject;

/**
 * Implementation sample for 'Single account' mode.
 * <p>
 * If your app only supports one account being signed-in at a time, this is for you.
 * This requires "account_mode" to be set as "SINGLE" in the configuration file.
 * (Please see res/raw/auth_config_single_account.json for more info).
 * <p>
 * Please note that switching mode (between 'single' and 'multiple' might cause a loss of data.
 */

using System;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Fragment.App;
using Com.Microsoft.Identity.Client.Exception;
using global::Com.Microsoft.Identity.Client;
using MSAL.Sample;

namespace MSALSample
{
    internal class SingleAccountModeFragment : Fragment
    {
        private static readonly String TAG =
                                            // SingleAccountModeFragment.class.getSimpleName()
                                            typeof(SingleAccountModeFragment).Name
                                            ;

        /* UI & Debugging Variables */
        Button signInButton;
        Button signOutButton;
        Button callGraphApiInteractiveButton;
        Button callGraphApiSilentButton;
        TextView scopeTextView;
        TextView graphResourceTextView;
        TextView logTextView;
        TextView currentUserTextView;
        TextView deviceModeTextView;

        /* Azure AD Variables */
        private ISingleAccountPublicClientApplication mSingleAccountApp;
        private IAccount mAccount;

        // @Override
        public override View OnCreateView(LayoutInflater inflater,
                                  ViewGroup container,
                                  Bundle savedInstanceState)
        {
            // Inflate the layout for this fragment
            /* final */ View view = inflater.Inflate(Resource.Layout.fragment_single_account_mode, container, false);

            InitializeUI(view);

            // Creates a PublicClientApplication object with res/raw/auth_config_single_account.json
            PublicClientApplication.createSingleAccountPublicClientApplication(Context,
                    Resource.Id.Raw.auth_config_single_account,
                    new SingleAccountApplicationCreatedListener()                    
                    );
     
            return view;
        }

        /**
        * Initializes UI variables and callbacks.
        */
        private void InitializeUI(/* @NonNull */ /* final */ View view) {
            signInButton = view.FindViewById<Button>(Resource.Id.btn_signIn);
            signOutButton = view.FindViewById<Button>(Resource.Id.btn_removeAccount);
            callGraphApiInteractiveButton = view.FindViewById<Button>(Resource.Id.btn_callGraphInteractively);
            callGraphApiSilentButton = view.FindViewById<Button>(Resource.Id.btn_callGraphSilently);
            scopeTextView = view.FindViewById<TextView>(Resource.Id.scope);
            graphResourceTextView = view.FindViewById<TextView>(Resource.Id.msgraph_url);
            logTextView = view.FindViewById<TextView>(Resource.Id.txt_log);
            currentUserTextView = view.FindViewById<TextView>(Resource.Id.current_user);
            deviceModeTextView = view.FindViewById<TextView>(Resource.Id.device_mode);
            /* final */ String defaultGraphResourceUrl = MSGraphRequestWrapper.MS_GRAPH_ROOT_ENDPOINT + "v1.0/me";
            graphResourceTextView.Text = defaultGraphResourceUrl;
            signInButton.SetOnClickListener(new SignInButtonOnClickListener());
            signOutButton.SetOnClickListener(new SignOutButtonOnClickListener()); 
            callGraphApiInteractiveButton.SetOnClickListener(new CallGraphApiInteractiveOnClickListener());
            callGraphApiSilentButton.SetOnClickListener(new CallGraphApiSilentOnClickListener()); 

        }

    // @Override
        public void onResume() 
        {
            super.onResume();
 
            /**
            * The account may have been removed from the device (if broker is in use).
            *
            * In shared device mode, the account might be signed in/out by other apps while this app is not in focus.
            * Therefore, we want to update the account state by invoking loadAccount() here.
            */
             LoadAccount();
        }

         /**
          * Extracts a scope array from a text field,
          * i.e. from "User.Read User.ReadWrite" to ["user.read", "user.readwrite"]
          */
         private String[] GetScopes() 
         {
             return scopeTextView.Text.ToLower().Split(" ");
         }
     
         /**
          * Load the currently signed-in account, if there's any.
          */
         private void LoadAccount() 
         {
             if (mSingleAccountApp == null) 
             {
                 return;
             }
     
             mSingleAccountApp.GetCurrentAccountAsync(new SingleAccountPublicClientApplicationCurrentAccountCallback());
         }

         /**
          * Callback used in for silent acquireToken calls.
          */
         private SilentAuthenticationCallback getAuthSilentCallback() 
         {
             return new AuthSilentCallback();             
         }
         

         /**
          * Callback used for interactive request.
          * If succeeds we use the access token to call the Microsoft Graph.
          * Does not check cache.
          */
         private AuthenticationCallback getAuthInteractiveCallback() {
             return new AuthInteractiveCallback();             
         }


         /**
          * Make an HTTP request to obtain MSGraph data
          */
         private void CallGraphAPI(/* final */ IAuthenticationResult authenticationResult)
        {
             MSGraphRequestWrapper.callGraphAPIUsingVolley(
                     Context,
                     graphResourceTextView.Text.ToString(),
                     authenticationResult.AccessToken,
                     new ResponseListenerJSONObject(),
                     new ResponseErrorListener()
                    );
         }


         //
         // Helper methods manage UI updates
         // ================================
         // displayGraphResult() - Display the graph response
         // displayError() - Display the graph response
         // updateSignedInUI() - Updates UI when the user is signed in
         // updateSignedOutUI() - Updates UI when app sign out succeeds
         //
     
         /**
          * Display the graph response
          */
         private void DisplayGraphResult(/* @NonNull */ /* final */ Org.Json.JSONObject graphResponse) 
         {
             logTextView.Text = graphResponse.ToString();
         }


         /**
          * Display the error message
          */
         private void DisplayError(/* @NonNull */ /* final */ Exception exception) 
         {
             logTextView.Text = exception.ToString();
         }
     
         /**
          * Updates UI based on the current account.
          */
         private void UpdateUI() 
         {
             if (mAccount != null) 
             {
                 signInButton.Enabled = false;
                 signOutButton.Enabled = true;
                 callGraphApiInteractiveButton.Enabled = true;
                 callGraphApiSilentButton.Enabled = true;
                 currentUserTextView.Text = mAccount.Username;
             } 
             else 
             {
                 signInButton.Enabled = true;
                 signOutButton.Enabled = false;
                 callGraphApiInteractiveButton.Enabled = false;
                 callGraphApiSilentButton.Enabled = false;
                 currentUserTextView.Text = "None";
             }
     
             deviceModeTextView.Text = mSingleAccountApp.IsSharedDevice() ? "Shared" : "Non-shared";
         }

         /**
          * Updates UI when app sign out succeeds
          */
         private void showToastOnSignOut() 
         {
             /* final */ String signOutText = "Signed Out.";
             currentUserTextView.Text = "";
             Toast.MakeText(Context, signOutText, ToastLength.Short)
                     .Show();
         }
    }

    internal class SingleAccountApplicationCreatedListener : Java.Lang.Object, PublicClientApplication.ISingleAccountApplicationCreatedListener
    {
            // @Override
            public override void OnCreated(ISingleAccountPublicClientApplication application)
            {
                /**
                 * This test app assumes that the app is only going to support one account.
                 * This requires "account_mode" : "SINGLE" in the config json file.
                 **/
                mSingleAccountApp = application;
                LoadAccount();
            }

           //@Override
            public override void OnError(MsalException exception)
            {
                DisplayError(exception);
            }

    }

    internal class SignInButtonOnClickListener : Java.Lang.Object, View.IOnClickListener
    {
        public void OnClick(View v) 
        {
            if (mSingleAccountApp == null) 
            {
                return;
            }

            mSingleAccountApp.SignIn(GetActivity(), null, GetScopes(), getAuthInteractiveCallback());
        }
    }

    internal class SignOutButtonOnClickListener : Java.Lang.Object, View.IOnClickListener
    {
        public void OnClick(View v) 
        {
            if (mSingleAccountApp == null) {
                return;
            }

            /**
            * Removes the signed-in account and cached tokens from this app (or device, if the device is in shared mode).
            */
            mSingleAccountApp.SignOut(new SingleAccountSignOutCallback());
        }
    }

    internal class SingleAccountSignOutCallback : Java.Lang.Object, ISingleAccountPublicClientApplication.SignOutCallback
    {
        //@Override
        public void OnSignOut() 
        {
            mAccount = null;
            UpdateUI();
            ShowToastOnSignOut();
        }

        //@Override
        public void OnError(/* @NonNull */ MsalException exception) 
        {
            DisplayError(exception);
        }
    }

    internal class CallGraphApiInteractiveOnClickListener : Java.Lang.Object, View.IOnClickListener
    {
        public void OnClick(View v) 
        {
            if (mSingleAccountApp == null) 
            {
                return;
            }

            /**
            * If acquireTokenSilent() returns an error that requires an interaction (MsalUiRequiredException),
            * invoke acquireToken() to have the user resolve the interrupt interactively.
            *
            * Some example scenarios are
            *  - password change
            *  - the resource you're acquiring a token for has a stricter set of requirement than your Single Sign-On refresh token.
            *  - you're introducing a new scope which the user has never consented for.
            */
            mSingleAccountApp.AcquireToken(Activity, GetScopes(), GetAuthInteractiveCallback());
        }
    }

    internal class CallGraphApiSilentOnClickListener : Java.Lang.Object, View.IOnClickListener
    {
        //@Override
        public void OnClick(View v) 
        {
            if (mSingleAccountApp == null) 
            {
                return;
            }

            /**
            * Once you've signed the user in,
            * you can perform acquireTokenSilent to obtain resources without interrupting the user.
            */
            mSingleAccountApp.acquireTokenSilentAsync(GetScopes(), mAccount.getAuthority(), GetAuthSilentCallback());
        }
    }

    internal class SingleAccountPublicClientApplicationCurrentAccountCallback : Java.Lang.Object, ISingleAccountPublicClientApplication.CurrentAccountCallback 
    {
        //@Override
        public void OnAccountLoaded(/* @Nullable */ IAccount activeAccount) 
        {
            // You can use the account data to update your UI or your app database.
            mAccount = activeAccount;
            UpdateUI();
        }

        // @Override
        public void OnAccountChanged(/* @Nullable */ IAccount priorAccount, /* @Nullable */ IAccount currentAccount) 
        {
            if (currentAccount == null) {
                // Perform a cleanup task as the signed-in account changed.
                ShowToastOnSignOut();
            }
        }

        // @Override
        public void OnError(/* @NonNull */ MsalException exception) 
        {
            DisplayError(exception);
        }
    }
    internal class AuthSilentCallback : SilentAuthenticationCallback
    {

        //@Override
        public void OnSuccess(IAuthenticationResult authenticationResult) 
        {
            Log.Debug(TAG, "Successfully authenticated");

            /* Successfully got a token, use it to call a protected resource - MSGraph */
            CallGraphAPI(authenticationResult);
        }

        //@Override
        public void OnError(MsalException exception) 
        {
            /* Failed to acquireToken */
            Log.Debug(TAG, "Authentication failed: " + exception.ToString());
            DisplayError(exception);

            if (exception is MsalClientException) 
            {
                /* Exception inside MSAL, more info inside MsalError.java */
            } 
            else if (exception is MsalServiceException) 
            {
                /* Exception when communicating with the STS, likely config issue */
            } 
            else if (exception is MsalUiRequiredException) 
            {
                /* Tokens expired or no session, retry with interactive */
            }
        }
    }

    internal class AuthInteractiveCallback : AuthenticationCallback
    {
        //@Override
        public void OnSuccess(IAuthenticationResult authenticationResult) 
        {
            /* Successfully got a token, use it to call a protected resource - MSGraph */
            Log.Debug(TAG, "Successfully authenticated");
            Log.Debug(TAG, "ID Token: " + authenticationResult.Account.Claims.["id_token"]);

            /* Update account */
            mAccount = authenticationResult.Account;
            UpdateUI();

            /* call graph */
            CallGraphAPI(authenticationResult);
        }

        //@Override
        public void OnError(MsalException exception) 
        {
            /* Failed to acquireToken */
            Log.Debug(TAG, "Authentication failed: " + exception.ToString());
            DisplayError(exception);

            if (exception is MsalClientException) 
            {
                /* Exception inside MSAL, more info inside MsalError.java */
            } 
            else if (exception is MsalServiceException) 
            {
                /* Exception when communicating with the STS, likely config issue */
            }
        }

        //@Override
        public void OnCancel() 
        {
            /* User canceled the authentication */
            Log.Debug(TAG, "User cancelled login.");
        }
    };

    internal class ResponseListenerJSONObject : Java.Lang.Object, Response.Listener<JSONObject> 
    {
        //@Override
        public void OnResponse(JSONObject response) 
        {
            /* Successfully called graph, process data and send to UI */
            Log.Debug(TAG, "Response: " + response.ToString());
            DisplayGraphResult(response);
        }
    }
    internal class  ResponseErrorListener : Response.ErrorListener
    {
        //@Override
        public void onErrorResponse(VolleyError error) 
        {
            Log.Debug(TAG, "Error: " + error.ToString());
            DisplayError(error);
        }
    }

}
