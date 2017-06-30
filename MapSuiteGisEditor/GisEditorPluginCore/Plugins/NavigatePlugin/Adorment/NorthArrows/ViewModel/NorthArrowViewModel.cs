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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class NorthArrowViewModel : ViewModelBase
    {
        private static string emfImagePath = "pack://application:,,,/GisEditorPluginCore;component/Images/NorthArrow/NorthArrow{0:D2}.emf";
        private static string gridImagePath = "pack://application:,,,/GisEditorPluginCore;component/Images/NorthArrow/arrow_type{0:D2}_grid.png";
        private static string previewImagePath = "pack://application:,,,/GisEditorPluginCore;component/Images/NorthArrow/arrow_type{0:D2}_preview.png";
        private static string pattern = "(?<=pack://application:,,,/GisEditorPluginCore;component/Images/NorthArrow/NorthArrow)\\d{2}(?=.emf)";

        private double arrowLeft;
        private double arrowTop;
        private double arrowWidth;
        private double arrowHeight;
        private AdornmentLocation arrowLocation;
        private LogoSizeMode arrowSize;
        private bool isCustomSizeEnable;
        private ObservableCollection<string> images;
        private string selectedImage;

        [NonSerialized]
        private RelayCommand browseCommand;
        [NonSerialized]
        private RelayCommand okCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;
        [NonSerialized]
        private RelayCommand applyCommand;

        public NorthArrowViewModel()
        {
            ArrowLocation = AdornmentLocation.LowerRight;
            ArrowSize = LogoSizeMode.Auto;
            images = new ObservableCollection<string>();
            for (int i = 0; i < 16; i++)
            {
                images.Add(string.Format(gridImagePath, i));
            }
            SelectedImage = Images[0];

            NorthArrowMapTool northMapTool = GisEditor.ActiveMap.MapTools.OfType<NorthArrowMapTool>().FirstOrDefault();
            if (northMapTool != null)
            {
                SetProperties(northMapTool);
            }
        }

        public RelayCommand BrowseCommand
        {
            get
            {
                if (browseCommand == null)
                {
                    browseCommand = new RelayCommand(() =>
                    {
                        NotificationMessageAction<string> message = new NotificationMessageAction<string>("Image Files(*.jpg;*.bmp;*.png;*.gif;*.emf;*.wmf)|*.jpg;*.bmp;*.png;*.gif;*.emf;*.wmf",
                            (fileName) =>
                            {
                                // unselect the previous selection.
                                SelectedImage = null;
                                SelectedImage = fileName;
                            });

                        MessengerInstance.Send(message);
                    });
                }
                return browseCommand;
            }
        }

        public RelayCommand OKCommand
        {
            get
            {
                if (okCommand == null)
                {
                    okCommand = new RelayCommand(() =>
                    {
                        Apply();
                        MessengerInstance.Send(GisEditor.LanguageManager.GetStringResource("CloseWindowMessage"), this);
                    });
                }
                return okCommand;
            }
        }

        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(() =>
                    {
                        MessengerInstance.Send(GisEditor.LanguageManager.GetStringResource("CloseWindowMessage"), this);
                    });
                }
                return cancelCommand;
            }
        }

        public RelayCommand ApplyCommand
        {
            get
            {
                if (applyCommand == null)
                {
                    applyCommand = new RelayCommand(Apply);
                }
                return applyCommand;
            }
        }

        public string DisplayPathInText
        {
            get
            {
                if (Images.Contains(SelectedImage))
                {
                    return string.Empty;
                }
                else
                {
                    return selectedImage;
                }
            }
        }

        public ImageSource PreviewImage
        {
            get
            {
                BitmapImage imageSource = new BitmapImage();
                if (selectedImage == null)
                {
                    return null;
                }
                else if (selectedImage == Images[0])
                {
                    return null;
                }
                else if (selectedImage.StartsWith("pack://application"))
                {
                    var uri = new Uri(string.Format(CultureInfo.InvariantCulture, previewImagePath, Images.IndexOf(selectedImage)), UriKind.RelativeOrAbsolute);
                    var stream = Application.GetResourceStream(uri).Stream;
                    stream.Seek(0, SeekOrigin.Begin);
                    imageSource.BeginInit();
                    imageSource.StreamSource = stream;
                    imageSource.EndInit();
                }
                else if (File.Exists(selectedImage))
                {
                    var stream = new MemoryStream(File.ReadAllBytes(selectedImage));
                    stream.Seek(0, SeekOrigin.Begin);
                    imageSource.BeginInit();
                    imageSource.StreamSource = stream;
                    imageSource.EndInit();
                }
                return imageSource;
            }
        }

        public double ArrowLeft
        {
            get { return arrowLeft; }
            set
            {
                arrowLeft = value;
                RaisePropertyChanged(()=>ArrowLeft);
            }
        }

        public double ArrowTop
        {
            get { return arrowTop; }
            set
            {
                arrowTop = value;
                RaisePropertyChanged(()=>ArrowTop);
            }
        }

        public double ArrowWidth
        {
            get { return arrowWidth; }
            set
            {
                arrowWidth = value;
                RaisePropertyChanged(()=>ArrowWidth);
            }
        }

        public double ArrowHeight
        {
            get { return arrowHeight; }
            set
            {
                arrowHeight = value;
                RaisePropertyChanged(()=>ArrowHeight);
            }
        }

        public AdornmentLocation ArrowLocation
        {
            get { return arrowLocation; }
            set
            {
                arrowLocation = value;
                RaisePropertyChanged(()=>ArrowLocation);
            }
        }

        public LogoSizeMode ArrowSize
        {
            get { return arrowSize; }
            set
            {
                arrowSize = value;
                RaisePropertyChanged(()=>ArrowSize);
                IsCustomSizeEnable = arrowSize == LogoSizeMode.FixedSize;
            }
        }

        public bool IsCustomSizeEnable
        {
            get { return isCustomSizeEnable; }
            set
            {
                isCustomSizeEnable = value;
                RaisePropertyChanged(()=>IsCustomSizeEnable);
            }
        }

        public bool IsSizeAndPositionEnable
        {
            get { return !string.IsNullOrEmpty(SelectedImage) && SelectedImage != Images[0]; }
        }

        public ObservableCollection<string> Images
        {
            get { return images; }
        }

        public string SelectedImage
        {
            get { return selectedImage; }
            set
            {
                selectedImage = value;
                RaisePropertyChanged(()=>SelectedImage);
                RaisePropertyChanged(()=>DisplayPathInText);
                RaisePropertyChanged(()=>IsSizeAndPositionEnable);
                RaisePropertyChanged(()=>PreviewImage);
            }
        }

        public string ActualImagePath
        {
            get
            {
                if (selectedImage == Images[0])
                {
                    return null;
                }
                else if (selectedImage.StartsWith("pack://application"))
                {
                    return string.Format(CultureInfo.InvariantCulture, emfImagePath, Images.IndexOf(selectedImage.Replace("_preview", "_grid")));
                }
                else
                {
                    return selectedImage;
                }
            }
        }

        public void SetPropertiesForNorthArrowMapTool(NorthArrowMapTool northArrowMapTool)
        {
            int width = ArrowSize == LogoSizeMode.Auto ? 0 : (int)ArrowWidth;
            int height = ArrowSize == LogoSizeMode.Auto ? 0 : (int)ArrowHeight;

            northArrowMapTool.Content = new Image() { Source = ImageHelper.GetImageSource(ActualImagePath, width, height) };
            northArrowMapTool.ImagePath = ActualImagePath;
            if (PreviewImage != null)
            {
                double originalWidth = ((Image)northArrowMapTool.Content).Source.Width;
                double originalHeight = ((Image)northArrowMapTool.Content).Source.Height;
                northArrowMapTool.Width = ArrowSize == LogoSizeMode.Auto ? originalWidth : ArrowWidth;
                northArrowMapTool.Height = ArrowSize == LogoSizeMode.Auto ? originalHeight : ArrowHeight;
            }

            northArrowMapTool.HorizontalAlignment = GetHorizontalAlignment(ArrowLocation);
            northArrowMapTool.VerticalAlignment = GetVerticalAlignment(ArrowLocation);
            northArrowMapTool.Margin = new Thickness(ArrowLeft, ArrowTop, -ArrowLeft, -ArrowTop);
        }

        internal static AdornmentLocation GetAdornmentLocation(HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
        {
            if (horizontalAlignment == HorizontalAlignment.Center)
            {
                switch (verticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                    default:
                        return AdornmentLocation.LowerCenter;
                    case VerticalAlignment.Center:
                        return AdornmentLocation.Center;
                    case VerticalAlignment.Top:
                        return AdornmentLocation.UpperCenter;
                }
            }
            else if (horizontalAlignment == HorizontalAlignment.Left)
            {
                switch (verticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                    default:
                        return AdornmentLocation.LowerLeft;
                    case VerticalAlignment.Center:
                        return AdornmentLocation.CenterLeft;
                    case VerticalAlignment.Top:
                        return AdornmentLocation.UpperLeft;
                }
            }
            else
            {
                switch (verticalAlignment)
                {
                    case VerticalAlignment.Bottom:
                    default:
                        return AdornmentLocation.LowerRight;
                    case VerticalAlignment.Center:
                        return AdornmentLocation.CenterRight;
                    case VerticalAlignment.Top:
                        return AdornmentLocation.UpperRight;
                }
            }
        }
    
        private void Apply()
        {
            NorthArrowMapTool northArrowMapTool = GisEditor.ActiveMap.MapTools.OfType<NorthArrowMapTool>().FirstOrDefault();
            if (northArrowMapTool == null)
            {
                northArrowMapTool = new NorthArrowMapTool();
                GisEditor.ActiveMap.MapTools.Add(northArrowMapTool);
            }
            SetPropertiesForNorthArrowMapTool(northArrowMapTool);
        }

        private void SetProperties(NorthArrowMapTool maptool)
        {
            Image image = maptool.Content as Image;
            if (image != null)
            {
                if (image.Source == null)
                {
                    ArrowSize = LogoSizeMode.Auto;
                }
                else
                {
                    //make the SelectedItem of ListBox unselected
                    SelectedImage = null;
                    if (!string.IsNullOrEmpty(maptool.ImagePath) && Regex.IsMatch(maptool.ImagePath, pattern))
                    {
                        Match match = Regex.Match(maptool.ImagePath, pattern);
                        SelectedImage = string.Format(CultureInfo.InvariantCulture, gridImagePath, Convert.ToInt32(match.Value));
                    }
                    else
                    {
                        SelectedImage = maptool.ImagePath;
                    }
                    ArrowSize = (maptool.Width == image.Source.Width && maptool.Height == image.Source.Height) ? LogoSizeMode.Auto : LogoSizeMode.FixedSize;
                }

                if (ArrowSize == LogoSizeMode.FixedSize)
                {
                    ArrowWidth = maptool.Width;
                    ArrowHeight = maptool.Height;
                }
            }

            ArrowLocation = GetAdornmentLocation(maptool.HorizontalAlignment, maptool.VerticalAlignment);
            ArrowLeft = maptool.Margin.Left;
            ArrowTop = maptool.Margin.Top;
        }

        private HorizontalAlignment GetHorizontalAlignment(AdornmentLocation location)
        {
            if (location == AdornmentLocation.Center || location == AdornmentLocation.LowerCenter || location == AdornmentLocation.UpperCenter)
            {
                return HorizontalAlignment.Center;
            }
            else if (location == AdornmentLocation.CenterLeft || location == AdornmentLocation.LowerLeft || location == AdornmentLocation.UpperLeft)
            {
                return HorizontalAlignment.Left;
            }
            else
            {
                return HorizontalAlignment.Right;
            }
        }

        private VerticalAlignment GetVerticalAlignment(AdornmentLocation location)
        {
            if (location == AdornmentLocation.Center || location == AdornmentLocation.CenterLeft || location == AdornmentLocation.CenterRight)
            {
                return VerticalAlignment.Center;
            }
            else if (location == AdornmentLocation.LowerCenter || location == AdornmentLocation.LowerLeft || location == AdornmentLocation.LowerRight)
            {
                return VerticalAlignment.Bottom;
            }
            else
            {
                return VerticalAlignment.Top;
            }
        }
    }
}