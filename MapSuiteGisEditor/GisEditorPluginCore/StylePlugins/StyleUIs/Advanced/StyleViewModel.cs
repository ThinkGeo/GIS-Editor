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
using System.ComponentModel;
using System.Windows.Media.Imaging;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class StyleViewModel : ObjectViewModel<Style>, IStyleTypeChangeable //where T : Style
    {
        [NonSerialized]
        private BitmapImage previewSource;
        private Dictionary<string, StylePlugin> availableStyleTypesToChange;
        private KeyValuePair<string, StylePlugin> selectedStyleType;
        private string helpKey;

        protected StyleViewModel(Style actualStyle)
            : base(actualStyle)
        {
            availableStyleTypesToChange = new Dictionary<string, StylePlugin>();
            ActualObject = actualStyle;
            PropertyChanged += new PropertyChangedEventHandler(StyleViewModel_PropertyChanged);
            if (actualStyle != null)
            {
                PreviewSource = actualStyle.GetPreviewImage();
            }
        }

        public string Name
        {
            get { return ActualObject.Name; }
            set
            {
                ActualObject.Name = value;
                RaisePropertyChanged("Name");
            }
        }

        public string HelpKey
        {
            get { return helpKey; }
            set
            {
                helpKey = value;
                RaisePropertyChanged("HelpKey");
            }
        }

        public System.Windows.Visibility HelpButtonVisibility
        {
            get { return string.IsNullOrEmpty(HelpKey) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible; }
        }

        public BitmapImage PreviewSource
        {
            get { return previewSource; }
            set
            {
                previewSource = value;
                RaisePropertyChanged("PreviewSource");
                RaisePropertyChanged("PreviewSmallSource");
            }
        }

        public BitmapImage PreviewSmallSource
        {
            get
            {
                if (ActualObject != null) return ActualObject.GetPreviewImage(16, 16);
                else return new BitmapImage();
            }
        }

        public KeyValuePair<string, StylePlugin> SelectedStyleType
        {
            get { return selectedStyleType; }
            set
            {
                selectedStyleType = value;
                RaisePropertyChanged("SelectedStyleType");
            }
        }

        protected virtual void StyleViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (CanRefreshPreviewSource(e.PropertyName))
            {
                PreviewSource = ActualObject.GetPreviewImage();
            }
        }

        public bool CanRefreshPreviewSource(string propertyName)
        {
            return !propertyName.Equals("PreviewSource")
                && !propertyName.Equals("HelpKey")
                && !propertyName.Equals("PreviewSmallSource");
        }
    }
}