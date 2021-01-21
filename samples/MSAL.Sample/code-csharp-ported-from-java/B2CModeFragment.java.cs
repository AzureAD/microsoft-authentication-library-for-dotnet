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
// mc++ import androidx.fragment.app.Fragment;
// mc++ 
// mc++ import android.util.Log;
// mc++ import android.view.LayoutInflater;
// mc++ import android.view.View;
// mc++ import android.view.ViewGroup;
// mc++ import android.widget.ArrayAdapter;
// mc++ import android.widget.Button;
// mc++ import android.widget.Spinner;
// mc++ import android.widget.TextView;
// mc++ 
// mc++ import com.microsoft.identity.client.AcquireTokenParameters;
// mc++ import com.microsoft.identity.client.AuthenticationCallback;
// mc++ import com.microsoft.identity.client.IAccount;
// mc++ import com.microsoft.identity.client.IAuthenticationResult;
// mc++ import com.microsoft.identity.client.IMultipleAccountPublicClientApplication;
// mc++ import com.microsoft.identity.client.IPublicClientApplication;
// mc++ import com.microsoft.identity.client.Prompt;
// mc++ import com.microsoft.identity.client.PublicClientApplication;
// mc++ import com.microsoft.identity.client.SilentAuthenticationCallback;
// mc++ import com.microsoft.identity.client.exception.MsalClientException;
// mc++ import com.microsoft.identity.client.exception.MsalException;
// mc++ import com.microsoft.identity.client.exception.MsalServiceException;
// mc++ import com.microsoft.identity.client.exception.MsalUiRequiredException;
// mc++ 
// mc++ import java.util.ArrayList;
// mc++ import java.util.List;
// mc++ 
// mc++ /**
// mc++  * Implementation sample for 'B2C' mode.
// mc++  */
// mc++ public class B2CModeFragment extends Fragment {
// mc++     private static final String TAG = B2CModeFragment.class.getSimpleName();
// mc++ 
// mc++     /* UI & Debugging Variables */
// mc++     Button removeAccountButton;
// mc++     Button runUserFlowButton;
// mc++     Button acquireTokenSilentButton;
// mc++     TextView graphResourceTextView;
// mc++     TextView logTextView;
// mc++     Spinner policyListSpinner;
// mc++     Spinner b2cUserList;
// mc++ 
// mc++     private List<B2CUser> users;
// mc++ 
// mc++     /* Azure AD Variables */
// mc++     private IMultipleAccountPublicClientApplication b2cApp;
// mc++ 
// mc++     @Override
// mc++     public View onCreateView(LayoutInflater inflater, ViewGroup container,
// mc++                              Bundle savedInstanceState) {
// mc++         // Inflate the layout for this fragment
// mc++         final View view = inflater.inflate(R.layout.fragment_b2c_mode, container, false);
// mc++         initializeUI(view);
// mc++ 
// mc++         // Creates a PublicClientApplication object with res/raw/auth_config_single_account.json
// mc++         PublicClientApplication.createMultipleAccountPublicClientApplication(getContext(),
// mc++                 R.raw.auth_config_b2c,
// mc++                 new IPublicClientApplication.IMultipleAccountApplicationCreatedListener() {
// mc++                     @Override
// mc++                     public void onCreated(IMultipleAccountPublicClientApplication application) {
// mc++                         b2cApp = application;
// mc++                         loadAccounts();
// mc++                     }
// mc++ 
// mc++                     @Override
// mc++                     public void onError(MsalException exception) {
// mc++                         displayError(exception);
// mc++                         removeAccountButton.setEnabled(false);
// mc++                         runUserFlowButton.setEnabled(false);
// mc++                         acquireTokenSilentButton.setEnabled(false);
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
// mc++         removeAccountButton = view.findViewById(R.id.btn_removeAccount);
// mc++         runUserFlowButton = view.findViewById(R.id.btn_runUserFlow);
// mc++         acquireTokenSilentButton = view.findViewById(R.id.btn_acquireTokenSilently);
// mc++         graphResourceTextView = view.findViewById(R.id.msgraph_url);
// mc++         logTextView = view.findViewById(R.id.txt_log);
// mc++         policyListSpinner = view.findViewById(R.id.policy_list);
// mc++         b2cUserList = view.findViewById(R.id.user_list);
// mc++ 
// mc++         final ArrayAdapter<String> dataAdapter = new ArrayAdapter<>(
// mc++                 getContext(), android.R.layout.simple_spinner_item,
// mc++                 new ArrayList<String>() {{
// mc++                     for (final String policyName : B2CConfiguration.Policies)
// mc++                         add(policyName);
// mc++                 }}
// mc++         );
// mc++ 
// mc++         dataAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
// mc++         policyListSpinner.setAdapter(dataAdapter);
// mc++         dataAdapter.notifyDataSetChanged();
// mc++ 
// mc++         runUserFlowButton.setOnClickListener(new View.OnClickListener() {
// mc++             public void onClick(View v) {
// mc++                 if (b2cApp == null) {
// mc++                     return;
// mc++                 }
// mc++ 
// mc++                 /**
// mc++                  * Runs user flow interactively.
// mc++                  * <p>
// mc++                  * Once the user finishes with the flow, you will also receive an access token containing the claims for the scope you passed in (see B2CConfiguration.getScopes()),
// mc++                  * which you can subsequently use to obtain your resources.
// mc++                  */
// mc++ 
// mc++                 AcquireTokenParameters parameters = new AcquireTokenParameters.Builder()
// mc++                         .startAuthorizationFromActivity(getActivity())
// mc++                         .fromAuthority(B2CConfiguration.getAuthorityFromPolicyName(policyListSpinner.getSelectedItem().toString()))
// mc++                         .withScopes(B2CConfiguration.getScopes())
// mc++                         .withPrompt(Prompt.LOGIN)
// mc++                         .withCallback(getAuthInteractiveCallback())
// mc++                         .build();
// mc++ 
// mc++                 b2cApp.acquireToken(parameters);
// mc++ 
// mc++             }
// mc++         });
// mc++ 
// mc++         acquireTokenSilentButton.setOnClickListener(new View.OnClickListener() {
// mc++             @Override
// mc++             public void onClick(View v) {
// mc++                 if (b2cApp == null) {
// mc++                     return;
// mc++                 }
// mc++ 
// mc++                 final B2CUser selectedUser = users.get(b2cUserList.getSelectedItemPosition());
// mc++                 selectedUser.acquireTokenSilentAsync(b2cApp,
// mc++                         policyListSpinner.getSelectedItem().toString(),
// mc++                         B2CConfiguration.getScopes(),
// mc++                         getAuthSilentCallback());
// mc++             }
// mc++         });
// mc++ 
// mc++         removeAccountButton.setOnClickListener(new View.OnClickListener() {
// mc++             public void onClick(View v) {
// mc++                 if (b2cApp == null) {
// mc++                     return;
// mc++                 }
// mc++ 
// mc++                 final B2CUser selectedUser = users.get(b2cUserList.getSelectedItemPosition());
// mc++                 selectedUser.signOutAsync(b2cApp,
// mc++                         new IMultipleAccountPublicClientApplication.RemoveAccountCallback() {
// mc++                             @Override
// mc++                             public void onRemoved() {
// mc++                                 logTextView.setText("Signed Out.");
// mc++                                 loadAccounts();
// mc++                             }
// mc++ 
// mc++                             @Override
// mc++                             public void onError(@NonNull MsalException exception) {
// mc++                                 displayError(exception);
// mc++                             }
// mc++                         });
// mc++             }
// mc++         });
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Load signed-in accounts, if there's any.
// mc++      */
// mc++     private void loadAccounts() {
// mc++         if (b2cApp == null) {
// mc++             return;
// mc++         }
// mc++ 
// mc++         b2cApp.getAccounts(new IPublicClientApplication.LoadAccountsCallback() {
// mc++             @Override
// mc++             public void onTaskCompleted(final List<IAccount> result) {
// mc++                 users = B2CUser.getB2CUsersFromAccountList(result);
// mc++                 updateUI(users);
// mc++             }
// mc++ 
// mc++             @Override
// mc++             public void onError(MsalException exception) {
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
// mc++                 /* Successfully got a token. */
// mc++                 displayResult(authenticationResult);
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
// mc++ 
// mc++                 /* display result info */
// mc++                 displayResult(authenticationResult);
// mc++ 
// mc++                 /* Reload account asynchronously to get the up-to-date list. */
// mc++                 loadAccounts();
// mc++             }
// mc++ 
// mc++             @Override
// mc++             public void onError(MsalException exception) {
// mc++                 final String B2C_PASSWORD_CHANGE = "AADB2C90118";
// mc++                 if (exception.getMessage().contains(B2C_PASSWORD_CHANGE)) {
// mc++                     logTextView.setText("The user clicks the 'Forgot Password' link in a sign-up or sign-in user flow.\n" +
// mc++                             "Your application needs to handle this error code by running a specific user flow that resets the password.");
// mc++                     return;
// mc++                 }
// mc++ 
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
// mc++     //
// mc++     // Helper methods manage UI updates
// mc++     // ================================
// mc++     // displayResult() - Display the authentication result.
// mc++     // displayError() - Display the token error.
// mc++     // updateSignedInUI() - Updates UI when the user is signed in
// mc++     // updateSignedOutUI() - Updates UI when app sign out succeeds
// mc++     //
// mc++ 
// mc++     /**
// mc++      * Display the graph response
// mc++      */
// mc++     private void displayResult(@NonNull final IAuthenticationResult result) {
// mc++         final String output =
// mc++                 "Access Token :" + result.getAccessToken() + "\n" +
// mc++                         "Scope : " + result.getScope() + "\n" +
// mc++                         "Expiry : " + result.getExpiresOn() + "\n" +
// mc++                         "Tenant ID : " + result.getTenantId() + "\n";
// mc++ 
// mc++         logTextView.setText(output);
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
// mc++      * Updates UI based on the obtained user list.
// mc++      */
// mc++     private void updateUI(final List<B2CUser> users) {
// mc++         if (users.size() != 0) {
// mc++             removeAccountButton.setEnabled(true);
// mc++             acquireTokenSilentButton.setEnabled(true);
// mc++         } else {
// mc++             removeAccountButton.setEnabled(false);
// mc++             acquireTokenSilentButton.setEnabled(false);
// mc++         }
// mc++ 
// mc++         final ArrayAdapter<String> dataAdapter = new ArrayAdapter<>(
// mc++                 getContext(), android.R.layout.simple_spinner_item,
// mc++                 new ArrayList<String>() {{
// mc++                     for (final B2CUser user : users)
// mc++                         add(user.getDisplayName());
// mc++                 }}
// mc++         );
// mc++ 
// mc++         dataAdapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
// mc++         b2cUserList.setAdapter(dataAdapter);
// mc++         dataAdapter.notifyDataSetChanged();
// mc++     }
// mc++ 
// mc++ }
// mc++ 
