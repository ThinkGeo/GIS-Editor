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
using System.ComponentModel.Composition;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represent a plugin for a logger
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(LoggerPlugin))]
    public abstract class LoggerPlugin : Plugin
    {
        private long ticks = TimeSpan.FromDays(1).Ticks;
        private bool canGetLoggerMessages;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerPlugin" /> class.
        /// </summary>
        protected LoggerPlugin()
        {
            canGetLoggerMessages = false;
        }

        /// <summary>
        /// Logs the specified logger level.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        public void Log(LoggerLevel loggerLevel, string message)
        {
            Log(loggerLevel, message, default(Exception));
        }

        /// <summary>
        /// Logs the specified logger level.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void Log(LoggerLevel loggerLevel, string message, Exception error)
        {
            if (error != null)
            {
                Log(loggerLevel, message, new ExceptionInfo(error.Message, error.StackTrace, error.Source));
            }
        }

        /// <summary>
        /// Logs the specified logger level.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void Log(LoggerLevel loggerLevel, string message, ExceptionInfo error)
        {
            Log(new LoggerMessage(loggerLevel, message, error));
        }

        /// <summary>
        /// Logs the specified logger message.
        /// </summary>
        /// <param name="loggerMessage">The logger message.</param>
        public void Log(LoggerMessage loggerMessage)
        {
            LogCore(loggerMessage);
        }

        /// <summary>
        /// Logs the core.
        /// </summary>
        /// <param name="loggerMessage">The logger message.</param>
        protected abstract void LogCore(LoggerMessage loggerMessage);

        /// <summary>
        /// Gets or sets a value indicating whether this instance can get logger messages.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance can get logger messages; otherwise, <c>false</c>.
        /// </value>
        public bool CanGetLoggerMessages
        {
            get { return canGetLoggerMessages; }
            protected set { canGetLoggerMessages = value; }
        }

        /// <summary>
        /// Gets the logger messages.
        /// </summary>
        /// <returns>Messages of logger collection</returns>
        public Collection<LoggerMessage> GetLoggerMessages()
        {
            return GetLoggerMessages(DateTime.MinValue, DateTime.MaxValue);
        }

        /// <summary>
        /// Gets the logger messages.
        /// </summary>
        /// <param name="latestMessageCount">The latest message count.</param>
        /// <returns>Messages of logger collection</returns>
        public Collection<LoggerMessage> GetLoggerMessages(int latestMessageCount)
        {
            int i = 0;
            Collection<LoggerMessage> tmpCollection = new Collection<LoggerMessage>();
            Collection<LoggerMessage> result = new Collection<LoggerMessage>();
            while (result.Count < latestMessageCount)
            {
                tmpCollection = GetLoggerMessages(DateTime.Now.AddTicks(-ticks - i * ticks), DateTime.Now.AddTicks(-i * ticks));
                for (int j = 0; j < tmpCollection.Count; j++)
                {
                    result.Add(tmpCollection[j]);
                }
                i++;
            }
            return (Collection<LoggerMessage>)result.Take(latestMessageCount);
        }

        /// <summary>
        /// Gets the logger messages.
        /// </summary>
        /// <param name="timespan">The timespan.</param>
        /// <returns>Messages of logger collection</returns>
        public Collection<LoggerMessage> GetLoggerMessages(TimeSpan timespan)
        {
            return GetLoggerMessages(DateTime.Now.Subtract(timespan), DateTime.Now);
        }

        /// <summary>
        /// Gets the logger messages.
        /// </summary>
        /// <param name="fromDate">From date.</param>
        /// <param name="toDate">To date.</param>
        /// <returns>Messages of logger collection</returns>
        public Collection<LoggerMessage> GetLoggerMessages(DateTime fromDate, DateTime toDate)
        {
            var logMessages = new Collection<LoggerMessage>();
            if (CanGetLoggerMessages)
            {
                var tempMessages = GetLoggerMessagesCore(fromDate, toDate);//.Distinct(new LoggerMessageComparer());
                foreach (var item in tempMessages)
                {
                    logMessages.Add(item);
                }
            }

            return logMessages;
        }

        /// <summary>
        /// Gets the logger messages core.
        /// </summary>
        /// <param name="fromDate">From date.</param>
        /// <param name="toDate">To date.</param>
        /// <returns>Messages of logger collection</returns>
        protected abstract Collection<LoggerMessage> GetLoggerMessagesCore(DateTime fromDate, DateTime toDate);
    }
}