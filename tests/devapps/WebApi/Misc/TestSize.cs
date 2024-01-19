// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WebApi.Misc
{
    class TestSize<T>
    {
        // as per https://github.com/CyberSaving/MemoryUsage/blob/master/Main/Program.cs
        static private int SizeOfObj(Type T, object thevalue)
        {
            var type = T;
            int returnval = 0;
            if (type.IsValueType)
            {
                var nulltype = Nullable.GetUnderlyingType(type);
                returnval = System.Runtime.InteropServices.Marshal.SizeOf(nulltype ?? type);
            }
            else if (thevalue == null)
                return 0;
            else if (thevalue is string stringValue)
                returnval = Encoding.Default.GetByteCount(stringValue);
            else if (type.IsArray && type.GetElementType().IsValueType)
            {
                returnval = ((Array)thevalue).GetLength(0) * System.Runtime.InteropServices.Marshal.SizeOf(type.GetElementType());
            }
            else if (thevalue is Stream streamValue)
            {
                returnval = (int)streamValue.Length;
            }
            else if (type.IsSerializable)
            {
                try
                {
                    using (Stream s = new MemoryStream())
                    {
                        BinaryFormatter formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
                        formatter.Serialize(s, thevalue);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
                        returnval = (int)s.Length;
                    }
                }
                catch { }
            }
            else
            {
                var fields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                for (int i = 0; i < fields.Length; i++)
                {
                    Type t = fields[i].FieldType;
                    Object v = fields[i].GetValue(thevalue);
                    returnval += 4 + SizeOfObj(t, v);
                }
            }
            if (returnval == 0)
                try
                {
                    returnval = System.Runtime.InteropServices.Marshal.SizeOf(thevalue);
                }
                catch { }
            return returnval;
        }
        static public int SizeOf(T value)
        {
            return SizeOfObj(typeof(T), value);
        }
    }
}
