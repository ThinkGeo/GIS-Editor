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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    internal static class MarkerHelper
    {
        private static string markerOverlayName = "AnnotationMarkerOverlay";
        private static bool shouldAddTextBoxWhenNextClick = false;
        private static bool isPositionChangeStarted = false;

        internal static SimpleMarkerOverlay CurrentMarkerOverlay
        {
            get
            {
                if (GisEditor.ActiveMap != null)
                {
                    var markerOverlay = GisEditor.ActiveMap.Overlays.OfType<SimpleMarkerOverlay>()
                                          .Where(mOverlay => mOverlay.Name == markerOverlayName)
                                          .FirstOrDefault();

                    return markerOverlay;
                }
                return null;
            }
        }

        internal static AnnotationViewModel ViewModel { get; set; }

        internal static void AddMarkerOverlayIfNotExisting(WpfMap map)
        {
            var markerOverlay = map.Overlays.OfType<SimpleMarkerOverlay>()
                .Where(mOverlay => mOverlay.Name.Equals(markerOverlayName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (markerOverlay == null)
            {
                markerOverlay = new AnnotationSimpleMarkerOverlay
                {
                    Name = markerOverlayName,
                    DragMode = MarkerDragMode.Drag,

                    AddMarker = new Action<MarkerState>(tmpObj =>
                    {
                        AddMarker(tmpObj.Position, tmpObj.ContentText, tmpObj.Id, tmpObj.StyleValue);
                    })
                };

                map.Overlays.Add(markerOverlay);
                map.Refresh();
            }
        }

        internal static void ActiveMap_MapClick(object sender, MapMouseClickInteractiveOverlayEventArgs e)
        {
            if (CurrentMarkerOverlay != null)
            {
                bool needTakeSnapshot = true;

                if (Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    needTakeSnapshot = CommitTextAnnotations();
                    ViewModel.SyncStylePreview();
                }

                if (needTakeSnapshot)
                {
                    ViewModel.TakeSnapshot();
                }
            }

            if (ViewModel.SelectedMode != null && ViewModel.SelectedMode.Mode == TrackMode.Custom && shouldAddTextBoxWhenNextClick)
            {
                AddMarker(new PointShape(e.InteractionArguments.WorldX, e.InteractionArguments.WorldY));
            }
        }

        internal static Action<MarkerState> AddMarkerAction
        {
            get
            {
                return new Action<MarkerState>(tmpObj =>
                    {
                        AddMarker(tmpObj.Position, tmpObj.ContentText, tmpObj.Id, tmpObj.StyleValue);
                    });
            }
        }

        internal static void AddMarker(PointShape point, string initText = "", object tag = null, string styleValue = "")
        {
            string markerId = (tag ?? ViewModel.CurrentAnnotationOverlay.NewFeatureName()) as string;
            AddMarkerOverlayIfNotExisting(GisEditor.ActiveMap);
            SimpleMarkerOverlay markerOverlay = CurrentMarkerOverlay;
            var textBox = new TextBox
            {
                MinWidth = 100,
                AcceptsReturn = true,
                AcceptsTab = true,
                Text = initText,
                Tag = styleValue
            };

            textBox.FocusableChanged += new DependencyPropertyChangedEventHandler((ds, de) =>
            {
                if ((bool)de.NewValue)
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Black);
                }
            });

            TextStyle textStyle = null;
            if (String.IsNullOrEmpty(styleValue))
            {
                textStyle = ViewModel.CurrentAnnotationOverlay.GetLatestStyle<TextStyle>(AnnotaionStyleType.LayerStyle);
            }
            else
            {
                textStyle = ViewModel.CurrentAnnotationOverlay.GetSpecificTextStyle(styleValue);
            }

            if (textStyle != null)
            {
                ApplyTextStyleToTextBox(textStyle, textBox);
            }

            textBox.Loaded += (s, arg) =>
            {
                var box = s as TextBox;
                box.Focus();
                if (!String.IsNullOrEmpty(box.Text))
                    box.Cursor = System.Windows.Input.Cursors.SizeAll;
            };

            textBox.KeyDown += textBox_KeyDown;
            Marker marker = new AnnotationMarker(point)
            {
                Content = textBox,
                ImageSource = null,
                Tag = markerId,
                VerticalContentAlignment = VerticalAlignment.Top,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            marker.PositionChanged += marker_PositionChanged;

            if (!String.IsNullOrEmpty(initText))
            {
                var measuredSize = new PlatformGeoCanvas().MeasureText(initText, textStyle.Font);
                marker.Width = measuredSize.Width;
                marker.Height = measuredSize.Height;
                textBox.Focusable = false;
                textBox.IsReadOnly = true;

                var textBoxLoseFocus = new Action<object, RoutedEventArgs>((ds, de) =>
                {
                    textBox.Focusable = false;
                    textBox.IsReadOnly = true;

                    if (String.IsNullOrEmpty(textBox.Text))
                    {
                        textBox.Cursor = System.Windows.Input.Cursors.IBeam;
                    }
                    else
                    {
                        textBox.Cursor = System.Windows.Input.Cursors.SizeAll;
                    }
                });

                Point clickPoint = new Point(double.PositiveInfinity, double.PositiveInfinity);
                marker.MouseLeftButtonDown += new MouseButtonEventHandler((ds, de) =>
                {
                    clickPoint = de.GetPosition(GisEditor.ActiveMap);
                    ((Marker)ds).Cursor = System.Windows.Input.Cursors.SizeAll;
                });

                marker.MouseLeftButtonUp += new MouseButtonEventHandler((ds, de) =>
                {
                    ((Marker)ds).Cursor = System.Windows.Input.Cursors.Arrow;
                });

                marker.MouseLeftButtonUp += new MouseButtonEventHandler((ds, de) =>
                {
                    Point tmpPoint = de.GetPosition(GisEditor.ActiveMap);
                    if (Math.Abs(tmpPoint.X - clickPoint.X) < 1 && Math.Abs(tmpPoint.Y - clickPoint.Y) < 1)
                    {
                        textBox.Focusable = true;
                        textBox.IsReadOnly = false;
                        textBox.LostFocus -= new RoutedEventHandler(textBoxLoseFocus);
                        textBox.LostFocus += new RoutedEventHandler(textBoxLoseFocus);
                        textBox.Focus();
                        textBox.Cursor = System.Windows.Input.Cursors.IBeam;
                    }

                    clickPoint.X = double.PositiveInfinity;
                    clickPoint.Y = double.PositiveInfinity;
                });
            }

            marker.AdjustPosition(textStyle.PointPlacement);
            markerOverlay.Markers.Add(markerId, marker);
            markerOverlay.Refresh();
        }

        private static void marker_PositionChanged(object sender, PositionChangedMarkerEventArgs e)
        {
            //if (!isPositionChangeStarted && Keyboard.Modifiers == ModifierKeys.Shift)
            if (!isPositionChangeStarted)
            {
                isPositionChangeStarted = true;
                double offsetX = e.NewPosition.X - e.PreviousPosition.X;
                double offsetY = e.NewPosition.Y - e.PreviousPosition.Y;
                foreach (var marker in CurrentMarkerOverlay.Markers.Where(tmpMarker => tmpMarker != sender))
                {
                    marker.Position = new Point(marker.Position.X + offsetX, marker.Position.Y + offsetY);
                }

                isPositionChangeStarted = false;
            }
        }

        internal static void ApplyTextStyleToTextBox(TextStyle textStyle, TextBox textBox)
        {
            TextStyle tempTextStyle = textStyle;
            textBox.FontSize = tempTextStyle.Font.Size;
            //Use black color to stand the text out no matter what the text style's color is.
            textBox.Foreground = new SolidColorBrush(Colors.Black);
            textBox.FontFamily = new FontFamily(tempTextStyle.Font.FontName);
            textBox.FontWeight = tempTextStyle.Font.IsBold ? FontWeights.Bold : FontWeights.Normal;
            textBox.FontStyle = tempTextStyle.Font.IsItalic ? FontStyles.Italic : FontStyles.Normal;
            if (tempTextStyle.Font.IsUnderline)
            {
                textBox.TextDecorations.Add(TextDecorations.Underline);
            }
            if (tempTextStyle.Font.IsStrikeout)
            {
                textBox.TextDecorations.Add(TextDecorations.Strikethrough);
            }
        }

        [System.Reflection.Obfuscation]
        private static void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CancelTextAnnotation();
            }
        }

        private static void CancelTextAnnotation()
        {
            CurrentMarkerOverlay.Markers.Clear();
            CurrentMarkerOverlay.Refresh();
        }

        internal static bool CommitTextAnnotations()
        {
            bool isChanged = false;
            //if (CurrentMarkerOverlay != null && !ViewModel.IsInSelectMoveMode)
            if (CurrentMarkerOverlay != null)
            {
                var markerResults = CurrentMarkerOverlay.Markers
                        .Where(marker => !string.IsNullOrEmpty((marker.Content as TextBox).Text))
                        .Select(marker =>
                        {
                            TextBox textBox = (TextBox)marker.Content;
                            string text = textBox.Text;
                            string styleValue = textBox.Tag.ToString();
                            return new
                            {
                                Text = text,
                                WorldX = marker.Position.X,
                                WorldY = marker.Position.Y,
                                Id = (string)(marker.Tag ?? ViewModel.CurrentAnnotationOverlay.NewFeatureName()),
                                StyleValue = styleValue
                            };
                        }).ToArray();

                shouldAddTextBoxWhenNextClick = markerResults.Length == 0;
                foreach (var markerResult in markerResults)
                {
                    var currentTrackShapeLayer = ViewModel.CurrentAnnotationOverlay.TrackShapeLayer;
                    var correspondFeature = currentTrackShapeLayer.InternalFeatures.FirstOrDefault(f => f.Id.Equals(markerResult.Id));
                    int index = -1;
                    if (correspondFeature != null)
                    {
                        index = currentTrackShapeLayer.InternalFeatures.IndexOf(correspondFeature);
                        currentTrackShapeLayer.InternalFeatures.Remove(correspondFeature);
                    }

                    var newFeature = new Feature(new Vertex(markerResult.WorldX, markerResult.WorldY), markerResult.Id);

                    if (index > -1)
                    {
                        currentTrackShapeLayer.InternalFeatures.Insert(index, newFeature);
                    }
                    else
                    {
                        currentTrackShapeLayer.InternalFeatures.Add(markerResult.Id, newFeature);
                    }

                    if (currentTrackShapeLayer.FeatureIdsToExclude.Contains(markerResult.Id))
                    {
                        currentTrackShapeLayer.FeatureIdsToExclude.Remove(markerResult.Id);
                    }

                    ViewModel.CurrentAnnotationOverlay.StyleLastTextAnnotation(newFeature, markerResult.Text, markerResult.StyleValue);
                    isChanged = true;
                }

                CurrentMarkerOverlay.Markers.Clear();

                CurrentMarkerOverlay.Refresh();
                ViewModel.CurrentAnnotationOverlay.Refresh();
                ViewModel.SyncUIState();
                GisEditor.UIManager.RefreshPlugins(new RefreshArgs(CurrentMarkerOverlay, RefreshArgsDescription.CommitTextAnnotationsDescription));
            }

            return isChanged;
        }

        public static void AdjustPosition(this Marker marker, PointPlacement placement)
        {
            marker.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Size markerSize = marker.DesiredSize;
            Size contentSize = new Size();
            FrameworkElement contentElement = marker.Content as FrameworkElement;
            if (contentElement != null)
            {
                contentSize = contentElement.DesiredSize;
            }

            double contentOX = 0;
            double contentOY = 0;
            double markerOX = 0;
            double markerOY = 0;

            switch (placement)
            {
                case PointPlacement.UpperLeft:
                    markerOX = -markerSize.Width * .5;
                    markerOY = -markerSize.Height * .5;
                    break;
                case PointPlacement.UpperCenter:
                    markerOY = -markerSize.Height * .5;
                    break;
                case PointPlacement.UpperRight:
                    markerOX = markerSize.Width * .5;
                    markerOY = -markerSize.Height * .5;
                    break;
                case PointPlacement.CenterRight:
                    markerOX = markerSize.Width * .5;
                    break;
                case PointPlacement.CenterLeft:
                    markerOX = -markerSize.Width * .5;
                    break;
                case PointPlacement.LowerLeft:
                    markerOX = -markerSize.Width * .5;
                    markerOY = markerSize.Height * .5;
                    break;
                case PointPlacement.LowerCenter:
                    markerOY = markerSize.Height * .5;
                    break;
                case PointPlacement.LowerRight:
                    markerOX = markerSize.Width * .5;
                    markerOY = markerSize.Height * .5;
                    break;
                case PointPlacement.Center:
                default:
                    break;
            }

            marker.XOffset = markerOX;
            marker.YOffset = markerOY;

            if (contentElement != null)
            {
                contentOX = -markerSize.Width * .5 + contentSize.Width * .5 - 4;
                contentOY = -markerSize.Height * .5 + contentSize.Height * .5 - 2;
                contentElement.Margin = new Thickness(contentOX, contentOY, -contentOX, -contentOY);
            }
        }
    }
}