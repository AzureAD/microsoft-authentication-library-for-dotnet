//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.IO;

namespace Test.ADAL.NET.Friend
{
    class RecorderDateTimeHelper : IDateTimeHelper
    {
        private const string DateTimeFilename = @"recorded_datetime.dat";

        private readonly string dateTimeFilePath;

        private readonly DateTime recordedDateTime;
        public RecorderDateTimeHelper()
        {
            dateTimeFilePath = RecorderSettings.Path + DateTimeFilename;
            if (RecorderSettings.Mode == RecorderMode.Replay && File.Exists(dateTimeFilePath))
            {
                using (var stream = File.OpenRead(dateTimeFilePath))
                {
                    this.recordedDateTime = SerializationHelper.DeserializeDateTime(SerializationHelper.StreamToString(stream));
                }
            }
            else
            {
                recordedDateTime = (new DefaultDateTimeHelper()).UtcNow;
            }
        }

        public DateTime UtcNow
        {
            get
            {
                return recordedDateTime;
            }
        }

        public void WriteToFile()
        {
            using (var stream = File.Create(dateTimeFilePath))
            {
                SerializationHelper.StringToStream(SerializationHelper.SerializeDateTime(recordedDateTime), stream);
            }
        }
    }
}
