
using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace Xamarin_Manual.Droid
{
    //<data android:scheme="msauth"
    //             android:host="com.companyname.xamarindev"
    //             android:path="/t+Bk/nrTiK6yhmUDgd80TS5ZZT8="/>

    [Activity]
    [IntentFilter(new[] { Intent.ActionView },
       Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault },
       DataHost = AuthConfig.AndroidPackgeName,
       DataScheme = "msauth", 
       DataPath = AuthConfig.AndroidApkSignature)]
    public class MsalActivity : BrowserTabActivity
    {
    }
}
