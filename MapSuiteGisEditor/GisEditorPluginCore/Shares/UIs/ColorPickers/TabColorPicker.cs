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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.Win32;
using ThinkGeo.MapSuite.Drawing;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class TabColorPicker : Control
    {
        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register("SelectedBrush", typeof(GeoBrush), typeof(TabColorPicker), new PropertyMetadata(new GeoSolidBrush(GeoColor.StandardColors.White), new PropertyChangedCallback(OnSelectedBrushPropertyChanged)));

        private static string accessDeniedPattern = "(?<=Access to the path ')(.|\n)+?(?=' is denied)";
        private static string accessDeniedMessageFormat = GisEditor.LanguageManager.GetStringResource("TabColorPickerDonotHavePermissionText") + Environment.NewLine + GisEditor.LanguageManager.GetStringResource("TabColorPickerRestartToFinishText");
        private static ObservableCollection<SolidColorBrush> defaultColors;

        private bool isTemplateApplied;
        private TabItem solidColorBrushTabItem;
        private TabItem hatchBrushTabItem;
        private TabItem textureTabItem;
        private TabItem gradientTabItem;
        private ListBox customColorList;
        private SolidColorPicker solidColorPicker;
        private TexturePicker textureColorPicker;
        private HatchPicker hatchColorPicker;
        private DrawingLinearGradientBrushPicker gradientPicker;
        private Button pickTextureButton;
        private TabControl colorTabControl;
        private ContentPresenter helpContainer;

        public event MouseButtonEventHandler SelectedItemDoubleClick;

        private Button addcutomColorButton;

        public TabColorPicker()
        {
            DefaultStyleKey = typeof(TabColorPicker);
            IsSolidColorBrushTabEnabled = true;
            IsGradientColorBrushTabEnabled = true;
            IsHatchBrushTabEnabled = true;
            IsTextureBrushTabEnabled = true;
        }

        public static ObservableCollection<SolidColorBrush> DefaultColors
        {
            get { return defaultColors; }
            set { defaultColors = value; }
        }

        public GeoBrush SelectedBrush
        {
            get { return (GeoBrush)GetValue(SelectedBrushProperty); }
            set { SetValue(SelectedBrushProperty, value); }
        }

        public bool IsSolidColorBrushTabEnabled { get; set; }

        public bool IsGradientColorBrushTabEnabled { get; set; }

        public bool IsHatchBrushTabEnabled { get; set; }

        public bool IsTextureBrushTabEnabled { get; set; }

        public void UnSelect()
        {
            if (solidColorPicker != null) solidColorPicker.IsPicking = false;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            solidColorBrushTabItem = GetTemplateChild("SolidColorBrushTabItem") as TabItem;
            hatchBrushTabItem = GetTemplateChild("HatchBrushTabItem") as TabItem;
            textureTabItem = GetTemplateChild("TextureTabItem") as TabItem;
            gradientTabItem = GetTemplateChild("GradientColorBrushTabItem") as TabItem;
            solidColorPicker = GetTemplateChild("SolidColorPicker") as SolidColorPicker;
            textureColorPicker = GetTemplateChild("TextureColorPicker") as TexturePicker;
            hatchColorPicker = GetTemplateChild("HatchColorPicker") as HatchPicker;
            gradientPicker = GetTemplateChild("GradientPicker") as DrawingLinearGradientBrushPicker;
            pickTextureButton = GetTemplateChild("PickTextureButton") as Button;
            colorTabControl = GetTemplateChild("ColorTabControl") as TabControl;
            customColorList = GetTemplateChild("CustomColorList") as ListBox;
            helpContainer = GetTemplateChild("HelpContainer") as ContentPresenter;
            addcutomColorButton = GetTemplateChild("AddCustomColorButton") as Button;

            if (!isTemplateApplied)
            {
                solidColorPicker.IsEnabled = IsSolidColorBrushTabEnabled;
                textureColorPicker.IsEnabled = IsTextureBrushTabEnabled;
                hatchColorPicker.IsEnabled = IsHatchBrushTabEnabled;
                gradientPicker.IsEnabled = IsGradientColorBrushTabEnabled;

                solidColorPicker.SelectionChanged += new EventHandler(SolidColorPicker_SelectionChanged);
                hatchColorPicker.SelectionChanged += new EventHandler(HatchColorPicker_SelectionChanged);
                textureColorPicker.PropertyChanged += new PropertyChangedEventHandler(TextureColorPicker_PropertyChanged);
                gradientPicker.SelectedBrushChanged += new EventHandler(GradientPicker_SelectedBrushChanged);
                pickTextureButton.Click += new RoutedEventHandler(PickTextureButton_Click);

                solidColorPicker.SelectedItemDoubleClick += OnSelectedItemDoubleClick;
                hatchColorPicker.SelectedItemDoubleClick += OnSelectedItemDoubleClick;
                textureColorPicker.SelectedItemDoubleClick += OnSelectedItemDoubleClick;

                solidColorPicker.ColorPanelMouseDoubleClick += new MouseButtonEventHandler(SolidColorPicker_ColorPanelMouseDoubleClick);
                customColorList.SelectionChanged += new SelectionChangedEventHandler(CustomColorList_SelectionChanged);
                colorTabControl.SelectionChanged += new SelectionChangedEventHandler(ColorTabControl_SelectionChanged);
                addcutomColorButton.Click += new RoutedEventHandler(AddcutomColorButton_Click);
                helpContainer.Content = HelpResourceHelper.GetHelpButton("ColorPickerHelp", HelpButtonMode.IconWithLabel);

                defaultColors = CreateDefaultPalette();
                customColorList.ItemsSource = defaultColors;
                isTemplateApplied = true;

                Loaded -= TabColorPicker_Loaded;
                Loaded += TabColorPicker_Loaded;
            }
        }

        void TabColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            SetSelectedBrush(SelectedBrush);
            Loaded -= TabColorPicker_Loaded;
        }

        private void SolidColorPicker_ColorPanelMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            int index = customColorList.SelectedIndex;
            if (index == -1)
            {
                foreach (var item in defaultColors)
                {
                    index++;
                    if (item.Color == Colors.Transparent)
                    {
                        customColorList.SelectedIndex = index;
                        break;
                    }
                }
            }

            index = customColorList.SelectedIndex;
            if (index == -1)
            {
                customColorList.SelectedIndex = 0;
            }

            AddcutomColorButton_Click(sender, new RoutedEventArgs());
            OnSelectedItemDoubleClick(sender, e);
        }

        private void AddcutomColorButton_Click(object sender, RoutedEventArgs e)
        {
            int index = customColorList.SelectedIndex;
            if (index != -1)
            {
                defaultColors.RemoveAt(index);
                defaultColors.Insert(index, new SolidColorBrush(solidColorPicker.SelectedCustomColor));
                customColorList.SelectedIndex = index;

                UpdateDefaultColors();
            }
        }

        private static void UpdateDefaultColors()
        {
            string defaultColorsPathFileName = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "DefaultColors.xml");
            if (File.Exists(defaultColorsPathFileName))
            {
                File.Delete(defaultColorsPathFileName);
            }
            XElement root = new XElement("DefaultColors");
            foreach (var item in defaultColors)
            {
                root.Add(new XElement("Color",
                    new XElement("A", item.Color.A),
                    new XElement("R", item.Color.R),
                    new XElement("G", item.Color.G),
                    new XElement("B", item.Color.B)));
            }
            root.Save(defaultColorsPathFileName);
        }

        private void CustomColorList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (customColorList.SelectedItem != null)
            {
                Color color = ((SolidColorBrush)customColorList.SelectedItem).Color;
                SelectedBrush = new GeoSolidBrush(GeoColor.FromArgb(color.A, color.R, color.G, color.B));
            }
        }

        private void ColorTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tabItem = e.AddedItems.OfType<TabItem>().FirstOrDefault();
            if (tabItem != null && tabItem.Content != null)
            {
                var tempSolidColorPicker = tabItem.Content as SolidColorPicker;
                var tempGradientColorPicker = tabItem.Content as DrawingLinearGradientBrushPicker;
                var tempHatchColorPicker = tabItem.Content as HatchPicker;
                var tempTexturePicker = tabItem.Content as TexturePicker;
                if (tempSolidColorPicker != null)
                {
                    BeginChangeColorFromColorPicker(tempSolidColorPicker, e);
                }
                else if (tempGradientColorPicker != null)
                {
                    BeginChangeColorFromColorPicker(tempGradientColorPicker, e);
                }
                else if (tempHatchColorPicker != null)
                {
                    BeginChangeColorFromColorPicker(tempHatchColorPicker, e);
                }
                else if (tempTexturePicker != null)
                {
                    BeginChangeColorFromColorPicker(tempTexturePicker, new PropertyChangedEventArgs("SelectedBrush"));
                }
            }
        }

        protected virtual void OnSelectedItemDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var handler = SelectedItemDoubleClick;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void SetSelectedBrush(GeoBrush geoBrush)
        {
            if (!isTemplateApplied) return;

            if (geoBrush is GeoSolidBrush)
            {
                SelectTabItem(solidColorBrushTabItem);
                Color selectedMediaColor = GeoColor2MediaColorConverter.Convert(((GeoSolidBrush)geoBrush).Color);
                if (solidColorPicker.SelectedColor != selectedMediaColor)
                {
                    solidColorPicker.SelectedColor = selectedMediaColor;
                }
            }
            else if (geoBrush is GeoLinearGradientBrush)
            {
                SelectTabItem(gradientTabItem);
                GeoLinearGradientBrush geoGradientBrush = (GeoLinearGradientBrush)geoBrush;
                System.Drawing.Color drawingStartColor = GeoColor2DrawingColorConverter.Convert(geoGradientBrush.StartColor);
                System.Drawing.Color drawingEndColor = GeoColor2DrawingColorConverter.Convert(geoGradientBrush.EndColor);

                if (gradientPicker.SelectedBrush.StartColor != drawingStartColor
                    || gradientPicker.SelectedBrush.EndColor != drawingEndColor
                    || gradientPicker.SelectedBrush.Angle - geoGradientBrush.DirectionAngle > 1)
                    gradientPicker.SelectedBrush = new LinearGradientBrushEntity
                    {
                        StartColor = drawingStartColor,
                        EndColor = drawingEndColor,
                        Angle = (int)geoGradientBrush.DirectionAngle
                    };
            }
            else if (geoBrush is GeoHatchBrush)
            {
                GeoHatchBrush geoHatchBrush = (GeoHatchBrush)geoBrush;
                SelectTabItem(hatchBrushTabItem);
                System.Drawing.Drawing2D.HatchStyle drawingHatchStyle = GeoHatchStyle2DrawingHatchStyle.Convert(geoHatchBrush.HatchStyle);
                System.Drawing.Color drawingBackgroundColor = GeoColor2DrawingColorConverter.Convert(geoHatchBrush.BackgroundColor);
                System.Drawing.Color drawingForegroundColor = GeoColor2DrawingColorConverter.Convert(geoHatchBrush.ForegroundColor);

                if (hatchColorPicker.SelectedHatchStyle != drawingHatchStyle
                    || drawingBackgroundColor != hatchColorPicker.BackgroundColor
                    || drawingForegroundColor != hatchColorPicker.ForegroundColor)
                {
                    hatchColorPicker.BackgroundColor = drawingBackgroundColor;
                    hatchColorPicker.ForegroundColor = drawingForegroundColor;
                    hatchColorPicker.SelectedHatchStyle = drawingHatchStyle;
                }
            }
            else if (geoBrush is GeoTextureBrush)
            {
                SelectTabItem(textureTabItem);
                textureTabItem.IsSelected = true;
                GeoTextureBrush geoTextureBrush = (GeoTextureBrush)geoBrush;
                if (textureColorPicker.SelectedBrush == null || !textureColorPicker.SelectedBrush.GetValue(Canvas.TagProperty).Equals(geoTextureBrush.GeoImage.GetPathFilename()))
                {
                    textureColorPicker.SelectedBrush = GeoTextureBrushToImageBrushConverter.Convert(geoTextureBrush);
                }
            }
        }

        private void UnselectOtherColors(Control colorPicker)
        {
            //if (colorPicker != solidColorPicker)
            //{
            //    solidColorPicker.UnSelect();
            //}
            //if (colorPicker != hatchColorPicker)
            //{
            //    hatchColorPicker.UnSelect();
            //}
            //if (colorPicker != textureColorPicker)
            //{
            //    textureColorPicker.SelectedBrush = null;
            //}
        }

        private void PickTextureButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog() { Multiselect = false, Filter = "Png Images | *.png" };
            string newFilePath = "";
            NonTopMostPopup.IsOpenedByDialog = true;
            if (ofd.ShowDialog().GetValueOrDefault())
            {
                string directory = TexturePickerViewModel.GetTextureImagesFolder();
                string newFileName = Guid.NewGuid().ToString().Replace("-", "") + Path.GetExtension(ofd.FileName);
                newFilePath = Path.Combine(directory, newFileName);
                try
                {
                    File.Copy(ofd.FileName, newFilePath, true);

                    BitmapImage imageSource = new BitmapImage();
                    imageSource.BeginInit();
                    imageSource.UriSource = new Uri("file://" + newFilePath);
                    imageSource.EndInit();

                    ImageBrush brush = new ImageBrush(imageSource);
                    brush.SetValue(Canvas.TagProperty, newFilePath);
                    textureColorPicker.ImageBrushes.Add(brush);

                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ((Popup)((FrameworkElement)this.Parent)).IsOpen = true;
                        e.Handled = true;
                    }));
                }
                catch (IOException ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                    System.Windows.Forms.MessageBox.Show(ex.Message, "IOException");
                }
                catch (Exception ex)
                {
                    string originalExceptionMessage = ex.Message;
                    string newExceptionMessage = Regex.IsMatch(originalExceptionMessage, accessDeniedPattern) ?
                        string.Format(CultureInfo.InvariantCulture, accessDeniedMessageFormat, newFilePath) : originalExceptionMessage;
                    System.Windows.Forms.MessageBox.Show(newExceptionMessage, "Warning");
                }
            }
        }

        private void SolidColorPicker_SelectionChanged(object sender, EventArgs e)
        {
            BeginChangeColorFromColorPicker(solidColorPicker, e);
            UnselectOtherColors(solidColorPicker);
        }

        private void HatchColorPicker_SelectionChanged(object sender, EventArgs e)
        {
            BeginChangeColorFromColorPicker(hatchColorPicker, e);
            UnselectOtherColors(hatchColorPicker);
        }

        private void TextureColorPicker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            BeginChangeColorFromColorPicker(textureColorPicker, e);
            UnselectOtherColors(textureColorPicker);
        }

        private void GradientPicker_SelectedBrushChanged(object sender, EventArgs e)
        {
            BeginChangeColorFromColorPicker(gradientPicker, e);
            UnselectOtherColors(gradientPicker);
        }

        private void BeginChangeColorFromColorPicker(Control colorPicker, EventArgs e)
        {
            //Dispatcher.BeginInvoke(parameters =>
            {
                Tuple<Control, EventArgs> args = (Tuple<Control, EventArgs>)new Tuple<Control, EventArgs>(colorPicker, e);
                ChangeColorFromColorPicker(args.Item1, args.Item2);
            }//, new Tuple<Control, EventArgs>(colorPicker, e), DispatcherPriority.Background);
        }

        private void ChangeColorFromColorPicker(Control colorPicker, EventArgs e)
        {
            if (colorPicker is SolidColorPicker)
            {
                SolidColorPicker solidColorPicker = (SolidColorPicker)colorPicker;
                Color newColor = solidColorPicker.SelectedColor;
                GeoColor newGeoColor = GeoColor2MediaColorConverter.ConvertBack(newColor);

                hatchColorPicker.SyncBaseColor(DrawingColorToMediaColorConverter.ConvertBack(newColor));
                gradientPicker.SyncBaseColor(DrawingColorToMediaColorConverter.ConvertBack(newColor));

                SelectedBrush = new GeoSolidBrush(newGeoColor);
            }
            else if (colorPicker is HatchPicker)
            {
                HatchPicker hatchPicker = (HatchPicker)colorPicker;
                System.Drawing.Drawing2D.HatchStyle drawingHatchStyle = hatchPicker.SelectedHatchStyle;
                GeoHatchStyle geoHatchStyle = GeoHatchStyle2DrawingHatchStyle.ConvertBack(drawingHatchStyle);

                //solidColorPicker.SyncBaseColor(hatchPicker.ForegroundColor);
                gradientPicker.SyncBaseColor(hatchPicker.ForegroundColor);

                SelectedBrush = new GeoHatchBrush(geoHatchStyle, GeoColor2DrawingColorConverter.ConvertBack(hatchPicker.ForegroundColor)
                    , GeoColor2DrawingColorConverter.ConvertBack(hatchColorPicker.BackgroundColor));
            }
            else if (colorPicker is TexturePicker)
            {
                PropertyChangedEventArgs args = (PropertyChangedEventArgs)e;
                TexturePicker texturePicker = (TexturePicker)colorPicker;
                if (args.PropertyName == "SelectedBrush")
                {
                    ImageBrush mediaBrush = texturePicker.SelectedBrush as ImageBrush;
                    if (mediaBrush != null)
                    {
                        BitmapImage imageSource = (BitmapImage)mediaBrush.ImageSource;
                        if (imageSource != null)
                        {
                            SelectedBrush = new GeoTextureBrush(new GeoImage(imageSource.UriSource.LocalPath));
                        }
                    }
                }
            }
            else if (colorPicker is DrawingLinearGradientBrushPicker)
            {
                DrawingLinearGradientBrushPicker linearPicker = (DrawingLinearGradientBrushPicker)colorPicker;
                LinearGradientBrushEntity brushEntity = linearPicker.SelectedBrush;

                solidColorPicker.SyncBaseColor(brushEntity.StartColor);
                hatchColorPicker.SyncBaseColor(brushEntity.StartColor);

                SelectedBrush = new GeoLinearGradientBrush(GeoColor2DrawingColorConverter.ConvertBack(brushEntity.StartColor),
                    GeoColor2DrawingColorConverter.ConvertBack(brushEntity.EndColor),
                    brushEntity.Angle);
            }
        }

        private static void SelectTabItem(TabItem tabItem)
        {
            if (tabItem != null && !tabItem.IsSelected)
            {
                tabItem.IsSelected = true;
            }
        }

        private static void OnSelectedBrushPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            TabColorPicker currentInstance = (TabColorPicker)sender;
            if (!currentInstance.isTemplateApplied) return;

            GeoBrush geoBrush = (GeoBrush)e.NewValue;
            currentInstance.SetSelectedBrush(geoBrush);
        }

        private static ObservableCollection<SolidColorBrush> CreateDefaultPalette()
        {
            if (defaultColors == null)
            {
                string defaultColorsPathFileName = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "DefaultColors.xml");
                lock (string.Intern(defaultColorsPathFileName))
                {
                    if (!File.Exists(defaultColorsPathFileName))
                    {
                        Stream colorStream = Application.GetResourceStream(new Uri("/GisEditorPluginCore;component/Shares/UIs/ColorPickers/DefaultColors.xml", UriKind.Relative)).Stream;
                        XElement.Load(colorStream).Save(defaultColorsPathFileName);
                    }
                }

                XElement defaultColorsX = XElement.Load(defaultColorsPathFileName);
                IEnumerable<XElement> colors = defaultColorsX.Elements("Color");

                defaultColors = new ObservableCollection<SolidColorBrush>();
                foreach (XElement colorX in colors)
                {
                    string a = colorX.Element("A").Value;
                    string r = colorX.Element("R").Value;
                    string g = colorX.Element("G").Value;
                    string b = colorX.Element("B").Value;
                    Color color = Color.FromArgb(byte.Parse(a), byte.Parse(r), byte.Parse(g), byte.Parse(b));
                    defaultColors.Add(new SolidColorBrush(color));
                }
            }

            return defaultColors;
        }
    }
}