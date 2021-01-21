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
// mc++ import androidx.annotation.NonNull;
// mc++ 
// mc++ import com.microsoft.identity.client.AcquireTokenSilentParameters;
// mc++ import com.microsoft.identity.client.IAccount;
// mc++ import com.microsoft.identity.client.IMultipleAccountPublicClientApplication;
// mc++ import com.microsoft.identity.client.SilentAuthenticationCallback;
// mc++ import com.microsoft.identity.client.exception.MsalException;
// mc++ import com.microsoft.identity.client.exception.MsalUiRequiredException;
// mc++ import com.microsoft.identity.common.internal.providers.oauth2.IDToken;
// mc++ 
// mc++ import java.util.ArrayList;
// mc++ import java.util.HashMap;
// mc++ import java.util.List;
// mc++ 
// mc++ /**
// mc++  * Represents a B2C user.
// mc++  */
// mc++ public class B2CUser {
// mc++     /**
// mc++      * A factory method for generating B2C users based on the given IAccount list.
// mc++      */
// mc++     public static List<B2CUser> getB2CUsersFromAccountList(@NonNull final List<IAccount> accounts) {
// mc++         final HashMap<String, B2CUser> b2CUserHashMap = new HashMap<>();
// mc++ 
// mc++         for (IAccount account : accounts) {
// mc++             /**
// mc++              * NOTE: Because B2C treats each policy as a separate authority, the access tokens, refresh tokens, and id tokens returned from each policy are considered logically separate entities.
// mc++              *       In practical terms, this means that each policy returns a separate IAccount object whose tokens cannot be used to invoke other policies.
// mc++              *
// mc++              *       You can use the 'Subject' claim to identify that those accounts belong to the same user.
// mc++              */
// mc++             final String subject = B2CUser.getSubjectFromAccount(account);
// mc++ 
// mc++             B2CUser user = b2CUserHashMap.get(subject);
// mc++             if (user == null) {
// mc++                 user = new B2CUser();
// mc++                 b2CUserHashMap.put(subject, user);
// mc++             }
// mc++ 
// mc++             user.accounts.add(account);
// mc++         }
// mc++ 
// mc++         List<B2CUser> users = new ArrayList<>();
// mc++         users.addAll(b2CUserHashMap.values());
// mc++         return users;
// mc++     }
// mc++ 
// mc++     /**
// mc++      * List of account objects that are associated to this B2C user.
// mc++      */
// mc++     private List<IAccount> accounts = new ArrayList<>();
// mc++ 
// mc++     private B2CUser() {
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Gets this user's display name.
// mc++      * If the value is not set, returns 'subject' instead.
// mc++      */
// mc++     public String getDisplayName() {
// mc++         if (accounts.isEmpty()) {
// mc++             return null;
// mc++         }
// mc++ 
// mc++         // Make sure that all of your policies are returning the same set of claims.
// mc++         final String displayName = getB2CDisplayNameFromAccount(accounts.get(0));
// mc++         if (displayName != null) {
// mc++             return displayName;
// mc++         }
// mc++ 
// mc++         return getSubjectFromAccount(accounts.get(0));
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Acquires a token without interrupting the user.
// mc++      */
// mc++     public void acquireTokenSilentAsync(final IMultipleAccountPublicClientApplication multipleAccountPublicClientApplication,
// mc++                                         final String policyName,
// mc++                                         final List<String> scopes,
// mc++                                         final SilentAuthenticationCallback callback) {
// mc++ 
// mc++         for (IAccount account : accounts) {
// mc++             if (policyName.equalsIgnoreCase(getB2CPolicyNameFromAccount(account))) {
// mc++                 AcquireTokenSilentParameters parameters = new AcquireTokenSilentParameters.Builder()
// mc++                         .fromAuthority(B2CConfiguration.getAuthorityFromPolicyName(policyName))
// mc++                         .withScopes(scopes)
// mc++                         .forAccount(account)
// mc++                         .withCallback(callback)
// mc++                         .build();
// mc++ 
// mc++                 multipleAccountPublicClientApplication.acquireTokenSilentAsync(parameters);
// mc++                 return;
// mc++             }
// mc++         }
// mc++ 
// mc++         callback.onError(
// mc++                 new MsalUiRequiredException(MsalUiRequiredException.NO_ACCOUNT_FOUND,
// mc++                         "Account associated to the policy is not found."));
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Signs the user out of your application.
// mc++      */
// mc++     public void signOutAsync(final IMultipleAccountPublicClientApplication multipleAccountPublicClientApplication,
// mc++                              final IMultipleAccountPublicClientApplication.RemoveAccountCallback callback) {
// mc++         new Thread(new Runnable() {
// mc++             @Override
// mc++             public void run() {
// mc++                 try {
// mc++                     for (IAccount account : accounts) {
// mc++                         multipleAccountPublicClientApplication.removeAccount(account);
// mc++                     }
// mc++ 
// mc++                     accounts.clear();
// mc++                     callback.onRemoved();
// mc++                 } catch (MsalException e) {
// mc++                     callback.onError(e);
// mc++                 } catch (InterruptedException e) {
// mc++                     // Unexpected.
// mc++                 }
// mc++             }
// mc++         }).start();
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Get name of the policy associated with the given B2C account.
// mc++      * See https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-tokens for more info.
// mc++      */
// mc++     private static String getB2CPolicyNameFromAccount(@NonNull final IAccount account) {
// mc++         final String policyName = (String) (account.getClaims().get("tfp"));
// mc++ 
// mc++         if (policyName == null) {
// mc++             // Fallback to "acr" (for older policies)
// mc++             return (String) (account.getClaims().get("acr"));
// mc++         }
// mc++ 
// mc++         return policyName;
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Get subject of the given B2C account.
// mc++      * <p>
// mc++      * Subject is the principal about which the token asserts information, such as the user of an application.
// mc++      * See https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-tokens for more info.
// mc++      */
// mc++     private static String getSubjectFromAccount(@NonNull final IAccount account) {
// mc++         return (String) (account.getClaims().get(IDToken.SUBJECT));
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Get a displayable name of the given B2C account.
// mc++      * This claim is optional.
// mc++      */
// mc++     private static String getB2CDisplayNameFromAccount(@NonNull final IAccount account) {
// mc++         Object displayName = account.getClaims().get(IDToken.NAME);
// mc++ 
// mc++         if (displayName == null) {
// mc++             return null;
// mc++         }
// mc++ 
// mc++         return displayName.toString();
// mc++     }
// mc++ }
