//----------------------------------------------------------------------
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
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;
using System.Linq;
using Microsoft.IdentityModel.Tokens;
using WebApp.Utils;

namespace WebApp
{
    public class Startup
    {
        public static string ClientId;
        public static string ClientSecret;

        public static string[] Scopes = { "User.Read" };

        public static string WebApiScope = "api://2878ab18-1738-4a30-96f3-085df7ed4a70/access_as_user";

        public static string GraphResourceId;
        public static string TodoListResourceId;

        public static Dictionary<string, string> ExtraParamsDictionary = new Dictionary<string, string>();

        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .Build();

            InitExtraParameters();
        }

        private static void InitExtraParameters()
        {
            ExtraParamsDictionary.Add("slice", "testslice");
            ExtraParamsDictionary.Add("combineddelegationquery", "true");
            ExtraParamsDictionary.Add("dc", "prod-wst-test");
        }

        public static IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            // Add session services.
            services.AddSession();

            // Add Authentication services.
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Add the console logger.
            loggerFactory.AddConsole(LogLevel.Debug);

            // Configure error handling middleware.
            app.UseExceptionHandler("/Home/Error");
            app.UseDeveloperExceptionPage();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Configure session middleware.
            app.UseSession();

            ClientId = Configuration["AzureAd:ClientId"];
            ClientSecret = Configuration["AzureAd:ClientSecret"];
            GraphResourceId = Configuration["AzureAd:GraphResourceId"];
            TodoListResourceId = Configuration["AzureAd:TodoListResourceId"];

            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            // Configure the OWIN pipeline to use OpenID Connect auth.
            var authOptions = new OpenIdConnectOptions
            {
                AutomaticChallenge = true,
                ClientId = ClientId,
                Authority = Configuration["AzureAd:CommonAuthority"],
                PostLogoutRedirectUri = Configuration["AzureAd:PostLogoutRedirectUri"],
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                GetClaimsFromUserInfoEndpoint = false,
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = OnAuthenticationFailed,
                    OnAuthorizationCodeReceived = OnAuthorizationCodeReceived,
                    OnRedirectToIdentityProvider = context =>
                    {
                        foreach (var entry in ExtraParamsDictionary)
                        {
                            context.ProtocolMessage.SetParameter(entry.Key, entry.Value);
                        }

                        return Task.FromResult(0);
                    }
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                    // we inject our own multitenant validation logic
                    ValidateIssuer = false
                },
            };

            authOptions.Scope.Add("User.Read");
            authOptions.Scope.Add("offline_access");
            authOptions.Scope.Add(WebApiScope);

            app.UseOpenIdConnectAuthentication(authOptions);

            // Configure MVC routes
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private async Task OnAuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            string[] scopes = { "User.Read" };

            var userId =  context.JwtSecurityToken != null ? context.JwtSecurityToken.Claims.FirstOrDefault(claim => claim.Type == "oid").Value : "";
            // Acquire a Token for the Graph API and cache it using MSAL.  
            var authenticationResult = await ConfidentialClientUtils.AcquireTokenByAuthorizationCodeAsync(context.ProtocolMessage.Code,
                scopes, context.HttpContext.Session, ConfidentialClientUtils.CreateSecretClientCredential(), userId);

            // Notify the OIDC middleware that we already took care of code redemption.
            context.HandleCodeRedemption(authenticationResult.AccessToken, authenticationResult.IdToken);
        }
        
 
        // Handle sign-in errors differently than generic errors.
        private Task OnAuthenticationFailed(FailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
            return Task.FromResult(0);
        }
    }
}
