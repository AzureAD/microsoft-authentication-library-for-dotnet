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
// mc++ import android.content.Context;
// mc++ import android.util.Log;
// mc++ 
// mc++ import androidx.annotation.NonNull;
// mc++ 
// mc++ import com.android.volley.DefaultRetryPolicy;
// mc++ import com.android.volley.Request;
// mc++ import com.android.volley.RequestQueue;
// mc++ import com.android.volley.Response;
// mc++ import com.android.volley.toolbox.JsonObjectRequest;
// mc++ import com.android.volley.toolbox.Volley;
// mc++ 
// mc++ import org.json.JSONObject;
// mc++ 
// mc++ import java.util.HashMap;
// mc++ import java.util.Map;
// mc++ 
// mc++ public class MSGraphRequestWrapper {
// mc++     private static final String TAG = MSGraphRequestWrapper.class.getSimpleName();
// mc++ 
// mc++     // See: https://docs.microsoft.com/en-us/graph/deployments#microsoft-graph-and-graph-explorer-service-root-endpoints
// mc++     public static final String MS_GRAPH_ROOT_ENDPOINT = "https://graph.microsoft.com/";
// mc++ 
// mc++     /**
// mc++      * Use Volley to make an HTTP request with
// mc++      * 1) a given MSGraph resource URL
// mc++      * 2) an access token
// mc++      * to obtain MSGraph data.
// mc++      **/
// mc++     public static void callGraphAPIUsingVolley(@NonNull final Context context,
// mc++                                                @NonNull final String graphResourceUrl,
// mc++                                                @NonNull final String accessToken,
// mc++                                                @NonNull final Response.Listener<JSONObject> responseListener,
// mc++                                                @NonNull final Response.ErrorListener errorListener) {
// mc++         Log.d(TAG, "Starting volley request to graph");
// mc++ 
// mc++         /* Make sure we have a token to send to graph */
// mc++         if (accessToken == null || accessToken.length() == 0) {
// mc++             return;
// mc++         }
// mc++ 
// mc++         RequestQueue queue = Volley.newRequestQueue(context);
// mc++         JSONObject parameters = new JSONObject();
// mc++ 
// mc++         try {
// mc++             parameters.put("key", "value");
// mc++         } catch (Exception e) {
// mc++             Log.d(TAG, "Failed to put parameters: " + e.toString());
// mc++         }
// mc++ 
// mc++         JsonObjectRequest request = new JsonObjectRequest(Request.Method.GET, graphResourceUrl,
// mc++                 parameters, responseListener, errorListener) {
// mc++             @Override
// mc++             public Map<String, String> getHeaders() {
// mc++                 Map<String, String> headers = new HashMap<>();
// mc++                 headers.put("Authorization", "Bearer " + accessToken);
// mc++                 return headers;
// mc++             }
// mc++         };
// mc++ 
// mc++         Log.d(TAG, "Adding HTTP GET to Queue, Request: " + request.toString());
// mc++ 
// mc++         request.setRetryPolicy(new DefaultRetryPolicy(
// mc++                 3000,
// mc++                 DefaultRetryPolicy.DEFAULT_MAX_RETRIES,
// mc++                 DefaultRetryPolicy.DEFAULT_BACKOFF_MULT));
// mc++         queue.add(request);
// mc++     }
// mc++ }
