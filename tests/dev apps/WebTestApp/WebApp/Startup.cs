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
using WebApp.Utils;

namespace WebApp
{
    public class Startup
    {
        public static string ClientId;
        public static string ClientSecret;
        public static string Authority;

        public static string GraphResourceId;
        public static string TodoListResourceId;

        public static string[] Scopes = { "User.Read" };

        public static string CommonAuthority = "https://login.microsoftonline.com/common";
        public static string CommonAuthority2 = "https://login.microsoftonline.com/common/v2.0";


        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .Build();
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

            // Populate AzureAd Configuration Values
            //Authority = String.Format(Configuration["AzureAd:AadInstance"], Configuration["AzureAd:Tenant"]);

            Authority = string.Format(CultureInfo.InvariantCulture, Configuration["AzureAd:AadInstance1"], Configuration["AzureAd:Tenant"], "/v2.0");


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
                Authority = Authority,
                PostLogoutRedirectUri = Configuration["AzureAd:PostLogoutRedirectUri"],
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                GetClaimsFromUserInfoEndpoint = false,
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = OnAuthenticationFailed,
                    OnAuthorizationCodeReceived = OnAuthorizationCodeReceived
                }
            };
            authOptions.Scope.Add("User.Read");
            authOptions.Scope.Add("offline_access");
              authOptions.Scope.Add("openid");
            authOptions.Scope.Add("profile");

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
            // Acquire a Token for the Graph API and cache it using MSAL.  
            var authenticationResult = await ConfidentialClientUtils.AcquireTokenByAuthorizationCodeAsync(context.ProtocolMessage.Code,
                Scopes, context.HttpContext.Session);

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
