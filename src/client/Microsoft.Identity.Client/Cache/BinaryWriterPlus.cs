// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Identity.Client.Cache
{
    /// <summary>
    /// Ref: https://msblox-02.visualstudio.com/_git/Frugal?path=%2FFrugal%2FFrugal%2FIO%2FBinaryWriterPlus.cs
    /// </summary>
    internal class BinaryWriterPlus : BinaryWriter
    {
        private readonly Encoding encoding;

        private byte[] buffer;

        public BinaryWriterPlus(Stream output) : base(output, Encoding.Unicode)
        {
            this.encoding = Encoding.Unicode;
        }

        public override void Write(char[] chars)
        {
            if (chars == null)
            {
                throw new ArgumentNullException(nameof(chars));
            }

            this.WriteString(chars, 0, chars.Length, lengthPrefix: false);
        }

        public override void Write(char[] chars, int index, int count)
        {
            this.WriteString(chars, index, count, lengthPrefix: false);
        }

        /// <summary>
        /// Same as Write(value.Substring(start, length))
        /// </summary>
        public unsafe void WriteString(string value, int start, int length, bool lengthPrefix = true)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            RangeCheck(value.Length, start, length);

            fixed (char* chars = value)
            {
                this.WriteStringData(chars + start, length, lengthPrefix);
            }
        }

        /// <summary>
        /// Same as Write(new string(value, start, length))
        /// </summary>
        public unsafe void WriteString(char[] value, int start, int length, bool lengthPrefix = true)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            RangeCheck(value.Length, start, length);

            fixed (char* chars = value)
            {
                this.WriteStringData(chars + start, length, lengthPrefix);
            }
        }

        /// <summary>
        /// Write as string
        /// </summary>
        public void WriteString(byte[] value, int start, int length)
        {
            if (length == 0)
            {
                base.Write7BitEncodedInt(0);
            }
            else
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                RangeCheck(value.Length, start, length);

                base.Write7BitEncodedInt(length);
                base.Write(value, start, length);
            }
        }

        public unsafe void WriteStringData(char* value, int length, bool lengthPrefix = true)
        {
            int byteLength = this.encoding.GetByteCount(value, length);

            if (lengthPrefix)
            {
                base.Write7BitEncodedInt(byteLength);
            }

            if ((this.buffer == null) || (this.buffer.Length < byteLength))
            {
                this.buffer = new byte[byteLength];
            }

            fixed (byte* dst = this.buffer)
            {
                this.encoding.GetBytes(value, length, dst, 0);
            }

            base.Write(this.buffer, 0, byteLength);
        }

        public static void RangeCheck(int totalLength, int startIndex, int length)
        {
            if ((startIndex < 0) || (startIndex > totalLength))
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if ((length < 0) || startIndex > (totalLength - length))
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
        }
    }
}
