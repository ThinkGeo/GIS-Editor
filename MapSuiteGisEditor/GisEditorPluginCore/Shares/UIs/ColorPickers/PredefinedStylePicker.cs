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


using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class PredefinedStylePicker : Control
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(GeoStyleViewModel), typeof(PredefinedStylePicker), new UIPropertyMetadata(null, new PropertyChangedCallback(PredefinedSelectedItemChanged)));

        public static readonly DependencyProperty PredefinedStyleTypeProperty =
            DependencyProperty.Register("PredefinedStyleType", typeof(PredefinedStyleType), typeof(PredefinedStylePicker), new UIPropertyMetadata(PredefinedStyleType.Unknown, new PropertyChangedCallback(PredefinedStyleTypeChanged)));

        public static void PredefinedSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            GeoStyleViewModel geoStyleEntity = (GeoStyleViewModel)e.NewValue;
            PredefinedStylePicker control = (PredefinedStylePicker)sender;
            if (e.OldValue != null && geoStyleEntity != null && ((GeoStyleViewModel)e.OldValue).Name != geoStyleEntity.Name)
            {
                control.SelectedItem = control.StylesSource.FirstOrDefault(i => { return i.Name == geoStyleEntity.Name; });
            }
        }

        public static void PredefinedStyleTypeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PredefinedStylePicker control = (PredefinedStylePicker)sender;
            control.stylesSource.Clear();
            switch ((PredefinedStyleType)e.NewValue)
            {
                case PredefinedStyleType.Line:
                    PropertyInfo[] lineStyleInfos = typeof(LineStyles).GetProperties();
                    foreach (var lineStyleInfo in lineStyleInfos)
                    {
                        LineStyle geoLineStyle = (LineStyle)lineStyleInfo.GetValue(null, null);
                        GeoStyleViewModel geoStyleEntity = new GeoStyleViewModel(lineStyleInfo.Name, geoLineStyle);
                        control.stylesSource.Add(geoStyleEntity);
                    }
                    break;
                case PredefinedStyleType.Point:
                    PropertyInfo[] pointStyleInfos = typeof(PointStyles).GetProperties();
                    foreach (var pointStyleInfo in pointStyleInfos)
                    {
                        PointStyle geoPointStyle = (PointStyle)pointStyleInfo.GetValue(null, null);
                        GeoStyleViewModel geoStyleEntity = new GeoStyleViewModel(pointStyleInfo.Name, geoPointStyle);
                        control.stylesSource.Add(geoStyleEntity);
                    }
                    break;
                case PredefinedStyleType.Area:
                    PropertyInfo[] areaStyleInfos = typeof(AreaStyles).GetProperties();
                    foreach (var areaStyleInfo in areaStyleInfos)
                    {
                        AreaStyle geoAreaStyle = (AreaStyle)areaStyleInfo.GetValue(null, null);
                        GeoStyleViewModel geoStyleEntity = new GeoStyleViewModel(areaStyleInfo.Name, geoAreaStyle);
                        control.stylesSource.Add(geoStyleEntity);
                    }
                    break;
            }
        }

        private ObservableCollection<GeoStyleViewModel> stylesSource;

        static PredefinedStylePicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PredefinedStylePicker), new FrameworkPropertyMetadata(typeof(PredefinedStylePicker)));
        }

        public PredefinedStylePicker()
        {
            stylesSource = new ObservableCollection<GeoStyleViewModel>();
        }

        public GeoStyleViewModel SelectedItem
        {
            get { return (GeoStyleViewModel)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public PredefinedStyleType PredefinedStyleType
        {
            get
            {
                return (PredefinedStyleType)GetValue(PredefinedStyleTypeProperty);
            }
            set
            {
                SetValue(PredefinedStyleTypeProperty, value);
            }
        }

        public ObservableCollection<GeoStyleViewModel> StylesSource
        {
            get { return stylesSource; }
        }
    }
}