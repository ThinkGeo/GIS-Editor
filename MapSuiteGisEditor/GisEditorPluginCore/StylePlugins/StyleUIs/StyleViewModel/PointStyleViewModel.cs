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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class PointStyleViewModel : StyleViewModel
    {
        private static readonly string iconFalgs = "YZ:\\";
        private static readonly string delimiter = "\\";

        private string imagePath;
        private int imageWidth;
        private int imageHeight;
        private int oImageWidth;
        private int oImageHeight;
        private int previewWidth;
        private int previewHeight;
        private IconSource iconSource;
        private PointStyle actualPointStyle;
        private Collection<BitmapImage> symbolTypeList;

        public PointStyleViewModel(PointStyle style)
            : base(style)
        {
            if (style is SymbolPointStyle) HelpKey = "CustomPointStyleHelp";
            else HelpKey = "PointStyleHelp";

            ActualObject = style;
            actualPointStyle = style;
            if (actualPointStyle.CharacterIndex == 0)
            {
                actualPointStyle.CharacterIndex = 33;
            }

            if (style is SymbolPointStyle || style is FontPointStyle)
            {
                iconSource = new IconSource(this);
                if (string.IsNullOrEmpty(ImagePath))
                {
                    if (actualPointStyle.Image != null
                        && !String.IsNullOrEmpty(actualPointStyle.Image.GetPathFilename())
                        && actualPointStyle.Image.GetPathFilename().StartsWith(iconFalgs))
                    {
                        var selectedIconUri = actualPointStyle.Image.GetPathFilename().Replace(iconFalgs, "");
                        var catogeries = selectedIconUri.Split(delimiter.ToCharArray().FirstOrDefault());
                        if (catogeries.Length == 3)
                        {
                            iconSource.SelectedCategory = iconSource.IconCategories.FirstOrDefault(c => c.CategoryName.Equals(catogeries[0]));
                            iconSource.SelectedSubCategory = iconSource.SelectedCategory.SubCategories.FirstOrDefault(c => c.CategoryName.Equals(catogeries[1]));
                            iconSource.SelectedIcon = iconSource.SelectedSubCategory.Icons.FirstOrDefault(icon => icon.IconName.Equals(catogeries[2]));
                        }
                        else if (catogeries.Length == 2)
                        {
                            var category = iconSource.IconCategories.FirstOrDefault(c => c.CategoryName.Equals(catogeries[0]));
                            if (category != null) iconSource.SelectedCategory = category;
                            var iconEntity = iconSource.SelectedCategory.Icons.FirstOrDefault(icon => icon.IconName.Equals(catogeries[1]));
                            if (iconEntity != null) iconSource.SelectedIcon = iconEntity;
                        }
                    }
                }
                else
                {
                    ImagePath = ImagePath;
                }
            }

            if (iconSource != null)
            {
                iconSource.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName.Equals("SelectedIcon"))
                    {
                        if (iconSource.SelectedIcon != null)
                        {
                            var image = iconSource.SelectedIcon.Icon as BitmapImage;
                            if (image != null)
                            {
                                MemoryStream ms = new MemoryStream();
                                Image tmpImage = null;
                                string filePath = null;
                                if (image.StreamSource == null)
                                {
                                    tmpImage = Image.FromFile(image.UriSource.AbsolutePath);
                                    filePath = image.UriSource.AbsolutePath;
                                }
                                else
                                {
                                    tmpImage = Image.FromStream(image.StreamSource);
                                }
                                tmpImage.Save(ms, ImageFormat.Png);

                                if (ms != null)
                                {
                                    actualPointStyle.Image = new GeoImage(ms);
                                }
                                else
                                {
                                    actualPointStyle.Image = new GeoImage(filePath);
                                }
                            }
                        }
                    }
                };
            }

            if (symbolTypeList == null)
            {
                symbolTypeList = GetSymbolTypeListItems();
            }
        }

        public Collection<BitmapImage> SymbolTypeList
        {
            get { return symbolTypeList; }
        }

        public string ImagePath
        {
            get
            {
                if (string.IsNullOrEmpty(imagePath))
                {
                    if (actualPointStyle.Image != null && !string.IsNullOrEmpty(actualPointStyle.Image.GetPathFilename()))
                    {
                        if (actualPointStyle.Image.GetPathFilename().StartsWith(iconFalgs))
                        {
                            return string.Empty;
                        }
                        else
                            return actualPointStyle.Image.GetPathFilename();
                    }
                    else
                        return string.Empty;
                }
                else
                {
                    return imagePath;
                }
            }
            set
            {
                imagePath = value;
                if (!String.IsNullOrEmpty(value) && StyleHelper.IsImageValid(value))
                {
                    iconSource.SelectedIcon = null;
                    var bitmapImage = new BitmapImage(new Uri(value, UriKind.RelativeOrAbsolute));
                    imageWidth = (int)bitmapImage.Width;
                    imageHeight = (int)bitmapImage.Height;
                    oImageWidth = imageWidth;
                    oImageHeight = imageHeight;
                    RaisePropertyChanged("ImageWidth");
                    RaisePropertyChanged("ImageHeight");
                }
                RaisePropertyChanged("ImagePath");
            }
        }

        public int ImageWidth
        {
            get
            {
                return imageWidth;
            }
            set
            {
                if (oImageWidth == 0) { oImageWidth = imageWidth; }
                if (oImageHeight == 0) { oImageHeight = imageHeight; }

                imageWidth = value;
                RaisePropertyChanged("ImageWidth");

                if (iconSource != null && iconSource.ConstrainProportions)
                {
                    var ratio = (int)((value * 1.0 / oImageWidth) * 10000);

                    if (oImageWidth != oImageHeight)
                    {
                        imageHeight = oImageHeight * ratio / 10000 + 1;
                        if (imageHeight == 0) imageHeight = 1;
                    }
                    else
                    {
                        imageHeight = imageWidth;
                    }
                    RaisePropertyChanged("ImageHeight");
                }

                if (value > 0 && ImageHeight > 0)
                {
                    ResizeSymbol();
                }
            }
        }

        public GeoImage ActualImage
        {
            get { return actualPointStyle.Image; }
        }

        public int ImageHeight
        {
            get
            {
                return imageHeight;
            }
            set
            {
                if (oImageHeight == 0) { oImageHeight = imageHeight; }
                if (oImageWidth == 0) { oImageWidth = imageWidth; }

                imageHeight = value;
                RaisePropertyChanged("ImageHeight");

                if (iconSource != null && iconSource.ConstrainProportions)
                {
                    var ratio = (int)((value * 1.0 / oImageHeight) * 10000);

                    if (oImageWidth != oImageHeight)
                    {
                        imageWidth = imageHeight;
                    }
                    else
                    {
                        imageWidth = oImageWidth * ratio / 10000 + 1;
                        if (imageWidth == 0) imageWidth = 1;
                    }
                    RaisePropertyChanged("ImageWidth");
                }

                if (value > 0 && ImageWidth > 0)
                {
                    ResizeSymbol();
                }
            }
        }

        public int PreviewHeight
        {
            get { return previewHeight; }
            set { previewHeight = value; RaisePropertyChanged("PreviewHeight"); }
        }

        public int PreviewWidth
        {
            get { return previewWidth; }
            set { previewWidth = value; RaisePropertyChanged("PreviewWidth"); }
        }

        public int SymbolTypeIndex
        {
            get
            {
                return (int)actualPointStyle.SymbolType;
            }
            set
            {
                actualPointStyle.SymbolType = (PointSymbolType)value;
                RaisePropertyChanged("SymbolTypeIndex");
            }
        }

        public float SymbolSize
        {
            get
            {
                return actualPointStyle.SymbolSize;
            }
            set
            {
                actualPointStyle.SymbolSize = value;
                RaisePropertyChanged("SymbloSize");
            }
        }

        public GeoBrush FillColor
        {
            get
            {
                return actualPointStyle.Advanced.CustomBrush != null ? actualPointStyle.Advanced.CustomBrush : actualPointStyle.SymbolSolidBrush;
            }
            set
            {
                if (value is GeoSolidBrush)
                {
                    actualPointStyle.SymbolSolidBrush = (GeoSolidBrush)value;
                    actualPointStyle.Advanced.CustomBrush = null;
                }
                else
                {
                    actualPointStyle.Advanced.CustomBrush = value;
                }
                RaisePropertyChanged("FillColor");
            }
        }

        public GeoBrush OutlineColor
        {
            get
            {
                return actualPointStyle.SymbolPen.Brush;
            }
            set
            {
                actualPointStyle.SymbolPen.Brush = value;
                RaisePropertyChanged("OutlineColor");
            }
        }

        public float OutlineThickness
        {
            get
            {
                return actualPointStyle.SymbolPen.Width;
            }
            set
            {
                actualPointStyle.SymbolPen.Width = value;
                RaisePropertyChanged("OutlineThickness");
            }
        }

        public int CharacterIndex
        {
            get
            {
                return actualPointStyle.CharacterIndex;
            }
            set
            {
                actualPointStyle.CharacterIndex = value;
                RaisePropertyChanged("CharacterIndex");
            }
        }

        public System.Windows.Media.FontFamily CharacterFontFamily
        {
            get
            {
                return new System.Windows.Media.FontFamily(actualPointStyle.CharacterFont.FontName);
            }
            set
            {
                actualPointStyle.CharacterFont = new GeoFont(value.Source, actualPointStyle.CharacterFont.Size, actualPointStyle.CharacterFont.Style);
                RaisePropertyChanged("CharacterFontName");
            }
        }

        public int CharacterFontSize
        {
            get
            {
                return (int)actualPointStyle.CharacterFont.Size;
            }
            set
            {
                actualPointStyle.CharacterFont = new GeoFont(actualPointStyle.CharacterFont.FontName, value, actualPointStyle.CharacterFont.Style);
                actualPointStyle.SymbolSize = value;
                RaisePropertyChanged("CharacterFontSize");
            }
        }

        public DrawingFontStyles CharacterFontStyle
        {
            get
            {
                return actualPointStyle.CharacterFont.Style;
            }
            set
            {
                actualPointStyle.CharacterFont = new GeoFont(actualPointStyle.CharacterFont.FontName, actualPointStyle.CharacterFont.Size, value);
                RaisePropertyChanged("CharacterFontStyleIndex");
            }
        }

        public GeoBrush CharacterBrush
        {
            get
            {
                return actualPointStyle.Advanced.CustomBrush != null ? actualPointStyle.Advanced.CustomBrush : actualPointStyle.CharacterSolidBrush;
            }
            set
            {
                if (value is GeoSolidBrush)
                {
                    actualPointStyle.CharacterSolidBrush = (GeoSolidBrush)value;
                    actualPointStyle.Advanced.CustomBrush = null;
                }
                else
                {
                    actualPointStyle.Advanced.CustomBrush = value;
                    actualPointStyle.CharacterSolidBrush = null;
                }
                RaisePropertyChanged("CharacterBrush");
            }
        }

        public IconSource IconSource
        {
            get { return iconSource; }
        }

        public DrawingLevel DrawingLevel
        {
            get { return actualPointStyle.DrawingLevel; }
            set
            {
                actualPointStyle.DrawingLevel = value;
            }
        }

        public float XOffsetInPixel
        {
            get { return actualPointStyle.XOffsetInPixel; }
            set
            {
                actualPointStyle.XOffsetInPixel = value;
                RaisePropertyChanged("XOffsetInPixel");
            }
        }

        public float YOffsetInPixel
        {
            get { return actualPointStyle.YOffsetInPixel; }
            set
            {
                actualPointStyle.YOffsetInPixel = value;
                RaisePropertyChanged("YOffsetInPixel");
            }
        }

        protected override void StyleViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (actualPointStyle is SymbolPointStyle && CanRefreshPreviewSource(e.PropertyName))
            {
                ResizeSymbol();
            }
            else
            {
                base.StyleViewModel_PropertyChanged(sender, e);
            }
        }

        private void ResizeSymbol()
        {
            Stream stream = null;
            Bitmap bitmap = null;
            if (iconSource != null && iconSource.SelectedIcon != null)
            {
                var bitmapImage = iconSource.SelectedIcon.Icon as BitmapImage;
                if (bitmapImage != null)
                {
                    stream = bitmapImage.StreamSource;
                }
            }
            else if (!string.IsNullOrEmpty(ImagePath))
            {
                stream = new MemoryStream(File.ReadAllBytes(ImagePath));
                //actualPointStyle.Image.SetPathFilename(imagePath);

                actualPointStyle.SetImageIconPath(imagePath);
            }
            else
                return;

            try
            {
                bitmap = new Bitmap(System.Drawing.Image.FromStream(stream), new System.Drawing.Size(imageWidth, imageHeight));
                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.EndInit();

                PreviewSource = bitmapImage;
                actualPointStyle.Image = new GeoImage(ms);

                if (string.IsNullOrEmpty(actualPointStyle.Image.GetPathFilename()) && IconSource.SelectedIcon != null && ms == null)
                {
                    var selectedSubCategoryName = IconSource.SelectedCategory.HasSubCategories ? IconSource.SelectedSubCategory.CategoryName + delimiter : "";
                    string selectedIconUri = iconFalgs + IconSource.SelectedCategory.CategoryName + delimiter + selectedSubCategoryName + IconSource.SelectedIcon.IconName;
                    //actualPointStyle.Image.SetPathFilename(selectedIconUri);

                    actualPointStyle.SetImageIconPath(selectedIconUri);
                }
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
            }
            finally
            {
                if (bitmap != null) bitmap.Dispose();
            }
        }

        private Collection<BitmapImage> GetSymbolTypeListItems()
        {
            Collection<BitmapImage> images = new Collection<BitmapImage>();

            PointStyle pointStyle = new PointStyle();
            pointStyle.SymbolSize = 14;
            pointStyle.SymbolSolidBrush = new GeoSolidBrush(GeoColor.StandardColors.Black);
            pointStyle.SymbolPen = new GeoPen(GeoColor.SimpleColors.Black);
            var bufferUnitNames = Enum.GetValues(typeof(PointSymbolType));
            foreach (var item in bufferUnitNames)
            {
                pointStyle.SymbolType = (PointSymbolType)item;
                BitmapImage bitmapImage = pointStyle.GetPreviewImage(16, 16);
                images.Add(bitmapImage);
            }

            return images;
        }
    }
}