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
    /// This is the manager of logging system.
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(LoggerPluginManager))]
    public class LoggerPluginManager : PluginManager
    {
        /// <summary>
        /// Occurs when [logging].
        /// </summary>
        public event EventHandler<LoggingLoggerManagerEventArgs> Logging;

        /// <summary>
        /// Occurs when [logged].
        /// </summary>
        public event EventHandler<LoggedLoggerManagerEventArgs> Logged;

        /// <summary>
        /// Initialize an instance of LoggerManager.
        /// </summary>
        public LoggerPluginManager()
        {
        }

        /// <summary>
        /// Gets the plugins core.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            return CollectPlugins<LoggerPlugin>();
        }

        /// <summary>
        /// Gets the logger plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<LoggerPlugin> GetLoggerPlugins()
        {
            return new Collection<LoggerPlugin>(GetPlugins().Cast<LoggerPlugin>().ToList());
        }

        public Collection<T> GetActiveLoggerPlugins<T>() where T : LoggerPlugin
        {
            return new Collection<T>(GetActiveLoggerPlugins().OfType<T>().ToList());
        }

        /// <summary>
        /// Gets the logger plugins.
        /// </summary>
        /// <returns></returns>
        public Collection<LoggerPlugin> GetActiveLoggerPlugins()
        {
            var activePlugins = from p in GetLoggerPlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<LoggerPlugin>(activePlugins.ToList());
        }

        /// <summary>
        /// Logs the specified logger level.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        public void Log(LoggerLevel loggerLevel, string message)
        {
            Log(loggerLevel, message, LoggerMessage.DefaultCategory);
        }

        /// <summary>
        /// Logs the specified logger level.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void Log(LoggerLevel loggerLevel, string message, Exception error)
        {
            LoggerMessage loggerMessage = new LoggerMessage(loggerLevel, message);
            if (error != null) loggerMessage.Error = new ExceptionInfo(error.Message, error.StackTrace, error.Source);
            else loggerMessage.Error = new ExceptionInfo(string.Empty, string.Empty, string.Empty);

            Log(loggerMessage);
        }

        /// <summary>
        /// Logs the specified logger level.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void Log(LoggerLevel loggerLevel, string message, ExceptionInfo error)
        {
            Log(new LoggerMessage(loggerLevel, message, LoggerMessage.DefaultCategory, error));
        }

        public void Log(LoggerLevel loggerLevel, string message, string category)
        {
            Log(new LoggerMessage(loggerLevel, message, category, null));
        }

        /// <summary>
        /// Logs the specified logger level.
        /// </summary>
        /// <param name="loggerLevel">The logger level.</param>
        /// <param name="message">The message.</param>
        /// <param name="error">The error.</param>
        public void Log(LoggerLevel loggerLevel, string message, string category, ExceptionInfo error)
        {
            Log(new LoggerMessage(loggerLevel, message, category, error));
        }

        /// <summary>
        /// Logs the specified logger message.
        /// </summary>
        /// <param name="loggerMessage">The logger message.</param>
        public void Log(LoggerMessage loggerMessage)
        {
            OnLogging(new LoggingLoggerManagerEventArgs(loggerMessage));
            LogCore(loggerMessage);
            OnLogged(new LoggedLoggerManagerEventArgs(loggerMessage));
        }

        /// <summary>
        /// Logs the core.
        /// </summary>
        /// <param name="loggerMessage">The logger message.</param>
        protected virtual void LogCore(LoggerMessage loggerMessage)
        {
            foreach (var plugin in GetActiveLoggerPlugins())
            {
                plugin.Log(loggerMessage);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Logging" /> event.
        /// </summary>
        /// <param name="e">The <see cref="LoggingLoggerManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnLogging(LoggingLoggerManagerEventArgs e)
        {
            EventHandler<LoggingLoggerManagerEventArgs> handler = Logging;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:Logged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="LoggedLoggerManagerEventArgs" /> instance containing the event data.</param>
        protected virtual void OnLogged(LoggedLoggerManagerEventArgs e)
        {
            EventHandler<LoggedLoggerManagerEventArgs> handler = Logged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Gets the logger messages.
        /// </summary>
        /// <returns></returns>
        public Collection<LoggerMessage> GetLoggerMessages()
        {
            return GetLoggerMessages(DateTime.MinValue, DateTime.MaxValue);
        }

        /// <summary>
        /// Gets the logger messages.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns></returns>
        public Collection<LoggerMessage> GetLoggerMessages(DateTime from, DateTime to)
        {
            return GetLoggerMessagesCore(from, to);
        }

        /// <summary>
        /// Gets the logger messages core.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <returns></returns>
        protected virtual Collection<LoggerMessage> GetLoggerMessagesCore(DateTime from, DateTime to)
        {
            var result = new Collection<LoggerMessage>();
            foreach (var plugin in GetPlugins().Cast<LoggerPlugin>())
            {
                foreach (var messageItem in plugin.GetLoggerMessages(from, to))
                {
                    result.Add(messageItem);
                }
            }

            return result;
        }
    }
}