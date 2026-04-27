// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.Identity.Client.Utils
{
    internal class StringWriterWithEncoding(Encoding encoding) : StringWriter(CultureInfo.InvariantCulture)
    {
        public override Encoding Encoding { get; } = encoding;
    }
}
