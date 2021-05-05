// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace KerberosConsole
{
    /// <summary>
    /// Helper to provide internal tracking log for debugging.
    /// Shows the message to the console and save to internal log file under 'Documents' folder.
    /// </summary>
    public static class AADKerberosLogger
    {
        private static int _bytesPerLine = 16;
        private static int _lastFileIndex = 0;

        /// <summary>
        /// Name of log file.
        /// </summary>
        private static string _logFileName = "";

        /// <summary>
        /// Filename to save log data.
        /// </summary>
        internal static string LogFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_logFileName))
                {
                    do
                    {
                        _lastFileIndex++;

                        _logFileName = String.Format(
                            CultureInfo.InvariantCulture,
                            @"{0}\AzureAD-MSAL-{1}-{2}.log",
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            _lastFileIndex);
                    } while (File.Exists(_logFileName));
                }
                return _logFileName;
            }
        }

        public static bool SkipLoggingToConsole
        {
            get;
            set;
        } = false;

        /// <summary>
        /// Save the given formatted log text to log file.
        /// </summary>
        /// <param name="format">The message formatter.</param>
        /// <param name="args">The message format variables.</param>
        public static void Format(string format, params object[] args)
        {
            var entry = string.Format(CultureInfo.InvariantCulture, format, args);
            Save(entry);
        }

        /// <summary>
        /// Save the given log text to log file.
        /// </summary>
        /// <param name="logText">Log message to be saved.</param>
        public static void Save(string logText)
        {
            if (!SkipLoggingToConsole)
            {
                Console.WriteLine("[AADKerberos] " + logText);
            }

            File.AppendAllText(LogFileName, logText + Environment.NewLine);
        }

        /// <summary>
        /// Output given enumber of empty lines.
        /// </summary>
        /// <param name="numLines">Number of empty lines to display.</param>
        public static void PrintLines(int numLines = 1)
        {
            if (numLines > 0)
            {
                string message = "";
                for (int i = 0; i < numLines; i++)
                {
                    message += Environment.NewLine;
                }

                if (!SkipLoggingToConsole)
                {
                    Console.Write(message);
                }

                File.AppendAllText(LogFileName, message);
            }
        }

        /// <summary>
        /// Save the given log text to log file with timestamp.
        /// </summary>
        /// <param name="logText">Log message to be saved.</param>
        public static void SaveWithTimestamp(string logText)
        {
            string logMessage = DateTime.Now.ToString("s", CultureInfo.InvariantCulture) + ": " + logText;

            if (!SkipLoggingToConsole)
            {
                Console.WriteLine("[AADKerberos] " + logMessage);
            }

            File.AppendAllText(
                LogFileName,
                Environment.NewLine + logMessage + Environment.NewLine);
        }

        /// <summary>
        /// Convert given string table as Loggable text.
        /// </summary>
        /// <param name="dict">String table to be converted.</param>
        /// <returns>Loggable test string.</returns>
        public static string ToString(IDictionary<string, string> dict)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string key in dict.Keys)
            {
                sb.Append("\n        " + key)
                    .Append(": ")
                    .Append(dict[key]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Convert given string list as Loggable text.
        /// </summary>
        /// <param name="list">String list to be converted.</param>
        /// <returns>Loggable test string.</returns>
        public static string ToString(IEnumerable<string> list)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string str in list)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(str);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Shows given binary data with HEX display format as following format:
        ///     1C 4B 45 52 42 45 52 4F 53 2E 4D 49 43 52 4F 53 *KERBEROS.MICROS
        /// </summary>
        /// <param name="dataToPrint">Array of binary data to be displayed.</param>
        public static void PrintBinaryData(byte[] dataToPrint)
        {
            char[] line = new char[256];
            int charIndexBegin = _bytesPerLine * 3;
            int endOfLine = charIndexBegin + _bytesPerLine;

            for (int i = 0; i < dataToPrint.Length; i += _bytesPerLine)
            {
                int index = 0, charindex = charIndexBegin;
                Array.Fill(line, ' ');

                for (int j = 0; (i + j) < dataToPrint.Length && j < _bytesPerLine; j++)
                {
                    string hex = dataToPrint[i + j].ToString("X2", CultureInfo.InvariantCulture);
                    line[index] = hex[0];
                    line[index + 1] = hex[1];

                    if (dataToPrint[i + j] >= 32 && dataToPrint[i + j] < 127)
                    {
                        line[charindex] = (char)dataToPrint[i + j];
                    }
                    else
                    {
                        line[charindex] = '*';
                    }

                    charindex++;
                    index += 3;
                }

                line[endOfLine] = (char) 0;
                Save(new string(line, 0, _bytesPerLine * 4));
            }
        }
    }
}
