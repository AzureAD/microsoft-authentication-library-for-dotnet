// mc++ // Copyright (c) Microsoft Corporation.
// mc++ // All rights reserved.
// mc++ //
// mc++ // This code is licensed under the MIT License.
// mc++ //
// mc++ // Permission is hereby granted, free of charge, to any person obtaining a copy
// mc++ // of this software and associated documentation files(the "Software"), to deal
// mc++ // in the Software without restriction, including without limitation the rights
// mc++ // to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// mc++ // copies of the Software, and to permit persons to whom the Software is
// mc++ // furnished to do so, subject to the following conditions :
// mc++ //
// mc++ // The above copyright notice and this permission notice shall be included in
// mc++ // all copies or substantial portions of the Software.
// mc++ //
// mc++ // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// mc++ // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// mc++ // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// mc++ // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// mc++ // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// mc++ // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// mc++ // THE SOFTWARE.
// mc++ 
// mc++ package com.azuresamples.msalandroidapp;
// mc++ 
// mc++ import android.os.Bundle;
// mc++ 
// mc++ import androidx.annotation.NonNull;
// mc++ import androidx.annotation.Nullable;
// mc++ import androidx.fragment.app.Fragment;
// mc++ 
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
// mc++ 
// mc++ import org.json.JSONObject;
// mc++ 
// mc++ /**
// mc++  * Implementation sample for 'Single account' mode.
// mc++  * <p>
// mc++  * If your app only supports one account being signed-in at a time, this is for you.
// mc++  * This requires "account_mode" to be set as "SINGLE" in the configuration file.
// mc++  * (Please see res/raw/auth_config_single_account.json for more info).
// mc++  * <p>
// mc++  * Please note that switching mode (between 'single' and 'multiple' might cause a loss of data.
// mc++  */
// mc++ public class SingleAccountModeFragment extends Fragment {
// mc++     private static final String TAG = SingleAccountModeFragment.class.getSimpleName();
// mc++ 
// mc++     /* UI & Debugging Variables */
// mc++     Button signInButton;
// mc++     Button signOutButton;
// mc++     Button callGraphApiInteractiveButton;
// mc++     Button callGraphApiSilentButton;
// mc++     TextView scopeTextView;
// mc++     TextView graphResourceTextView;
// mc++     TextView logTextView;
// mc++     TextView currentUserTextView;
// mc++     TextView deviceModeTextView;
// mc++ 
// mc++     /* Azure AD Variables */
// mc++     private ISingleAccountPublicClientApplication mSingleAccountApp;
// mc++     private IAccount mAccount;
// mc++ 
// mc++     @Override
// mc++     public View onCreateView(LayoutInflater inflater,
// mc++                              ViewGroup container,
// mc++                              Bundle savedInstanceState) {
// mc++         // Inflate the layout for this fragment
// mc++         final View view = inflater.inflate(R.layout.fragment_single_account_mode, container, false);
// mc++         initializeUI(view);
// mc++ 
// mc++         // Creates a PublicClientApplication object with res/raw/auth_config_single_account.json
// mc++         PublicClientApplication.createSingleAccountPublicClientApplication(getContext(),
// mc++                 R.raw.auth_config_single_account,
// mc++                 new IPublicClientApplication.ISingleAccountApplicationCreatedListener() {
// mc++                     @Override
// mc++                     public void onCreated(ISingleAccountPublicClientApplication application) {
// mc++                         /**
// mc++                          * This test app assumes that the app is only going to support one account.
// mc++                          * This requires "account_mode" : "SINGLE" in the config json file.
// mc++                          **/
// mc++                         mSingleAccountApp = application;
// mc++                         loadAccount();
// mc++                     }
// mc++ 
// mc++                     @Override
// mc++                     public void onError(MsalException exception) {
// mc++                         displayError(exception);
// mc++                     }
// mc++                 });
// mc++ 
// mc++         return view;
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Initializes UI variables and callbacks.
// mc++      */
// mc++     private void initializeUI(@NonNull final View view) {
// mc++         signInButton = view.findViewById(R.id.btn_signIn);
// mc++         signOutButton = view.findViewById(R.id.btn_removeAccount);
// mc++         callGraphApiInteractiveButton = view.findViewById(R.id.btn_callGraphInteractively);
// mc++         callGraphApiSilentButton = view.findViewById(R.id.btn_callGraphSilently);
// mc++         scopeTextView = view.findViewById(R.id.scope);
// mc++         graphResourceTextView = view.findViewById(R.id.msgraph_url);
// mc++         logTextView = view.findViewById(R.id.txt_log);
// mc++         currentUserTextView = view.findViewById(R.id.current_user);
// mc++         deviceModeTextView = view.findViewById(R.id.device_mode);
// mc++ 
// mc++         final String defaultGraphResourceUrl = MSGraphRequestWrapper.MS_GRAPH_ROOT_ENDPOINT + "v1.0/me";
// mc++         graphResourceTextView.setText(defaultGraphResourceUrl);
// mc++ 
// mc++         signInButton.setOnClickListener(new View.OnClickListener() {
// mc++             public void onClick(View v) {
// mc++                 if (mSingleAccountApp == null) {
// mc++                     return;
// mc++                 }
// mc++ 
// mc++                 mSingleAccountApp.signIn(getActivity(), null, getScopes(), getAuthInteractiveCallback());
// mc++             }
// mc++         });
// mc++ 
// mc++         signOutButton.setOnClickListener(new View.OnClickListener() {
// mc++             public void onClick(View v) {
// mc++                 if (mSingleAccountApp == null) {
// mc++                     return;
// mc++                 }
// mc++ 
// mc++                 /**
// mc++                  * Removes the signed-in account and cached tokens from this app (or device, if the device is in shared mode).
// mc++                  */
// mc++                 mSingleAccountApp.signOut(new ISingleAccountPublicClientApplication.SignOutCallback() {
// mc++                     @Override
// mc++                     public void onSignOut() {
// mc++                         mAccount = null;
// mc++                         updateUI();
// mc++                         showToastOnSignOut();
// mc++                     }
// mc++ 
// mc++                     @Override
// mc++                     public void onError(@NonNull MsalException exception) {
// mc++                         displayError(exception);
// mc++                     }
// mc++                 });
// mc++             }
// mc++         });
// mc++ 
// mc++         callGraphApiInteractiveButton.setOnClickListener(new View.OnClickListener() {
// mc++             public void onClick(View v) {
// mc++                 if (mSingleAccountApp == null) {
// mc++                     return;
// mc++                 }
// mc++ 
// mc++                 /**
// mc++                  * If acquireTokenSilent() returns an error that requires an interaction (MsalUiRequiredException),
// mc++                  * invoke acquireToken() to have the user resolve the interrupt interactively.
// mc++                  *
// mc++                  * Some example scenarios are
// mc++                  *  - password change
// mc++                  *  - the resource you're acquiring a token for has a stricter set of requirement than your Single Sign-On refresh token.
// mc++                  *  - you're introducing a new scope which the user has never consented for.
// mc++                  */
// mc++                 mSingleAccountApp.acquireToken(getActivity(), getScopes(), getAuthInteractiveCallback());
// mc++             }
// mc++         });
// mc++ 
// mc++         callGraphApiSilentButton.setOnClickListener(new View.OnClickListener() {
// mc++             @Override
// mc++             public void onClick(View v) {
// mc++                 if (mSingleAccountApp == null) {
// mc++                     return;
// mc++                 }
// mc++ 
// mc++                 /**
// mc++                  * Once you've signed the user in,
// mc++                  * you can perform acquireTokenSilent to obtain resources without interrupting the user.
// mc++                  */
// mc++                 mSingleAccountApp.acquireTokenSilentAsync(getScopes(), mAccount.getAuthority(), getAuthSilentCallback());
// mc++             }
// mc++         });
// mc++ 
// mc++     }
// mc++ 
// mc++     @Override
// mc++     public void onResume() {
// mc++         super.onResume();
// mc++ 
// mc++         /**
// mc++          * The account may have been removed from the device (if broker is in use).
// mc++          *
// mc++          * In shared device mode, the account might be signed in/out by other apps while this app is not in focus.
// mc++          * Therefore, we want to update the account state by invoking loadAccount() here.
// mc++          */
// mc++         loadAccount();
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Extracts a scope array from a text field,
// mc++      * i.e. from "User.Read User.ReadWrite" to ["user.read", "user.readwrite"]
// mc++      */
// mc++     private String[] getScopes() {
// mc++         return scopeTextView.getText().toString().toLowerCase().split(" ");
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Load the currently signed-in account, if there's any.
// mc++      */
// mc++     private void loadAccount() {
// mc++         if (mSingleAccountApp == null) {
// mc++             return;
// mc++         }
// mc++ 
// mc++         mSingleAccountApp.getCurrentAccountAsync(new ISingleAccountPublicClientApplication.CurrentAccountCallback() {
// mc++             @Override
// mc++             public void onAccountLoaded(@Nullable IAccount activeAccount) {
// mc++                 // You can use the account data to update your UI or your app database.
// mc++                 mAccount = activeAccount;
// mc++                 updateUI();
// mc++             }
// mc++ 
// mc++             @Override
// mc++             public void onAccountChanged(@Nullable IAccount priorAccount, @Nullable IAccount currentAccount) {
// mc++                 if (currentAccount == null) {
// mc++                     // Perform a cleanup task as the signed-in account changed.
// mc++                     showToastOnSignOut();
// mc++                 }
// mc++             }
// mc++ 
// mc++             @Override
// mc++             public void onError(@NonNull MsalException exception) {
// mc++                 displayError(exception);
// mc++             }
// mc++         });
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Callback used in for silent acquireToken calls.
// mc++      */
// mc++     private SilentAuthenticationCallback getAuthSilentCallback() {
// mc++         return new SilentAuthenticationCallback() {
// mc++ 
// mc++             @Override
// mc++             public void onSuccess(IAuthenticationResult authenticationResult) {
// mc++                 Log.d(TAG, "Successfully authenticated");
// mc++ 
// mc++                 /* Successfully got a token, use it to call a protected resource - MSGraph */
// mc++                 callGraphAPI(authenticationResult);
// mc++             }
// mc++ 
// mc++             @Override
// mc++             public void onError(MsalException exception) {
// mc++                 /* Failed to acquireToken */
// mc++                 Log.d(TAG, "Authentication failed: " + exception.toString());
// mc++                 displayError(exception);
// mc++ 
// mc++                 if (exception instanceof MsalClientException) {
// mc++                     /* Exception inside MSAL, more info inside MsalError.java */
// mc++                 } else if (exception instanceof MsalServiceException) {
// mc++                     /* Exception when communicating with the STS, likely config issue */
// mc++                 } else if (exception instanceof MsalUiRequiredException) {
// mc++                     /* Tokens expired or no session, retry with interactive */
// mc++                 }
// mc++             }
// mc++         };
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Callback used for interactive request.
// mc++      * If succeeds we use the access token to call the Microsoft Graph.
// mc++      * Does not check cache.
// mc++      */
// mc++     private AuthenticationCallback getAuthInteractiveCallback() {
// mc++         return new AuthenticationCallback() {
// mc++ 
// mc++             @Override
// mc++             public void onSuccess(IAuthenticationResult authenticationResult) {
// mc++                 /* Successfully got a token, use it to call a protected resource - MSGraph */
// mc++                 Log.d(TAG, "Successfully authenticated");
// mc++                 Log.d(TAG, "ID Token: " + authenticationResult.getAccount().getClaims().get("id_token"));
// mc++ 
// mc++                 /* Update account */
// mc++                 mAccount = authenticationResult.getAccount();
// mc++                 updateUI();
// mc++ 
// mc++                 /* call graph */
// mc++                 callGraphAPI(authenticationResult);
// mc++             }
// mc++ 
// mc++             @Override
// mc++             public void onError(MsalException exception) {
// mc++                 /* Failed to acquireToken */
// mc++                 Log.d(TAG, "Authentication failed: " + exception.toString());
// mc++                 displayError(exception);
// mc++ 
// mc++                 if (exception instanceof MsalClientException) {
// mc++                     /* Exception inside MSAL, more info inside MsalError.java */
// mc++                 } else if (exception instanceof MsalServiceException) {
// mc++                     /* Exception when communicating with the STS, likely config issue */
// mc++                 }
// mc++             }
// mc++ 
// mc++             @Override
// mc++             public void onCancel() {
// mc++                 /* User canceled the authentication */
// mc++                 Log.d(TAG, "User cancelled login.");
// mc++             }
// mc++         };
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Make an HTTP request to obtain MSGraph data
// mc++      */
// mc++     private void callGraphAPI(final IAuthenticationResult authenticationResult) {
// mc++         MSGraphRequestWrapper.callGraphAPIUsingVolley(
// mc++                 getContext(),
// mc++                 graphResourceTextView.getText().toString(),
// mc++                 authenticationResult.getAccessToken(),
// mc++                 new Response.Listener<JSONObject>() {
// mc++                     @Override
// mc++                     public void onResponse(JSONObject response) {
// mc++                         /* Successfully called graph, process data and send to UI */
// mc++                         Log.d(TAG, "Response: " + response.toString());
// mc++                         displayGraphResult(response);
// mc++                     }
// mc++                 },
// mc++                 new Response.ErrorListener() {
// mc++                     @Override
// mc++                     public void onErrorResponse(VolleyError error) {
// mc++                         Log.d(TAG, "Error: " + error.toString());
// mc++                         displayError(error);
// mc++                     }
// mc++                 });
// mc++     }
// mc++ 
// mc++     //
// mc++     // Helper methods manage UI updates
// mc++     // ================================
// mc++     // displayGraphResult() - Display the graph response
// mc++     // displayError() - Display the graph response
// mc++     // updateSignedInUI() - Updates UI when the user is signed in
// mc++     // updateSignedOutUI() - Updates UI when app sign out succeeds
// mc++     //
// mc++ 
// mc++     /**
// mc++      * Display the graph response
// mc++      */
// mc++     private void displayGraphResult(@NonNull final JSONObject graphResponse) {
// mc++         logTextView.setText(graphResponse.toString());
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Display the error message
// mc++      */
// mc++     private void displayError(@NonNull final Exception exception) {
// mc++         logTextView.setText(exception.toString());
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Updates UI based on the current account.
// mc++      */
// mc++     private void updateUI() {
// mc++         if (mAccount != null) {
// mc++             signInButton.setEnabled(false);
// mc++             signOutButton.setEnabled(true);
// mc++             callGraphApiInteractiveButton.setEnabled(true);
// mc++             callGraphApiSilentButton.setEnabled(true);
// mc++             currentUserTextView.setText(mAccount.getUsername());
// mc++         } else {
// mc++             signInButton.setEnabled(true);
// mc++             signOutButton.setEnabled(false);
// mc++             callGraphApiInteractiveButton.setEnabled(false);
// mc++             callGraphApiSilentButton.setEnabled(false);
// mc++             currentUserTextView.setText("None");
// mc++         }
// mc++ 
// mc++         deviceModeTextView.setText(mSingleAccountApp.isSharedDevice() ? "Shared" : "Non-shared");
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Updates UI when app sign out succeeds
// mc++      */
// mc++     private void showToastOnSignOut() {
// mc++         final String signOutText = "Signed Out.";
// mc++         currentUserTextView.setText("");
// mc++         Toast.makeText(getContext(), signOutText, Toast.LENGTH_SHORT)
// mc++                 .show();
// mc++     }
// mc++ }
