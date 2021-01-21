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
// mc++ import java.util.Arrays;
// mc++ import java.util.List;
// mc++ 
// mc++ /**
// mc++  * The value in this class has to map with the json configuration file (auth_config_b2c.json).
// mc++  * i.e. If you are using the following json file.
// mc++  * {
// mc++  *   "client_id" : "90c0fe63-bcf2-44d5-8fb7-b8bbc0b29dc6",
// mc++  *   "redirect_uri" : "msauth://com.azuresamples.msalandroidapp/1wIqXSqBj7w%2Bh11ZifsnqwgyKrY%3D",
// mc++  *   "account_mode" : "MULTIPLE",
// mc++  *   "broker_redirect_uri_registered": false,
// mc++  *   "authorities": [
// mc++  *     {
// mc++  *       "type": "B2C",
// mc++  *       "authority_url": "https://fabrikamb2c.b2clogin.com/tfp/fabrikamb2c.onmicrosoft.com/b2c_1_susi/",
// mc++  *       "default": true
// mc++  *     },
// mc++  *     {
// mc++  *       "type": "B2C",
// mc++  *       "authority_url": "https://fabrikamb2c.b2clogin.com/tfp/fabrikamb2c.onmicrosoft.com/b2c_1_edit_profile/"
// mc++  *     },
// mc++  *     {
// mc++  *       "type": "B2C",
// mc++  *       "authority_url": "https://fabrikamb2c.b2clogin.com/tfp/fabrikamb2c.onmicrosoft.com/b2c_1_reset/"
// mc++  *     }
// mc++  *   ]
// mc++  * }
// mc++  * <p>
// mc++  * This file contains 2 B2C policies, namely "b2c_1_susi", "b2c_1_edit_profile" and "b2c_1_reset"
// mc++  * Its azureAdB2CHostName is "fabrikamb2c.b2clogin.com"
// mc++  * Its tenantName is "fabrikamb2c.onmicrosoft.com"
// mc++  */
// mc++ public class B2CConfiguration {
// mc++     /**
// mc++      * Name of the policies/user flows in your B2C tenant.
// mc++      * See https://docs.microsoft.com/en-us/azure/active-directory-b2c/active-directory-b2c-reference-policies for more info.
// mc++      */
// mc++     public final static String[] Policies = {
// mc++             "b2c_1_susi",
// mc++             "b2c_1_edit_profile",
// mc++             "b2c_1_reset"
// mc++     };
// mc++ 
// mc++     /**
// mc++      * Name of your B2C tenant hostname.
// mc++      */
// mc++     final static String azureAdB2CHostName = "fabrikamb2c.b2clogin.com";
// mc++ 
// mc++     /**
// mc++      * Name of your B2C tenant.
// mc++      */
// mc++     final static String tenantName = "fabrikamb2c.onmicrosoft.com";
// mc++ 
// mc++     /**
// mc++      * Returns an authority for the given policy name.
// mc++      *
// mc++      * @param policyName name of a B2C policy.
// mc++      */
// mc++     public static String getAuthorityFromPolicyName(final String policyName) {
// mc++         return "https://" + azureAdB2CHostName + "/tfp/" + tenantName + "/" + policyName + "/";
// mc++     }
// mc++ 
// mc++     /**
// mc++      * Returns an array of scopes you wish to acquire as part of the returned token result.
// mc++      * These scopes must be added in your B2C application page.
// mc++      */
// mc++     public static List<String> getScopes() {
// mc++         return Arrays.asList(
// mc++                 "https://fabrikamb2c.onmicrosoft.com/helloapi/demo.read");
// mc++     }
// mc++ }
