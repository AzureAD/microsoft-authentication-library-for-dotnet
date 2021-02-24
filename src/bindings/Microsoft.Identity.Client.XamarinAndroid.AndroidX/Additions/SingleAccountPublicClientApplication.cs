using System;
using Android.Runtime;
using Java.Interop;

/*
namespace Com.Microsoft.Identity.Client
{
	// Metadata.xml XPath class reference: path="/api/package[@name='com.microsoft.identity.client']/class[@name='PublicClientApplication']"
	// [global::Android.Runtime.Register("com/microsoft/identity/client/PublicClientApplication", DoNotGenerateAcw = true)]
	public partial class SingleAccountPublicClientApplication
	// : global::Java.Lang.Object, global::Com.Microsoft.Identity.Client.IPublicClientApplication, global::Com.Microsoft.Identity.Client.ITokenShare
	{



		static Delegate cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_;
#pragma warning disable 0169
		static Delegate GetSignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_Handler()
		{
			if (cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_ == null)
				cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_ = JNINativeWrapper.CreateDelegate((_JniMarshal_PPLLLL_V)n_SignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_);
			return cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_;
		}

		static void n_SignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_(IntPtr jnienv, IntPtr native__this, IntPtr native_activity, IntPtr native_loginHint, IntPtr native_scopes, IntPtr native__callback)
		{
			var __this = global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.SingleAccountPublicClientApplication>(jnienv, native__this, JniHandleOwnership.DoNotTransfer);
			var activity = global::Java.Lang.Object.GetObject<global::Android.App.Activity>(native_activity, JniHandleOwnership.DoNotTransfer);
			var loginHint = JNIEnv.GetString(native_loginHint, JniHandleOwnership.DoNotTransfer);
			var scopes = (string[])JNIEnv.GetArray(native_scopes, JniHandleOwnership.DoNotTransfer, typeof(string));
			var @callback = (global::Com.Microsoft.Identity.Client.IAuthenticationCallback)global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.IAuthenticationCallback>(native__callback, JniHandleOwnership.DoNotTransfer);
			__this.SignIn(activity, loginHint, scopes, @callback);
			if (scopes != null)
				JNIEnv.CopyArray(scopes, native_scopes);
		}
#pragma warning restore 0169

		// Metadata.xml XPath method reference: path="/api/package[@name='com.microsoft.identity.client']/class[@name='SingleAccountPublicClientApplication']/method[@name='signIn' and count(parameter)=4 and parameter[1][@type='android.app.Activity'] and parameter[2][@type='java.lang.String'] and parameter[3][@type='java.lang.String[]'] and parameter[4][@type='com.microsoft.identity.client.AuthenticationCallback']]"
		[Register("signIn", "(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/AuthenticationCallback;)V", "GetSignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_Handler")]
		public virtual unsafe void SignIn(global::Android.App.Activity activity, string loginHint, string[] scopes, global::Com.Microsoft.Identity.Client.IAuthenticationCallback @callback)
		{
			Android.Util.Log.Info("MSAL", "SingleAccountPublicClientApplication.SignIn");

			const string __id = "signIn.(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/AuthenticationCallback;)V";
			IntPtr native_loginHint = JNIEnv.NewString(loginHint);
			IntPtr native_scopes = JNIEnv.NewArray(scopes);
			try
			{
				JniArgumentValue* __args = stackalloc JniArgumentValue[4];
				__args[0] = new JniArgumentValue((activity == null) ? IntPtr.Zero : ((global::Java.Lang.Object)activity).Handle);
				__args[1] = new JniArgumentValue(native_loginHint);
				__args[2] = new JniArgumentValue(native_scopes);
				__args[3] = new JniArgumentValue((@callback == null) ? IntPtr.Zero : ((global::Java.Lang.Object)@callback).Handle);
				_members.InstanceMethods.InvokeVirtualVoidMethod(__id, this, __args);
			}
			finally
			{
				JNIEnv.DeleteLocalRef(native_loginHint);
				if (scopes != null)
				{
					JNIEnv.CopyArray(native_scopes, scopes);
					JNIEnv.DeleteLocalRef(native_scopes);
				}
				global::System.GC.KeepAlive(activity);
				global::System.GC.KeepAlive(scopes);
				global::System.GC.KeepAlive(@callback);
			}
		}

		static Delegate cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_;
#pragma warning disable 0169
		static Delegate GetSignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_Handler()
		{
			if (cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_ == null)
				cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_ = JNINativeWrapper.CreateDelegate((_JniMarshal_PPLLLLL_V)n_SignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_);
			return cb_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_;
		}

		static void n_SignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_(IntPtr jnienv, IntPtr native__this, IntPtr native_activity, IntPtr native_loginHint, IntPtr native_scopes, IntPtr native_prompt, IntPtr native__callback)
		{
			var __this = global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.SingleAccountPublicClientApplication>(jnienv, native__this, JniHandleOwnership.DoNotTransfer);
			var activity = global::Java.Lang.Object.GetObject<global::Android.App.Activity>(native_activity, JniHandleOwnership.DoNotTransfer);
			var loginHint = JNIEnv.GetString(native_loginHint, JniHandleOwnership.DoNotTransfer);
			var scopes = (string[])JNIEnv.GetArray(native_scopes, JniHandleOwnership.DoNotTransfer, typeof(string));
			var prompt = global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.Prompt>(native_prompt, JniHandleOwnership.DoNotTransfer);
			var @callback = (global::Com.Microsoft.Identity.Client.IAuthenticationCallback)global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.IAuthenticationCallback>(native__callback, JniHandleOwnership.DoNotTransfer);
			__this.SignIn(activity, loginHint, scopes, prompt, @callback);
			if (scopes != null)
				JNIEnv.CopyArray(scopes, native_scopes);
		}
#pragma warning restore 0169

		// Metadata.xml XPath method reference: path="/api/package[@name='com.microsoft.identity.client']/class[@name='SingleAccountPublicClientApplication']/method[@name='signIn' and count(parameter)=5 and parameter[1][@type='android.app.Activity'] and parameter[2][@type='java.lang.String'] and parameter[3][@type='java.lang.String[]'] and parameter[4][@type='com.microsoft.identity.client.Prompt'] and parameter[5][@type='com.microsoft.identity.client.AuthenticationCallback']]"
		[Register("signIn", "(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/Prompt;Lcom/microsoft/identity/client/AuthenticationCallback;)V", "GetSignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_Handler")]
		public virtual unsafe void SignIn(global::Android.App.Activity activity, string loginHint, string[] scopes, global::Com.Microsoft.Identity.Client.Prompt prompt, global::Com.Microsoft.Identity.Client.IAuthenticationCallback @callback)
		{
			Android.Util.Log.Info("MSAL", "SingleAccountPublicClientApplication.SignIn");

			const string __id = "signIn.(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/Prompt;Lcom/microsoft/identity/client/AuthenticationCallback;)V";
			IntPtr native_loginHint = JNIEnv.NewString(loginHint);
			IntPtr native_scopes = JNIEnv.NewArray(scopes);
			try
			{
				JniArgumentValue* __args = stackalloc JniArgumentValue[5];
				__args[0] = new JniArgumentValue((activity == null) ? IntPtr.Zero : ((global::Java.Lang.Object)activity).Handle);
				__args[1] = new JniArgumentValue(native_loginHint);
				__args[2] = new JniArgumentValue(native_scopes);
				__args[3] = new JniArgumentValue((prompt == null) ? IntPtr.Zero : ((global::Java.Lang.Object)prompt).Handle);
				__args[4] = new JniArgumentValue((@callback == null) ? IntPtr.Zero : ((global::Java.Lang.Object)@callback).Handle);
				_members.InstanceMethods.InvokeVirtualVoidMethod(__id, this, __args);
			}
			finally
			{
				JNIEnv.DeleteLocalRef(native_loginHint);
				if (scopes != null)
				{
					JNIEnv.CopyArray(native_scopes, scopes);
					JNIEnv.DeleteLocalRef(native_scopes);
				}
				global::System.GC.KeepAlive(activity);
				global::System.GC.KeepAlive(scopes);
				global::System.GC.KeepAlive(prompt);
				global::System.GC.KeepAlive(@callback);
			}
		}
	}

}
*/