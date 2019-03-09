// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Identity.Client.AppConfig
{
    /// <summary>
    /// </summary>
    public interface IMatsTelemetryBatch
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        string GetName();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int GetStringRowCount();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string GetStringKey(int index);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string GetStringValue(int index);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int GetIntRowCount();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string GetIntKey(int index);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        int GetIntValue(int index);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int GetInt64RowCount();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string GetInt64Key(int index);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        long GetInt64Value(int index);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        int GetBoolRowCount();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        string GetBoolKey(int index);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        bool GetBoolValue(int index);
    }
}
