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
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class LoggerMessage
    {
        private static readonly string format = "{0} is {1}";

        internal static readonly string DefaultCategory = "Default";
        private string message;
        private string category;
        private ExceptionInfo error;
        private LoggerLevel loggerLevel;
        private DateTime dateTime;
        private Dictionary<string, string> customData;
        private TimeSpan elapsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessage" /> class.
        /// </summary>
        public LoggerMessage()
            : this(LoggerLevel.Information, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessage" /> class.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        public LoggerMessage(LoggerLevel loggerLevel, string message)
            : this(loggerLevel, message, DefaultCategory)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessage" /> class.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        public LoggerMessage(LoggerLevel loggerLevel, string message, string category)
            : this(loggerLevel, message, category, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessage" /> class.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        public LoggerMessage(LoggerLevel loggerLevel, string message, ExceptionInfo error)
            : this(loggerLevel, message, DefaultCategory, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerMessage" /> class.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public LoggerMessage(LoggerLevel loggerLevel, string message, string category, ExceptionInfo error)
        {
            Category = category;
            LoggerLevel = loggerLevel;
            Message = message;
            Error = error;
            DateTime = DateTime.Now;
            customData = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the custom data.
        /// </summary>
        /// <value>
        /// The custom data.
        /// </value>
        public Dictionary<string, string> CustomData
        {
            get { return customData; }
            set { customData = value; }
        }

        /// <summary>
        /// Gets or sets the date time.
        /// </summary>
        /// <value>
        /// The date time.
        /// </value>
        public DateTime DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        /// <summary>
        /// Gets or sets the logger level.
        /// </summary>
        /// <value>
        /// The logger level.
        /// </value>
        public LoggerLevel LoggerLevel
        {
            get { return loggerLevel; }
            set { loggerLevel = value; }
        }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public ExceptionInfo Error
        {
            get { return error; }
            set { error = value; }
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public string Category
        {
            get { return category; }
            set { category = value; }
        }
        
        public TimeSpan Elapsed
        {
            get { return elapsed; }
            set { elapsed = value; }
        }

        public override string ToString()
        {
            string customData = string.Empty;
            if (CustomData.Count > 0)
            {
                customData = string.Join(", ", CustomData.Select(d => string.Format(CultureInfo.InvariantCulture, format, d.Key, d.Value)));
            }

            string error = string.Empty;
            if (Error != null)
            {
                error = Error.ToString();
            }
            string loggerMessage = Message + Category + DateTime.ToString() + error + LoggerLevel.ToString()
                + customData;
            return loggerMessage;

            //return Message + Category + DateTime.ToString() + Error == null ? string.Empty : Error.ToString() + LoggerLevel.ToString()
            //    + string.Join(", ", CustomData.Select(d => string.Format(CultureInfo.InvariantCulture, format, d.Key, d.Value)));
        }
    }
}