// ------------------------------------------------------------------------------
// 
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
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WebAppCore.Utils;

namespace WebAppCore
{
    public class Startup
    {
        public static string WebApiScope = "api://2878ab18-1738-4a30-96f3-085df7ed4a70/access_as_user";
        public static Dictionary<string, string> ExtraParamsDictionary = new Dictionary<string, string>();

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            InitExtraParameters();
        }

        public static IConfiguration Configuration { get; set; }
        public static string ClientId { get; private set; }

        private static void InitExtraParameters()
        {
            ExtraParamsDictionary.Add("slice", "testslice");
            ExtraParamsDictionary.Add("combineddelegationquery", "true");
            ExtraParamsDictionary.Add("dc", "prod-wst-test");
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(
                options =>
                {
                    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                    options.CheckConsentNeeded = context => true;
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            ClientId = Configuration["AzureAd:ClientId"];

            services.AddAuthentication(
                sharedOptions =>
                {
                    sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                }).AddOpenIdConnect(
                options =>
                {
                    options.ClientId = ClientId;
                    options.Authority = Configuration["AzureAd:CommonAuthority"];
                    options.SignedOutRedirectUri = Configuration["AzureAd:SignedOutRedirectUri"];
                    options.ResponseType = OpenIdConnectResponseType.CodeIdToken;
                    options.GetClaimsFromUserInfoEndpoint = false;
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRemoteFailure = OnAuthenticationFailedAsync,
                        OnAuthorizationCodeReceived = OnAuthorizationCodeReceivedAsync,
                        OnRedirectToIdentityProvider = context =>
                        {
                            foreach (KeyValuePair<string, string> entry in ExtraParamsDictionary)
                            {
                                context.ProtocolMessage.SetParameter(entry.Key, entry.Value);
                            }

                            return Task.FromResult(0);
                        }
                    };
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                        // we inject our own multi-tenant validation logic
                        ValidateIssuer = false
                    };
                    options.Scope.Add("User.Read");
                    options.Scope.Add("offline_access");
                    options.Scope.Add(WebApiScope);
                }).AddCookie();

            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache();
            services.AddSession(
                options =>
                {
                    options.IdleTimeout = TimeSpan.FromHours(1);
                    options.Cookie.HttpOnly = true;
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseSession();
            app.UseAuthentication();

            app.UseMvc(routes => { routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}"); });
        }

        private async Task OnAuthorizationCodeReceivedAsync(AuthorizationCodeReceivedContext context)
        {
            string[] scopes =
            {
                "User.Read"
            };

            string userId = context.JwtSecurityToken != null
                                ? context.JwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == "oid").Value
                                : "";
            // Acquire a Token for the Graph API and cache it using MSAL.  
            var authenticationResult = await ConfidentialClientUtils.AcquireTokenByAuthorizationCodeAsync(
                                           context.ProtocolMessage.Code,
                                           scopes,
                                           context.HttpContext.Session,
                                           ConfidentialClientUtils.CreateSecretClientCredential(),
                                           userId).ConfigureAwait(false);

            // Notify the OIDC middleware that we already took care of code redemption.
            context.HandleCodeRedemption(authenticationResult.AccessToken, authenticationResult.IdToken);
        }

        // Handle sign-in errors differently than generic errors.
        private Task OnAuthenticationFailedAsync(RemoteFailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
            return Task.FromResult(0);
        }
    }
}