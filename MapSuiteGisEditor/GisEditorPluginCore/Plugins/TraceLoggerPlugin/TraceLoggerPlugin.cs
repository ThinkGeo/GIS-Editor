/*
* Licensed to the Apache Software Foundation (ASF) under one
* or more contributor license agreements.  See the NOTICE file
* distributed with this work for additional information
* regarding copyright ownership.  The ASF licenses this file
* to you under the Apache License, Version 2.0 (the
* "License"); you may not use this file except in compliance
* with the License.  You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/


using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public abstract class TraceLoggerPlugin : LoggerPlugin
    {
        public TraceLoggerPlugin()
        {
            Name = "Trace";
        }

        protected override void LogCore(LoggerMessage loggerMessage)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(DateTime.Now.ToLongTimeString() + "\t|");
            stringBuilder.Append(loggerMessage.Message + "\t|");
            stringBuilder.Append(loggerMessage.LoggerLevel + "\t|");
            foreach (var item in loggerMessage.CustomData)
            {
                stringBuilder.Append(string.Format(CultureInfo.InvariantCulture, "{0} is {1}", item.Key, item.Value));
            }

            Trace.WriteLine(stringBuilder.ToString());
        }

        protected override Collection<LoggerMessage> GetLoggerMessagesCore(DateTime fromDate, DateTime toDate)
        {
            Collection<LoggerMessage> loggerMessages = new Collection<LoggerMessage>();
            return loggerMessages;
        }
    }
}