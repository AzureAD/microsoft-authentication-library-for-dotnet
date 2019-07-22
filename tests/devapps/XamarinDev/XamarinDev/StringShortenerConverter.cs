// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace XamarinDev
{
    internal class StringShortenerConverter : IValueConverter
    {
        internal static string GetShortStr(string str, int length)
        {
            if (str.Length > length)
            {
                return str.Substring(0, Length) + "...";
            }
            return str;
        }

        private const int Length = 30;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetShortStr((string)value, Length);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
