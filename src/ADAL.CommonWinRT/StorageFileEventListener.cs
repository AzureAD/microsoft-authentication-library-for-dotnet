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
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Microsoft.IdentityModel.Clients.ActiveDirectory
{
    /// <summary>
    /// Code used from http://code.msdn.microsoft.com/Logging-Sample-for-Windows-0b9dffd7
    /// This is an advanced usage, where you want to intercept the logging messages and divert them somewhere
    /// besides ETW.
    /// </summary>
    sealed class StorageFileEventListener : EventListener
    {
        /// <summary>
        /// Storage file to be used to write logs
        /// </summary>
        private StorageFile storageFile;

        /// <summary>
        /// Name of the current event listener
        /// </summary>
        private readonly string name;

        /// <summary>
        /// The format to be used by logging.
        /// </summary>
        private string format = "{0:yyyy-MM-dd HH\\:mm\\:ss\\:ffff}\tType: {1}\tId: {2}\tMessage: '{3}'";

        private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);

        public StorageFileEventListener(string name)
        {
            this.name = name;
            Debug.WriteLine("StorageFileEventListener for {0} has name {1}", GetHashCode(), name);
            AssignLocalFile();
        }

        private async void AssignLocalFile()
        {
            storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
        }

        private async void WriteToFile(IEnumerable<string> lines)
        {
            await semaphoreSlim.WaitAsync();

            await Task.Run(async () =>
            {
                try
                {
                    await FileIO.AppendLinesAsync(storageFile, lines);
                }
                catch (Exception ex)
                {
                    Logger.Warning(null, ex.Message);
                }
                finally
                {
                    semaphoreSlim.Release();
                }
            });
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (storageFile == null) return;

            var lines = new List<string>();

            var newFormatedLine = string.Format(format, DateTime.Now, eventData.Level, eventData.EventId, eventData.Payload[0]);

            Debug.WriteLine(newFormatedLine);

            lines.Add(newFormatedLine);

            WriteToFile(lines);
        }
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            Debug.WriteLine("OnEventSourceCreated for Listener {0} - {1} got eventSource {2}", GetHashCode(), name, eventSource.Name);
        }
    }
}
