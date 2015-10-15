/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    class ApplicationReceiver : BroadcastReceiver
    {
        private const String TAG = "ApplicationReceiver";
    public const String INSTALL_REQUEST_TRACK_FILE = "adal.broker.install.track";
    public const String INSTALL_REQUEST_KEY = "adal.broker.install.request";
    private const String INSTALL_UPN_KEY = "username";
    public const String INSTALL_URL_KEY = "app_link";

        public override void OnReceive(Context context, Intent intent)
        {        // Check if the application is install and belongs to the broker package
            if (intent.Action.Equals(Intent.ActionPackageAdded))
            {
                PlatformPlugin.Logger.Verbose(null, TAG + " - Application install message is received");
                if (intent != null && intent.Data != null)
                {
                    PlatformPlugin.Logger.Verbose(null, TAG + " - Installing:" + intent.Data.ToString());
                    if (intent.Data
                            .ToString()
                            .Equals(
                                    "package:" + BrokerConstants.AzureAuthenticatorAppPackageName, StringComparison.OrdinalIgnoreCase))
                    {
                        PlatformPlugin.Logger.Verbose(null, TAG + " - Message is related to the broker");
                       // string request = GetInstallRequestInthisApp(context);
/*                        if (!StringExtensions.IsNullOrBlank(request))
                        {
                            PlatformPlugin.Logger.Verbose(null, TAG + " - Resume request in broker");
                            ResumeRequestInBroker(context, request);
                        }#1#
                    }
                }
            }
        }

        private void resumeRequestInBroker(Context ctx, String request)
        {
            PlatformPlugin.Logger.Verbose(null, TAG + " - ApplicationReceiver:resumeRequestInBroker");
            Gson gson = new Gson();
            AuthenticationRequest pendingRequest = gson.fromJson(request, AuthenticationRequest.class);
        Intent intent = new Intent();
        intent.setAction(Intent.ACTION_PICK);
        intent.putExtra(AuthenticationConstants.Broker.BrokerRequest, pendingRequest);
        intent.putExtra(AuthenticationConstants.Broker.CallerInfoPackage, ctx.getPackageName());
        intent.putExtra(AuthenticationConstants.Broker.BrokerRequestResume,
                AuthenticationConstants.Broker.BrokerRequestResume);
        intent.setPackage(AuthenticationSettings.INSTANCE.getBrokerPackageName());
        intent.setClassName(AuthenticationSettings.INSTANCE.getBrokerPackageName(),
                AuthenticationSettings.INSTANCE.getBrokerPackageName()
                        + ".ui.AccountChooserActivity");

        PackageManager packageManager = ctx.getPackageManager();

        // Get activities that can handle the intent
        List<ResolveInfo> activities = packageManager.queryIntentActivities(intent, 0);

        // Check if 1 or more were returned
        boolean isIntentSafe = activities.size() > 0;

        if (isIntentSafe) {
            intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_MULTIPLE_TASK);
            ctx.startActivity(intent);
        }
}

public static String getInstallRequestInthisApp(Context ctx)
{
    Logger.v(TAG, "ApplicationReceiver:getInstallRequestInthisApp");
    SharedPreferences prefs = ctx.getSharedPreferences(INSTALL_REQUEST_TRACK_FILE,
            Activity.MODE_PRIVATE);
    if (prefs != null && prefs.contains(INSTALL_REQUEST_KEY))
    {
        String request = prefs.getString(INSTALL_REQUEST_KEY, "");
        Logger.d(TAG, "Install request:" + request);
        return request;
    }

    return "";
}

    }
}*/