using Com.Microsoft.Identity.Client;
using Com.Microsoft.Identity.Client.Exception;
using Android.Runtime;

namespace LoadAccountsFix {

	public partial class LoadAccountsCallback {
		public void OnError (global::Java.Lang.Object value)
		{
			OnError (value.JavaCast<MsalException>());
		}

		public void OnTaskCompleted (global::Java.Lang.Object value)
		{
			OnTaskCompleted ((global::System.Collections.Generic.IList<global::Com.Microsoft.Identity.Client.IAccount>) value);
		}
	}
}