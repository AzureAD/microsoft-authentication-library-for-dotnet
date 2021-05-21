namespace Com.Microsoft.Identity.Client {

	partial class MultipleAccountPublicClientApplication {
		public void GetAccounts(IPublicClientApplicationLoadAccountsCallback value)
		{
			GetAccounts ((global::LoadAccountsFix.LoadAccountsCallback) value);
		}
	}
}