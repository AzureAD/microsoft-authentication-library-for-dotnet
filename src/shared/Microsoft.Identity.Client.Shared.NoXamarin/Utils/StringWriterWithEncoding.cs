// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Identity.Client.Utils
{
    internal class StringWriterWithEncoding : StringWriter
    {
        public StringWriterWithEncoding(Encoding encoding)
            : base(CultureInfo.InvariantCulture)
        {
            Encoding = encoding;
        }

        public override Encoding Encoding { get; }
    }
}
