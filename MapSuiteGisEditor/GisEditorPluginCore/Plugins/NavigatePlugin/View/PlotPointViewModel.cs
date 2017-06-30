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
using System.Text;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Windows.Media.Imaging;
using System.Net.Cache;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class PlotPointViewModel : ViewModelBase
    {
        private static string errorMessage = GisEditor.LanguageManager.GetStringResource("NavigateRibbonGroupInvalidCoordinatesText");

        private string exampleX;
        private string exampleY;
        private CoordinateType selectedCoordinateFormat;
        private string latitude;
        private string longitude;
        private string latitudeDegrees;
        private string latitudeMinutes;
        private string latitudeSeconds;
        private string longitudeDegrees;
        private string longitudeMinutes;
        private string longitudeSeconds;
        private string longitudeType;
        private string latitudeType;
        private string pointLable;
        private Vertex resultVertex;
        [NonSerialized]
        private RelayCommand plotPointCommand;
        [NonSerialized]
        private RelayCommand cancelCommand;

        public PlotPointViewModel()
        {
            SelectedCoordinateFormat = CoordinateType.DecimalDegrees;
        }

        public string ExampleX
        {
            get { return exampleX; }
            set
            {
                exampleX = value;
                RaisePropertyChanged(() => ExampleX);
            }
        }

        public string ExampleY
        {
            get { return exampleY; }
            set
            {
                exampleY = value;
                RaisePropertyChanged(() => ExampleY);
            }
        }

        public RelayCommand PlotPointCommand
        {
            get
            {
                if (plotPointCommand == null)
                {
                    plotPointCommand = new RelayCommand(() =>
                    {
                        try
                        {
                            PlotPoint();
                            Messenger.Default.Send(true, this);
                        }
                        catch (Exception ex)
                        {
                            GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                            System.Windows.Forms.MessageBox.Show(ex.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                        }
                    });
                }
                return plotPointCommand;
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
                        Messenger.Default.Send(true, this);
                    });
                }
                return cancelCommand;
            }
        }

        public CoordinateType SelectedCoordinateFormat
        {
            get { return selectedCoordinateFormat; }
            set
            {
                selectedCoordinateFormat = value;
                RaisePropertyChanged(() => SelectedCoordinateFormat);
                switch (selectedCoordinateFormat)
                {
                    case CoordinateType.DegreesMinutes:
                        ExampleX = "Ex: 96°49.697'W";
                        ExampleY = "Ex: 30°6.165'N";
                        break;
                    case CoordinateType.DegreesMinutesSeconds:
                        ExampleX = "Ex: 96°49'41.8\"W";
                        ExampleY = "Ex: 33°6'9.9\"N";
                        break;
                    case CoordinateType.XY:
                        ExampleX = "Ex: -96.8281";
                        ExampleY = "Ex: 33.1026";
                        break;
                    case CoordinateType.DecimalDegrees:
                    default:
                        ExampleX = "Ex: -96.828278";
                        ExampleY = "Ex: 30.10275";
                        break;
                }
            }
        }

        public string Latitude
        {
            get { return latitude; }
            set
            {
                latitude = value;
                RaisePropertyChanged(() => Latitude);
            }
        }

        public string Longitude
        {
            get { return longitude; }
            set
            {
                longitude = value;
                RaisePropertyChanged(() => Longitude);
            }
        }

        public string LatitudeDegrees
        {
            get { return latitudeDegrees; }
            set
            {
                latitudeDegrees = value;
                RaisePropertyChanged(() => LatitudeDegrees);
            }
        }

        public string LatitudeMinutes
        {
            get { return latitudeMinutes; }
            set
            {
                latitudeMinutes = value;
                RaisePropertyChanged(() => LatitudeMinutes);
            }
        }

        public string LatitudeSeconds
        {
            get { return latitudeSeconds; }
            set
            {
                latitudeSeconds = value;
                RaisePropertyChanged(() => LatitudeSeconds);
            }
        }

        public string LongitudeDegrees
        {
            get { return longitudeDegrees; }
            set
            {
                longitudeDegrees = value;
                RaisePropertyChanged(() => LongitudeDegrees);
            }
        }

        public string LongitudeMinutes
        {
            get { return longitudeMinutes; }
            set
            {
                longitudeMinutes = value;
                RaisePropertyChanged(() => LongitudeMinutes);
            }
        }

        public string LongitudeSeconds
        {
            get { return longitudeSeconds; }
            set
            {
                longitudeSeconds = value;
                RaisePropertyChanged(() => LongitudeSeconds);
            }
        }

        public string LongitudeType
        {
            get { return longitudeType; }
            set
            {
                longitudeType = value;
                RaisePropertyChanged(() => LongitudeType);
            }
        }

        public string LatitudeType
        {
            get { return latitudeType; }
            set
            {
                latitudeType = value;
                RaisePropertyChanged(() => LatitudeType);
            }
        }

        public string PointLable
        {
            get { return pointLable; }
            set
            {
                pointLable = value;
                RaisePropertyChanged(() => PointLable);
            }
        }

        private Vertex ResultVertex
        {
            get { return resultVertex; }
            set
            {
                resultVertex = value;
            }
        }

        private void ConvertCoordinates()
        {
            Proj4Projection proj4 = new Proj4Projection();
            proj4.InternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
            proj4.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            double x = 0;
            double y = 0;

            switch (SelectedCoordinateFormat)
            {
                case CoordinateType.DegreesMinutes:
                case CoordinateType.DegreesMinutesSeconds:
                    if (String.IsNullOrEmpty(LatitudeSeconds))
                    {
                        LatitudeSeconds = "0";
                    }
                    if (String.IsNullOrEmpty(LongitudeSeconds))
                    {
                        LongitudeSeconds = "0";
                    }
                    double yDegrees, yMinutes, xDegrees, xMinutes, ySeconds, xSeconds;
                    if (double.TryParse(LatitudeDegrees, out yDegrees) && double.TryParse(LatitudeMinutes, out yMinutes) &&
                        double.TryParse(LatitudeSeconds, out ySeconds) && double.TryParse(LongitudeDegrees, out xDegrees) &&
                            double.TryParse(LongitudeMinutes, out xMinutes) && double.TryParse(LongitudeSeconds, out xSeconds)
                        )
                    {
                        double actualXMinutes = xMinutes + (xDegrees - (int)xDegrees) * 60;
                        double actualXSeconds = xSeconds + (actualXMinutes - (int)actualXMinutes) * 60;
                        double actualYMinutes = yMinutes + (yDegrees - (int)yDegrees) * 60;
                        double actualYSeconds = ySeconds + (actualYMinutes - (int)actualYMinutes) * 60;
                        x = DecimalDegreesHelper.GetDecimalDegreeFromDegreesMinutesSeconds((int)xDegrees, (int)actualXMinutes, actualXSeconds);
                        y = DecimalDegreesHelper.GetDecimalDegreeFromDegreesMinutesSeconds((int)yDegrees, (int)actualYMinutes, actualYSeconds);
                        x = LongitudeType == "E" ? x : -x;
                        y = LatitudeType == "N" ? y : -y;
                    }
                    else
                    {
                        throw new Exception(errorMessage);
                    }
                    break;
                case CoordinateType.XY:
                case CoordinateType.DecimalDegrees:
                default:
                    if (!(double.TryParse(Longitude, out x) && double.TryParse(Latitude, out y)))
                    {
                        throw new Exception(errorMessage);
                    }
                    break;
            }

            proj4.Open();
            ResultVertex = SelectedCoordinateFormat == CoordinateType.XY ? new Vertex(x, y) : proj4.ConvertToExternalProjection(x, y);
            proj4.Close();
        }

        public void PlotPoint()
        {
            ConvertCoordinates();

            PointShape plottedPoint = new PointShape(ResultVertex);
            PlotPoint(plottedPoint);
            GisEditor.ActiveMap.CenterAt(plottedPoint);
        }

        public static void PlotPoint(Vertex point)
        {
            PlotPoint(new PointShape(point));
        }

        public static void PlotPoint(PointShape plottedPoint)
        {
            Marker plottedMarker = new Marker(plottedPoint);
            plottedMarker.Cursor = System.Windows.Input.Cursors.Hand;
            plottedMarker.ImageSource = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/pin_icon_red.png", UriKind.Relative), new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable));
            plottedMarker.YOffset = -8.5;
            plottedMarker.ToolTip = GetWorkingContent(plottedPoint);
            plottedMarker.PositionChanged += MarkerPositionChanged;
            plottedMarker.MarkerMouseClick += MarkerMouseClick;
            plottedMarker.PreviewMouseRightButtonDown += PlottedMarker_PreviewMouseRightButtonDown;
            plottedMarker.ContextMenu = GetContextMenuForPlottedMarker(plottedMarker);

            SimpleMarkerOverlay simpleMarkerOverlay = CurrentOverlays.PlottedMarkerOverlay;
            simpleMarkerOverlay.Markers.Add(plottedMarker);
            GisEditor.ActiveMap.Refresh(simpleMarkerOverlay);
        }

        private static void PlottedMarker_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Marker currentMarker = (Marker)sender;
            if (currentMarker.ContextMenu != null) currentMarker.ContextMenu.IsOpen = true;
            e.Handled = true;
        }

        private static ContextMenu GetContextMenuForPlottedMarker(Marker marker)
        {
            ContextMenu ctx = new ContextMenu();

            MenuItem menuItem = new MenuItem();
            menuItem.Header = "Remove";
            menuItem.DataContext = marker;
            menuItem.Icon = new Image()
            {
                Width = 16,
                Height = 16,
                Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/Delete.png", UriKind.RelativeOrAbsolute))
            };

            menuItem.Click += (s, e) =>
            {
                MenuItem self = (MenuItem)s;
                Marker relatedMarker = self.DataContext as Marker;

                if (relatedMarker != null)
                {
                    Popup relatedPopup = relatedMarker.Tag as Popup;

                    SimpleMarkerOverlay currentMarkerOverlay = CurrentOverlays.PlottedMarkerOverlay;
                    if (currentMarkerOverlay.Markers.Contains(relatedMarker))
                    {
                        currentMarkerOverlay.Markers.Remove(relatedMarker);
                        GisEditor.ActiveMap.Refresh(currentMarkerOverlay);
                    }

                    PopupOverlay currentPopupOverlay = CurrentOverlays.PopupOverlay;
                    if (currentPopupOverlay.Popups.Contains(relatedPopup))
                    {
                        currentPopupOverlay.Popups.Remove(relatedPopup);
                        GisEditor.ActiveMap.Refresh(currentPopupOverlay);
                    }
                }
            };

            ctx.Items.Add(menuItem);
            return ctx;
        }

        public static void MarkerPositionChanged(object sender, PositionChangedMarkerEventArgs e)
        {
            Marker currentMarker = (Marker)sender;
            currentMarker.ToolTip = GetWorkingContent(e.NewPosition);

            ClosablePopup attachedPopup = currentMarker.Tag as ClosablePopup;
            if (attachedPopup != null)
            {
                attachedPopup.Position = new Point(e.NewPosition.X, e.NewPosition.Y);
                attachedPopup.WorkingContent = GetWorkingContent(e.NewPosition);
            }
        }

        public static void MarkerMouseClick(object sender, MouseButtonEventArgs e)
        {
            Marker currentMarker = (Marker)sender;
            if (e.ChangedButton == MouseButton.Left)
            {
                ClosablePopup attachedPopup = currentMarker.Tag as ClosablePopup;
                PopupOverlay popupOverlay = CurrentOverlays.PopupOverlay;
                if (attachedPopup == null || !popupOverlay.Popups.Contains(attachedPopup))
                {
                    PointShape position = new PointShape(currentMarker.Position.X, currentMarker.Position.Y);
                    attachedPopup = new ClosablePopup(position);
                    attachedPopup.ParentMap = GisEditor.ActiveMap;
                    attachedPopup.WorkingContent = GetWorkingContent(position);
                    SetPopupOffset(attachedPopup);
                    popupOverlay.Popups.Add(attachedPopup);

                    GisEditor.ActiveMap.Refresh(popupOverlay);
                    currentMarker.Tag = attachedPopup;
                }

                if (attachedPopup != null && attachedPopup.Visibility == Visibility.Collapsed)
                {
                    attachedPopup.Visibility = Visibility.Visible;
                }
            }
        }

        private static void SetPopupOffset(ClosablePopup attachedPopup)
        {
            TranslateTransform translate = new TranslateTransform();
            translate.Y = -25;
            attachedPopup.RenderTransform = translate;
        }

        public static string GetWorkingContent(PointShape position, string title = null)
        {
            string lonString = "--";
            string latString = "--";

            if (!Double.IsNaN(position.X)) lonString = position.X.ToString("N4");
            if (!Double.IsNaN(position.Y)) latString = position.Y.ToString("N4");

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(String.IsNullOrEmpty(title) ? "Point Plotted" : title);
            stringBuilder.Append("Longitude (X): ");
            stringBuilder.AppendLine(lonString);
            stringBuilder.Append("Latitude (Y): ");
            stringBuilder.AppendLine(latString);
            return stringBuilder.ToString();
        }

        private string GetWorkingContent()
        {
            double lon, lat, tempLon, tempLat;
            lon = lat = double.NaN;

            if (double.TryParse(Longitude, out tempLon)) lon = tempLon;
            if (double.TryParse(Latitude, out tempLat)) lat = tempLat;
            return GetWorkingContent(new PointShape(lon, lat), PointLable);
        }
    }
}