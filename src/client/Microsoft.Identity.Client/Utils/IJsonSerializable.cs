using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Utils
{
    internal interface IJsonSerializable<T>
    {
        T FromJsonString(string json);
        string ToJsonString(T objectToSerialize);
    }
}
