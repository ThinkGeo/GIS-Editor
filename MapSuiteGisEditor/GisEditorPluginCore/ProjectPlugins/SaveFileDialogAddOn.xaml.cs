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

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Interaction logic for SaveFileDialogAddOn.xaml
    /// </summary>
    public partial class SaveFileDialogAddOn : WindowAddOnBase
    {
        public static event EventHandler<EventArgs> ChildControlLoaded;

        public SaveFileDialogAddOn()
        {
            InitializeComponent();
        }

        [Obfuscation]
        private void TGProjPreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            image.Source = GenerateImage((FrameworkElement)((System.Windows.Controls.UserControl)Application.Current.MainWindow.Content).Content);
            OnChildControlLoaded(null);
        }

        [Obfuscation]
        private void TGProjPreviewControl_UnLoaded(object sender, RoutedEventArgs e)
        {
            BitmapImage bitmapImage = image.Source as BitmapImage;
            if (bitmapImage != null
                && bitmapImage.StreamSource != null)
            {
                bitmapImage.StreamSource.Dispose();
            }

            image.Source = null;
        }

        private static BitmapImage GenerateImage(FrameworkElement element)
        {
            Size size = RetrieveDesiredSize(element);

            Rect rect = new Rect(0, 0, size.Width, size.Height);

            if (size.Width < 1 || size.Height < 1)
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = Application.GetResourceStream(new Uri("/MapSuiteGisEditor;component/Images/DefaultPreview.png", UriKind.RelativeOrAbsolute)).Stream;
                bitmapImage.EndInit();

                return bitmapImage;
            }
            else
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);

                element.Arrange(rect); //Let the control arrange itself inside your Rectangle
                rtb.Render(element); //Render the control on the RenderTargetBitmap

                //Now encode and convert to a gdi+ Image object
                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rtb));
                MemoryStream stream = new MemoryStream();
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                png.Save(stream);
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        private static Size RetrieveDesiredSize(FrameworkElement element)
        {
            if (Equals(element.ActualWidth, double.NaN) || Equals(element.ActualHeight, double.NaN))
            {
                element.Measure(new System.Windows.Size(double.MaxValue, double.MaxValue));
                return element.DesiredSize;
            }

            return new Size(element.ActualWidth, element.ActualHeight);
        }

        private static void OnChildControlLoaded(EventArgs e)
        {
            EventHandler<EventArgs> handler = ChildControlLoaded;
            if (handler != null) handler(null, e);
        }
    }
}
