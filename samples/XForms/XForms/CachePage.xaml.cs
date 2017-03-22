using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace XForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CachePage : ContentPage
    {
        public CachePage()
        {
            InitializeComponent();

           // IEnumerable<FieldInfo> fields = App.PCA.GetType().GetRuntimeFields();
        }

        /// <summary>
        /// Uses reflection to get the field value from an object.
        /// </summary>
        ///
        /// <param name="type">The instance type.</param>
        /// <param name="instance">The instance object.</param>
        /// <param name="fieldName">The field's name which is to be fetched.</param>
        ///
        /// <returns>The field value from the object.</returns>
        /*	public static object GetInstanceField(Type type, object instance, string fieldName)
            {
                BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Static;
                FieldInfo field = type.GetField(fieldName, bindFlags);
                return field.GetValue(instance);
            }*/


        protected override void OnAppearing()
        {
            DateTime dateTime = DateTime.UtcNow;
            cache.Text = dateTime.ToString();

            //TokenCache _tokenCache = GetInstanceField(typeof(PublicClientApplication), App.PCA, "UserTokenCache") as TokenCache;

            //TypeInfo typeInfo = App.PCA.GetType().GetTypeInfo();

            //FieldInfo fi = App.PCA.GetType().get

            /*     FieldInfo inf = typeof(PublicClientApplication).GetTypeInfo().GetDeclaredField("UserTokenCache");


                 IEnumerable<FieldInfo> fields = App.PCA.GetType().GetTypeInfo().BaseType.GetRuntimeFields();

                 foreach (var item in fields)
                 {
                     if (item.Name.StartsWith("<UserTokenCache>"))
                     {
                         TokenCache val = item.GetValue(App.PCA) as TokenCache;

                         MethodInfo m = val.GetType().GetTypeInfo().GetDeclaredMethod("GetAllAccessTokenCacheItems");

                         m.Invoke(val, new object[] { });
                        //cache.Text = cache.Text + System.Environment.NewLine + item.Name;

                     }
                     //cache.Text = _tokenCache.toString();
                 }*/
        }
    }
}

