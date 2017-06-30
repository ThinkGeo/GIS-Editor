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
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class LogoViewModel : ViewModelBase
    {
        private AdornmentLocation selectedLocation;
        private LogoSizeMode selectedSizeMode;
        private LogoMode selectedLogoMode;
        private string logoPath;
        private int left;
        private int top;
        private int width;
        private int height;

        [NonSerialized]
        private RelayCommand browseCommand;
        [NonSerialized]
        private RelayCommand okCommand;
        [NonSerialized]
        private RelayCommand applyCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;

        public LogoViewModel()
        {
            if (GisEditor.ActiveMap != null)
            {
                AdornmentLogo logoTool = GisEditor.ActiveMap.MapTools.OfType<AdornmentLogo>().FirstOrDefault();
                if (logoTool != null)
                {
                    SelectedLocation = logoTool.AdornmentLocation;
                    SelectedLogoMode = logoTool.IsEnabled ? LogoMode.Custom : LogoMode.None;
                    SelectedSizeMode = logoTool.ImageStretchMode == Stretch.None ? LogoSizeMode.Auto : LogoSizeMode.FixedSize;
                    Width = (int)logoTool.Width;
                    Height = (int)logoTool.Height;
                    if (logoTool.IsEnabled)
                    {
                        Left = logoTool.Left;
                        Top = logoTool.Top;
                    }
                    LogoPath = logoTool.LogoPath;
                }
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
                        NotificationMessageAction<string> message = new NotificationMessageAction<string>("Image File (*.jpg;*.png;*.bmp)|*.jpg;*.png;*.bmp", (fileName) => LogoPath = fileName);
                        MessengerInstance.Send(message, this);
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

        public LogoMode SelectedLogoMode
        {
            get { return selectedLogoMode; }
            set
            {
                selectedLogoMode = value;
                if (selectedLogoMode == LogoMode.None) LogoPath = "";
                RaisePropertyChanged(()=>SelectedLogoMode);
                RaisePropertyChanged(()=>Preview);
                RaisePropertyChanged(()=>CanConfirm);
            }
        }

        public string LogoPath
        {
            get { return logoPath; }
            set
            {
                logoPath = value;
                RaisePropertyChanged(()=>LogoPath);
                RaisePropertyChanged(()=>Preview);
                RaisePropertyChanged(()=>CanConfirm);
            }
        }

        public bool CanConfirm
        {
            get
            {
                if (SelectedLogoMode == LogoMode.None) return true;
                else
                {
                    if (String.IsNullOrEmpty(LogoPath) || !File.Exists(LogoPath))
                    {
                        return false;
                    }
                    else return true;
                }
            }
        }

        public AdornmentLocation SelectedLocation
        {
            get { return selectedLocation; }
            set
            {
                selectedLocation = value;
                RaisePropertyChanged(()=>SelectedLocation);
                RaisePropertyChanged(()=>IsLocationSettingEnabled);
            }
        }

        public LogoSizeMode SelectedSizeMode
        {
            get { return selectedSizeMode; }
            set
            {
                selectedSizeMode = value;
                RaisePropertyChanged(()=>SelectedSizeMode);
                RaisePropertyChanged(()=>IsFixedSizeSettingEnabled);
            }
        }

        public ImageSource Preview
        {
            get
            {
                BitmapImage imageSource = new BitmapImage();
                if (SelectedLogoMode != LogoMode.None && !String.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
                {
                    MemoryStream streamSource = new MemoryStream(File.ReadAllBytes(LogoPath));
                    imageSource.BeginInit();
                    imageSource.StreamSource = streamSource;
                    imageSource.EndInit();

                    if (SelectedSizeMode == LogoSizeMode.Auto)
                    {
                        Width = imageSource.PixelWidth;
                        Height = imageSource.PixelHeight;
                    }
                }
                else
                {
                    Width = 0;
                    Height = 0;
                }
                return imageSource;
            }
        }

        public bool IsLocationSettingEnabled
        {
            get { return SelectedLocation == AdornmentLocation.UseOffsets; }
        }

        public bool IsFixedSizeSettingEnabled
        {
            get { return SelectedSizeMode == LogoSizeMode.FixedSize; }
        }

        public int Left { get { return left; } set { left = value; RaisePropertyChanged(()=>Left); } }

        public int Top { get { return top; } set { top = value; RaisePropertyChanged(()=>Top); } }

        public int Width { get { return width; } set { width = value; RaisePropertyChanged(()=>Width); } }

        public int Height { get { return height; } set { height = value; RaisePropertyChanged(()=>Height); } }

        public void Apply()
        {
            AdornmentLogo logoTool = GisEditor.ActiveMap.MapTools.OfType<AdornmentLogo>().FirstOrDefault();
            if (logoTool != null)
            {
                if (SelectedLogoMode == LogoMode.None)
                {
                    logoTool.IsEnabled = false;
                    logoTool.LogoPath = "";
                }
                else
                {
                    logoTool.IsEnabled = true;
                    logoTool.Source = Preview;
                    logoTool.Left = Left;
                    logoTool.Top = Top;
                    logoTool.AdornmentLocation = SelectedLocation;
                    logoTool.LogoPath = LogoPath;
                    if (SelectedSizeMode == LogoSizeMode.Auto)
                    {
                        logoTool.ImageStretchMode = Stretch.None;
                        logoTool.Width = logoTool.Source.Width;
                        logoTool.Height = logoTool.Source.Height;
                    }
                    else
                    {
                        logoTool.Width = Width;
                        logoTool.Height = Height;
                        logoTool.ImageStretchMode = Stretch.Fill;
                    }
                }
            }
        }
    }
}