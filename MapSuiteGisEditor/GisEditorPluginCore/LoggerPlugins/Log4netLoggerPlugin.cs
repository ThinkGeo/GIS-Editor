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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using log4net;
using log4net.Config;
using System.ComponentModel.Composition;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    [PartNotDiscoverable]
    public class Log4netLoggerPlugin : LoggerPlugin
    {
        private static ILog log;

        //private static readonly string logMessageFileName = "Logging\\MapSuiteGisEditor.log";
        //private static readonly string logMessageFileNameFormat = "Logging\\MapSuiteGisEditor{0}.log";
        private const string logMessageXmlStartFlag = "<LoggerMessage";
        private const string logMessageXmlEndFlag = "</LoggerMessage>";
        private static ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        public Log4netLoggerPlugin()
        {
            Name = "Log4net";
            CanGetLoggerMessages = true;
            var streamInfo = Application.GetResourceStream(new Uri("/GisEditorInfrastructure;component/Resources/LoggerSettings.txt", UriKind.RelativeOrAbsolute));
            var state = XElement.Load(streamInfo.Stream);
            log = GetCurrentLogger(state);
        }

        public XElement ToXml(LoggerMessage loggerMessage)
        {
            if (loggerMessage.CustomData == null)
                loggerMessage.CustomData = new Dictionary<string, string>();
            if (loggerMessage.Error == null)
                loggerMessage.Error = new ExceptionInfo();
            XElement xml = new XElement("LoggerMessage");
            xml.Add(new XAttribute("DateTime", loggerMessage.DateTime.ToString(CultureInfo.InvariantCulture)));
            XElement messageElement = new XElement("Message", loggerMessage.Message ?? string.Empty);
            XElement levelElement = new XElement("LoggerLevel", loggerMessage.LoggerLevel.ToString());
            XElement customDataElement = new XElement("CustomData");
            foreach (var item in loggerMessage.CustomData)
            {
                customDataElement.Add(new XElement("CustomDataItem", new XAttribute("Key", item.Key)) { Value = item.Value });
            }
            xml.Add(messageElement);
            xml.Add(levelElement);

            if (!string.IsNullOrEmpty(loggerMessage.Error.Message)
                || !string.IsNullOrEmpty(loggerMessage.Error.Source)
                || !string.IsNullOrEmpty(loggerMessage.Error.StackTrace))
            {
                XElement ErrorElement = new XElement("Error");
                ErrorElement.Add(new XElement("ErrorMessage", loggerMessage.Error.Message));
                ErrorElement.Add(new XElement("Source", loggerMessage.Error.Source));
                ErrorElement.Add(new XElement("StackTrace", loggerMessage.Error.StackTrace));
                xml.Add(ErrorElement);
            }
            xml.Add(customDataElement);

            return xml;
        }

        public LoggerMessage FromXml(XElement xml)
        {
            LoggerMessage loggerMessage = new LoggerMessage();
            loggerMessage.DateTime = Convert.ToDateTime(xml.FirstAttribute.Value);
            loggerMessage.Message = xml.Elements("Message").FirstOrDefault().Value;

            string loggerLevelString = xml.Elements("LoggerLevel").FirstOrDefault().Value;
            LoggerLevel loggerLevel = LoggerLevel.Information;
            Enum.TryParse(loggerLevelString, out loggerLevel);
            loggerMessage.LoggerLevel = loggerLevel;

            ExceptionInfo exceptionInfo = new ExceptionInfo();
            if (xml.Element("Error") != null)
            {
                string message = xml.Elements("Error").Elements("ErrorMessage").FirstOrDefault().Value;
                string stackTrace = xml.Elements("Error").Elements("StackTrace").FirstOrDefault().Value;
                string source = xml.Elements("Error").Elements("Source").FirstOrDefault().Value;
                exceptionInfo.Message = message;
                exceptionInfo.StackTrace = stackTrace;
                exceptionInfo.Source = source;
            }
            loggerMessage.Error = exceptionInfo;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            List<XElement> list = xml.Elements("CustomData").Elements("CustomDataItem").ToList();
            foreach (XElement item in list)
            {
                dictionary[item.FirstAttribute.Value] = item.Value;
            }
            loggerMessage.CustomData = dictionary;

            return loggerMessage;
        }

        protected override void LogCore(LoggerMessage loggerMessage)
        {
            ThreadPool.QueueUserWorkItem(obj =>
            {
                LoggerMessage currentMessage = (LoggerMessage)obj;
                locker.EnterWriteLock();
                try
                {
                    string loggerMessageString = ToXml(currentMessage).ToString();
                    switch (currentMessage.LoggerLevel)
                    {
                        case LoggerLevel.Error:
                            log.Error(loggerMessageString);
                            break;

                        case LoggerLevel.Debug:
                            log.Debug(loggerMessageString);
                            break;

                        case LoggerLevel.Warning:
                            log.Warn(loggerMessageString);
                            break;

                        case LoggerLevel.Information:
                        case LoggerLevel.Usage:
                        default:
                            log.Info(loggerMessageString);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }, loggerMessage);
        }

        protected override Collection<LoggerMessage> GetLoggerMessagesCore(DateTime from, DateTime to)
        {
            Collection<LoggerMessage> resultLoggerMessages = new Collection<LoggerMessage>();
#if GISEditorUnitTest
            var directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#else
            string directoryName = GisEditor.InfrastructureManager.SettingsPath;
            string logDirectoryName = Path.Combine(directoryName, "Logging");
#endif
            string[] logFiles = Directory.GetFiles(logDirectoryName, "*.log", SearchOption.TopDirectoryOnly);

            foreach (string logFile in Fileter(logFiles, from, to))
            {
                IEnumerable<LoggerMessage> loggerMessages = GetLoggerMessageXmlByFile(logFile);
                loggerMessages.Where(l => l.DateTime >= from && l.DateTime < to).ForEach(resultLoggerMessages.Add);
            }

            return resultLoggerMessages;
        }

        private IEnumerable<string> Fileter(IEnumerable<string> logFiles, DateTime from, DateTime to)
        {
            DateTime dateTime = DateTime.MinValue;
            return logFiles.Where(file =>
            {
                string dateText = Path.GetFileName(file).Replace("MapSuiteGisEditor", "").Replace(".log", "");
                if (!string.IsNullOrEmpty(dateText))
                {
                    if (DateTime.TryParse(dateText, out dateTime))
                    {
                        if (DateTime.Compare(dateTime, from.Date) >= 0 && DateTime.Compare(dateTime, to.Date) <= 0)
                        {
                            return true;
                        }
                        return false;
                    }
                }
                return true;
            });
        }

        private IEnumerable<LoggerMessage> GetLoggerMessageXmlByFile(string fileNamePath)
        {
            var targetLoggerPathFileName = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, Path.GetFileName(fileNamePath));
            File.Copy(fileNamePath, targetLoggerPathFileName, true);
            Collection<LoggerMessage> loggerMessages = new Collection<LoggerMessage>();
            if (File.Exists(targetLoggerPathFileName))
            {
                string[] lines = File.ReadAllLines(targetLoggerPathFileName);
                string xml = string.Empty;
                bool isStart = false;
                bool isEnd = false;
                foreach (string line in lines)
                {
                    if (line.Contains(logMessageXmlStartFlag))
                    {
                        isStart = true;
                    }
                    else if (line.Contains(logMessageXmlEndFlag))
                    {
                        isEnd = true;
                    }
                    else if (line.Contains(logMessageXmlStartFlag) && line.Contains(logMessageXmlEndFlag))
                    {
                        isStart = true;
                        isEnd = true;
                    }

                    xml += line;

                    if (isStart)
                    {
                        int index = xml.IndexOf(logMessageXmlStartFlag, StringComparison.Ordinal);
                        xml = xml.Substring(index);
                        isStart = false;
                    }

                    if (isEnd)
                    {
                        LoggerMessage loggerMessage = GetLoggerMessageByXmlString(xml);
                        loggerMessages.Add(loggerMessage);
                        xml = string.Empty;
                        isEnd = false;
                    }
                }
            }

            return loggerMessages;
        }

        private LoggerMessage GetLoggerMessageByXmlString(string xmlString)
        {
            LoggerMessage loggerMessage = new LoggerMessage();
            try
            {
                XElement loggerMessageXml = XElement.Parse(xmlString);
                loggerMessage = FromXml(loggerMessageXml);
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            return loggerMessage;
        }

        private static ILog GetCurrentLogger(XElement xElement)
        {
            if (log == null && xElement != null)
            {
                string activateLoggerName = xElement.Element("root").Attribute("name").Value;
                var activeAppenderFileXml = xElement.Descendants("File").FirstOrDefault();

                //string date = DateTime.Now.ToString("yyyy_MM_dd", CultureInfo.InvariantCulture);
                //string logMessageFileName = string.Format(CultureInfo.InvariantCulture, logMessageFileNameFormat, "_" + date);
                string directory = GisEditor.InfrastructureManager.SettingsPath;
                string logMessageFileName = Path.Combine(directory, "Logging", "MapSuiteGisEditor.log");
                if (activeAppenderFileXml != null && activeAppenderFileXml.Attribute("value") != null)
                {
                    activeAppenderFileXml.Attribute("value").Value = logMessageFileName;
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    xElement.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    SimplifyLoggerFile();
                    XmlConfigurator.Configure(ms);
                    log = LogManager.GetLogger(activateLoggerName);
                    return log;
                }
            }
            return log;
        }

        private static void SimplifyLoggerFile()
        {
            string loggerFolderPath = Path.Combine(GisEditor.InfrastructureManager.SettingsPath, "Logging");
            if (!Directory.Exists(loggerFolderPath))
            {
                Directory.CreateDirectory(loggerFolderPath);
            }

            string logMessageFileName = Path.Combine(loggerFolderPath, "MapSuiteGisEditor.log");
            string tempFilePath = logMessageFileName + ".temp";

            if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            string thresholdText = "";
            string[] files = Directory.GetFiles(loggerFolderPath, "MapSuiteGisEditor*.log").OrderBy(f => f).ToArray();
            if (files.Length > 1)
            {
                DateTime dateTime;
                string lastFilePath = files[files.Length - 1];
                using (StreamReader streamReader = new StreamReader(lastFilePath))
                {
                    string text = null;
                    while ((text = streamReader.ReadLine()) != null)
                    {
                        if (IsMatch(text, out dateTime))
                        {
                            thresholdText = text;
                        }
                    }
                }

                StreamWriter sw = null;
                StreamReader sr = null;
                try
                {
                    sw = new StreamWriter(tempFilePath, true);
                    sr = new StreamReader(File.Open(logMessageFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    string text = null;
                    bool hasFound = false;
                    bool hasBegin = false;
                    while ((text = sr.ReadLine()) != null)
                    {
                        if (hasFound && IsMatch(text, out dateTime) && !hasBegin)
                        {
                            hasBegin = true;
                        }
                        if (text.Equals(thresholdText))
                        {
                            hasFound = true;
                        }
                        if (hasBegin)
                        {
                            sw.WriteLine(text);
                        }
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    if (sr != null) sr.Dispose();
                    if (sw != null) sw.Dispose();
                    try
                    {
                        File.Delete(logMessageFileName);
                        File.Move(tempFilePath, logMessageFileName);
                    }
                    catch { }
                }
            }
        }

        private static bool IsMatch(string text, out DateTime dateTime)
        {
            string[] array = text.Split(' ');
            dateTime = DateTime.MinValue;
            return text.Contains("<LoggerMessage ") && DateTime.TryParse(array[0], out dateTime);
        }
    }
}