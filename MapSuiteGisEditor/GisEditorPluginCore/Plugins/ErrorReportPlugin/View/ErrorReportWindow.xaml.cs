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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ErrorReportWindow.xaml
    /// </summary>
    public partial class ErrorReportWindow : Window
    {
        private ErrorReport error;

        public event EventHandler NoticeErrorChanged;

        public ErrorReportWindow(ErrorReport error)
        {
            InitializeComponent();

            Error = error;
            ErrorMessage.Text = string.Empty;
            txtEmailAddress.Text = GetSenderEmailAddress();
        }

        public ErrorReport Error
        {
            get { return error; }
            set { error = value; }
        }

        private string GetErrorDetailMessage()
        {
            string message = String.Empty;

            if (Error != null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(String.Format(CultureInfo.InvariantCulture, "Source: \t{0}\r\n", Error.Source));
                sb.Append(String.Format(CultureInfo.InvariantCulture, "Message: \t{0}\r\n", Error.Message));
                sb.Append(String.Format(CultureInfo.InvariantCulture, "Stack: \t{0}\r\n", Error.StackTrace));
                message = sb.ToString();
            }

            return message;
        }

        protected virtual void OnNoticeErrorChanged(object sender, EventArgs e)
        {
            EventHandler handler = NoticeErrorChanged;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        [Obfuscation]
        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SendErrorReport()
        {
            ErrorReport errorReport = new ErrorReport();
            errorReport.Source = Error.Source;
            errorReport.Message = Error.Message;
            errorReport.StackTrace = Error.StackTrace;
            errorReport.SenderEmailAddress = string.IsNullOrEmpty(txtEmailAddress.Text) ? Environment.MachineName : txtEmailAddress.Text;
            errorReport.AdditionalComment = ErrorMessage.Text;

            SaveSenderEmailAddress(txtEmailAddress.Text);

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, a) =>
            {
                try
                {
                    ReportErrorToSQS(errorReport);
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, "Error happened when invoking error report service.", ex);
                }
            };

            worker.RunWorkerCompleted += (s, a) =>
            {
                ((BackgroundWorker)s).Dispose();
            };

            worker.RunWorkerAsync();
        }

        private static void ReportErrorToSQS(ErrorReport report)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(ErrorReport));
            string objectXml = null;
            using (MemoryStream stream = new MemoryStream())
            {
                serializer.WriteObject(stream, report);

                StreamReader reader = new StreamReader(stream);
                stream.Seek(0, SeekOrigin.Begin);
                objectXml = reader.ReadToEnd();
            }
        }

        [Obfuscation]
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string GetSenderEmailAddress()
        {
            var errorReportPlugin = GisEditor.UIManager.GetPlugins().OfType<ErrorReportUIPlugin>().FirstOrDefault();
            if (errorReportPlugin != null) return errorReportPlugin.Email;
            else return string.Empty;
        }

        private void SaveSenderEmailAddress(string email)
        {
            var errorReportPlugin = GisEditor.UIManager.GetPlugins().OfType<ErrorReportUIPlugin>().FirstOrDefault();
            if (errorReportPlugin != null) errorReportPlugin.Email = email;
        }

        [Obfuscation]
        private void ckbNoticeEx_Click(object sender, RoutedEventArgs e)
        {
            OnNoticeErrorChanged(sender, e);
        }

        [Obfuscation]
        private void viewDetailButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorDetailWindow errorDetailWindow = new ErrorDetailWindow(GetErrorDetailMessage()) { Owner = this };
            errorDetailWindow.ShowDialog();
        }

        [Obfuscation]
        private void Window_Closed(object sender, EventArgs e)
        {
            var invocationList = NoticeErrorChanged.GetInvocationList();
            foreach (var handler in invocationList)
            {
                Delegate.Remove(NoticeErrorChanged, handler);
            }
        }

        private static string DecodeString(string key)
        {
            byte[] bytes = Convert.FromBase64String(key);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}