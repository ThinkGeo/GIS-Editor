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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for GisEditorSplashScreen.xaml
    /// </summary>
    public partial class GisEditorSplashScreen : Window
    {
        private string versionInfo;
        private TimeSpan fadeoutDuration;

        public GisEditorSplashScreen()
            : this(string.Empty)
        {
        }

        public GisEditorSplashScreen(string resourceName)
            : this(null, resourceName)
        {
        }

        public GisEditorSplashScreen(Assembly resourceAssembly, string resourceName)
        {
            InitializeComponent();

            version.Text = GetVersion();

            BitmapImage splashScreenImageSource = new BitmapImage();
            splashScreenImageSource.BeginInit();
            splashScreenImageSource.UriSource = new Uri(resourceName, UriKind.Relative);
            splashScreenImageSource.EndInit();

            SplashScreenImage.Source = splashScreenImageSource;
            this.Width = SplashScreenImage.Width;
        }

        private static string GetEditionName()
        {
            string editionName = string.Empty;

            string localApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string settingsPath = Path.Combine(localApplicationDataPath, "MapSuiteGisEditor");

            if (!Directory.Exists(settingsPath))
            {
                editionName = new InfrastructureManager().EditionName;
            }
            else
            {
                string activeProfiler = Path.Combine(settingsPath, "ActiveProfiler.xml");
                if (File.Exists(activeProfiler))
                {
                    System.Xml.Linq.XElement startUpSetting = System.Xml.Linq.XElement.Load(activeProfiler);
                    if (startUpSetting.Element("EditionName") != null)
                        editionName = startUpSetting.Element("EditionName").Value;
                }
            }

            return editionName;
        }

        private static string GetVersion()
        {
            string editionName = GetEditionName();

            //string editionName = new InfrastructureManager().EditionName;
            if (!string.IsNullOrEmpty(editionName)) editionName = editionName + " - ";

            return string.Format(editionName + "{0}", AboutWindow.GetVersionInfo());
        }

        public void Close(TimeSpan fadeoutDuration)
        {
            this.fadeoutDuration = fadeoutDuration;
            AsynchronousExit();
        }

        private void AsynchronousExit()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
            worker.RunWorkerAsync();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(fadeoutDuration);
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Close();
        }

        public void Show(bool autoClose)
        {
            Show(autoClose, false);
        }

        public void Show(bool autoClose, bool topMost)
        {
            Topmost = topMost;
            ShowInTaskbar = false;
            Show();
        }

        public string VersionInfo
        {
            get { return versionInfo; }
            set
            {
                versionInfo = value;
                version.Text = versionInfo;
            }
        }
    }
}
