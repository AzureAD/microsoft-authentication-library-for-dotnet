using System;
using Android.Runtime;

/*
namespace Com.Microsoft.Identity.Client
{
	
	public partial interface ISingleAccountPublicClientApplication
	{
		// Metadata.xml XPath method reference: path="/api/package[@name='com.microsoft.identity.client']/interface[@name='ISingleAccountPublicClientApplication']/method[@name='signIn' and count(parameter)=4 and parameter[1][@type='android.app.Activity'] and parameter[2][@type='java.lang.String'] and parameter[3][@type='java.lang.String[]'] and parameter[4][@type='com.microsoft.identity.client.AuthenticationCallback']]"
		[Register("signIn", "(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/AuthenticationCallback;)V", "GetSignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_Handler:Com.Microsoft.Identity.Client.ISingleAccountPublicClientApplicationInvoker, Microsoft.Identity.Client")]
		void SignIn(global::Android.App.Activity activity, string loginHint, string[] scopes, global::Com.Microsoft.Identity.Client.IAuthenticationCallback @callback);

		// Metadata.xml XPath method reference: path="/api/package[@name='com.microsoft.identity.client']/interface[@name='ISingleAccountPublicClientApplication']/method[@name='signIn' and count(parameter)=5 and parameter[1][@type='android.app.Activity'] and parameter[2][@type='java.lang.String'] and parameter[3][@type='java.lang.String[]'] and parameter[4][@type='com.microsoft.identity.client.Prompt'] and parameter[5][@type='com.microsoft.identity.client.AuthenticationCallback']]"
		[Register("signIn", "(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/Prompt;Lcom/microsoft/identity/client/AuthenticationCallback;)V", "GetSignIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_Handler:Com.Microsoft.Identity.Client.ISingleAccountPublicClientApplicationInvoker, Microsoft.Identity.Client")]
		void SignIn(global::Android.App.Activity activity, string loginHint, string[] scopes, global::Com.Microsoft.Identity.Client.Prompt prompt, global::Com.Microsoft.Identity.Client.IAuthenticationCallback @callback);
	}

	internal partial class ISingleAccountPublicClientApplicationInvoker
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
			var __this = global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.ISingleAccountPublicClientApplication>(jnienv, native__this, JniHandleOwnership.DoNotTransfer);
			var activity = global::Java.Lang.Object.GetObject<global::Android.App.Activity>(native_activity, JniHandleOwnership.DoNotTransfer);
			var loginHint = JNIEnv.GetString(native_loginHint, JniHandleOwnership.DoNotTransfer);
			var scopes = (string[])JNIEnv.GetArray(native_scopes, JniHandleOwnership.DoNotTransfer, typeof(string));
			var @callback = (global::Com.Microsoft.Identity.Client.IAuthenticationCallback)global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.IAuthenticationCallback>(native__callback, JniHandleOwnership.DoNotTransfer);
			__this.SignIn(activity, loginHint, scopes, @callback);
			if (scopes != null)
				JNIEnv.CopyArray(scopes, native_scopes);
		}
#pragma warning restore 0169

		IntPtr id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_;
		public unsafe void SignIn(global::Android.App.Activity activity, string loginHint, string[] scopes, global::Com.Microsoft.Identity.Client.IAuthenticationCallback @callback)
		{
			Android.Util.Log.Info("MSAL", "ISingleAccountPublicClientApplicationInvoker.SignIn");

			if (id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_ == IntPtr.Zero)
				id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_ = JNIEnv.GetMethodID(class_ref, "signIn", "(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/AuthenticationCallback;)V");
			IntPtr native_loginHint = JNIEnv.NewString(loginHint);
			IntPtr native_scopes = JNIEnv.NewArray(scopes);
			JValue* __args = stackalloc JValue[4];
			__args[0] = new JValue((activity == null) ? IntPtr.Zero : ((global::Java.Lang.Object)activity).Handle);
			__args[1] = new JValue(native_loginHint);
			__args[2] = new JValue(native_scopes);
			__args[3] = new JValue((@callback == null) ? IntPtr.Zero : ((global::Java.Lang.Object)@callback).Handle);
			JNIEnv.CallVoidMethod(((global::Java.Lang.Object)this).Handle, id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_AuthenticationCallback_, __args);
			JNIEnv.DeleteLocalRef(native_loginHint);
			if (scopes != null)
			{
				JNIEnv.CopyArray(native_scopes, scopes);
				JNIEnv.DeleteLocalRef(native_scopes);
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
			var __this = global::Java.Lang.Object.GetObject<global::Com.Microsoft.Identity.Client.ISingleAccountPublicClientApplication>(jnienv, native__this, JniHandleOwnership.DoNotTransfer);
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

		IntPtr id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_;
		public unsafe void SignIn(global::Android.App.Activity activity, string loginHint, string[] scopes, global::Com.Microsoft.Identity.Client.Prompt prompt, global::Com.Microsoft.Identity.Client.IAuthenticationCallback @callback)
		{
			Android.Util.Log.Info("MSAL", "ISingleAccountPublicClientApplicationInvoker.SignIn");

			if (id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_ == IntPtr.Zero)
				id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_ = JNIEnv.GetMethodID(class_ref, "signIn", "(Landroid/app/Activity;Ljava/lang/String;[Ljava/lang/String;Lcom/microsoft/identity/client/Prompt;Lcom/microsoft/identity/client/AuthenticationCallback;)V");
			IntPtr native_loginHint = JNIEnv.NewString(loginHint);
			IntPtr native_scopes = JNIEnv.NewArray(scopes);
			JValue* __args = stackalloc JValue[5];
			__args[0] = new JValue((activity == null) ? IntPtr.Zero : ((global::Java.Lang.Object)activity).Handle);
			__args[1] = new JValue(native_loginHint);
			__args[2] = new JValue(native_scopes);
			__args[3] = new JValue((prompt == null) ? IntPtr.Zero : ((global::Java.Lang.Object)prompt).Handle);
			__args[4] = new JValue((@callback == null) ? IntPtr.Zero : ((global::Java.Lang.Object)@callback).Handle);
			JNIEnv.CallVoidMethod(((global::Java.Lang.Object)this).Handle, id_signIn_Landroid_app_Activity_Ljava_lang_String_arrayLjava_lang_String_Lcom_microsoft_identity_client_Prompt_Lcom_microsoft_identity_client_AuthenticationCallback_, __args);
			JNIEnv.DeleteLocalRef(native_loginHint);
			if (scopes != null)
			{
				JNIEnv.CopyArray(native_scopes, scopes);
				JNIEnv.DeleteLocalRef(native_scopes);
			}
		}
	}
}
*/