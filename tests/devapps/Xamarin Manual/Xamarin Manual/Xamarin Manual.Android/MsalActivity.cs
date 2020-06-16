
using Android.App;
using Android.Content;
using Microsoft.Identity.Client;

namespace Xamarin_Manual.Droid
{
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
