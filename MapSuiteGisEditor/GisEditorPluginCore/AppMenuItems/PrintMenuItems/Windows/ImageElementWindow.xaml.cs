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


using System.Linq;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for ImageElementWindow.xaml
    /// </summary>
    public partial class ImageElementWindow : Window
    {
        private ImageElementViewModel viewModel;

        public ImageElementWindow()
        {
            InitializeComponent();
            viewModel = new ImageElementViewModel();
            DataContext = viewModel;
            imgList.Focus();

            HelpContainer.Content = HelpResourceHelper.GetHelpButton("PrintMapImageHelp", HelpButtonMode.NormalButton);
        }

        [Obfuscation]
        private void BrowserClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files(*.jpg;*.bmp;*.png;*.gif)|*.jpg;*.bmp;*.png;*.gif";
            if (openFileDialog.ShowDialog().GetValueOrDefault())
            {
                viewModel.SelectedImage = null;
                viewModel.SelectedImage = File.ReadAllBytes(openFileDialog.FileName);
                uploadPathName.Text = openFileDialog.FileName;
            }
        }

        [Obfuscation]
        private void CancelClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        [Obfuscation]
        private void OKClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        internal void SetProperties(PrinterLayer printerLayer)
        {
            ImagePrinterLayer imagePrinterLayer = printerLayer as ImagePrinterLayer;
            if (imagePrinterLayer != null)
            {
                viewModel.BackgroundStyle = imagePrinterLayer.BackgroundMask;
                string imagePrinterLayerPathFilename = imagePrinterLayer.Image.PathFilename;
                if (File.Exists(imagePrinterLayerPathFilename))
                {
                    viewModel.SelectedImage = File.ReadAllBytes(imagePrinterLayerPathFilename);
                }
                else if (imagePrinterLayer.Image.GetImageStream() == null)
                {
                    System.Windows.Forms.MessageBox.Show(string.Format(CultureInfo.InvariantCulture,
                        GisEditor.LanguageManager.GetStringResource("FileNotExistsAlert"),
                        imagePrinterLayerPathFilename),
                        GisEditor.LanguageManager.GetStringResource("DataNotFoundlAlertTitle"));
                }
                else
                {
                    var stream = imagePrinterLayer.Image.GetImageStream();
                    stream.Seek(0, SeekOrigin.Begin);
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    viewModel.SelectedImage = bytes;
                }

                viewModel.DragMode = imagePrinterLayer.DragMode;
                viewModel.ResizeMode = imagePrinterLayer.ResizeMode;
            }
        }

        [Obfuscation]
        private void ConfigureBackgroundStyleClick(object sender, RoutedEventArgs e)
        {
            if (viewModel.BackgroundStyle != null)
                viewModel.BackgroundStyle.Name = GisEditor.StyleManager.GetStylePluginByStyle(viewModel.BackgroundStyle).Name;

            if (viewModel.BackgroundStyle != null
                && viewModel.BackgroundStyle.CustomAreaStyles.Count == 0)
            {
                var tempStyle = new AreaStyle();
                tempStyle.Name = viewModel.BackgroundStyle.Name;
                tempStyle = (AreaStyle)viewModel.BackgroundStyle.CloneDeep();
                viewModel.BackgroundStyle.CustomAreaStyles.Add(tempStyle);
            }

            StyleBuilderArguments styleArguments = new StyleBuilderArguments();
            styleArguments.AvailableStyleCategories = StyleCategories.Area;
            styleArguments.AvailableUIElements = StyleBuilderUIElements.StyleList;
            styleArguments.AppliedCallback = (result) =>
            {
                AreaStyle areaStyle = new AreaStyle();
                foreach (var item in result.CompositeStyle.Styles.OfType<AreaStyle>())
                {
                    areaStyle.CustomAreaStyles.Add(item);
                }
                viewModel.BackgroundStyle = areaStyle;
            };

            AreaStyle style = (AreaStyle)viewModel.BackgroundStyle.CloneDeep();
            style.Name = viewModel.BackgroundStyle.Name;
            var resultStyle = GisEditor.StyleManager.EditStyles(styleArguments, style);
            if (resultStyle != null)
            {
                resultStyle.SetDrawingLevel();
                viewModel.BackgroundStyle = resultStyle;
            }
        }
    }
}