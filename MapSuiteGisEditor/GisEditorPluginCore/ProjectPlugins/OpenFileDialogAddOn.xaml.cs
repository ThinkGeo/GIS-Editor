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
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public partial class OpenFileDialogAddOn : WindowAddOnBase
    {
        public OpenFileDialogAddOn()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            BitmapImage bitmapImage = image.Source as BitmapImage;
            if (bitmapImage != null && bitmapImage.StreamSource != null)
            {
                bitmapImage.StreamSource.Dispose();
            }
        }

        [Obfuscation]
        private void TGProjPreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            ParentDlg.EventFileNameChanged += new PathChangedEventHandler(ParentDlg_EventFileNameChanged);
        }

        [Obfuscation]
        private void TGProjPreviewControl_UnLoaded(object sender, RoutedEventArgs e)
        {
            ParentDlg.EventFileNameChanged -= new PathChangedEventHandler(ParentDlg_EventFileNameChanged);
            image.Source = null;
        }

        private void ParentDlg_EventFileNameChanged(IFileDlgExt sender, string filePath)
        {
            if (File.Exists(filePath))
            {
                ZipFileAdapter zipFileAdapter = null;
                try
                {
                    zipFileAdapter = ZipFileAdapterManager.CreateInstance(filePath);
                    string projName = Path.GetFileNameWithoutExtension(filePath);
                    string imagePath = "Preview.png";
                    if (zipFileAdapter.GetEntryNames().Contains(imagePath))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();

                        Stream stream = zipFileAdapter.GetEntryStreamByName(imagePath);
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, (int)stream.Length);

                        MemoryStream memoryStream = new MemoryStream(bytes);
                        bitmapImage.StreamSource = memoryStream;
                        bitmapImage.EndInit();
                        image.Source = bitmapImage;
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    System.Windows.Forms.MessageBox.Show(ex.Message, "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                }
                finally
                {
                    if (zipFileAdapter != null)
                    {
                        zipFileAdapter.Dispose();
                    }
                }
            }
        }
    }
}