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


using Microsoft.Windows.Controls.Ribbon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Xml.Serialization;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class EditorUIPlugin : UIPlugin
    {
        internal static readonly bool IsRelateAndAliasEnabled = true;

        private const string grabCursorPath = "/GisEditorPluginCore;component/Images/cursor_drag_hand.cur";
        private bool clickCopy;
        private Feature selectedFeature;
        private PointShape cursorWorldPoint;
        private RibbonEntry helpEntry;
        private RibbonEntry editingToolsEntry;
        private RibbonEntry snappingToolsEntry;
        private RibbonEntry shapeOperationsEntry;
        public Dictionary<string, Collection<CalculatedDbfColumn>> CalculatedColumns = new Dictionary<string, Collection<CalculatedDbfColumn>>();

        [NonSerialized]
        private EditingToolsRibbonGroup editingToolsRibbonGroup;

        [NonSerialized]
        private SnappingToolsRibbonGroup snappingToolsRibbonGroup;

        [NonSerialized]
        private ShapeOperationsRibbonGroup shapeOperationsRibbonGroup;

        [NonSerialized]
        private RibbonGroup helpRibbonGroup;

        [NonSerialized]
        private EditorOptionUserControl optionUI;

        [NonSerialized]
        private MenuItem removeVertexMenuItem;

        [NonSerialized]
        private MenuItem addVertexMenuItem;

        [NonSerialized]
        private MenuItem copyFeatureMenuItem;

        [NonSerialized]
        private MenuItem pasteFeatureMenuItem;

        public EditorUIPlugin()
        {
            Index = UIPluginOrder.EditorPlugin;
            SmallIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/EditorPlugin.png", UriKind.RelativeOrAbsolute));
            LargeIcon = new BitmapImage(new Uri("/GisEditorPluginCore;component/Images/EditorPlugin.png", UriKind.RelativeOrAbsolute));
            Description = GisEditor.LanguageManager.GetStringResource("EditorUIPluginDescription");

            editingToolsRibbonGroup = new EditingToolsRibbonGroup();
            snappingToolsRibbonGroup = new SnappingToolsRibbonGroup();
            shapeOperationsRibbonGroup = new ShapeOperationsRibbonGroup();

            helpRibbonGroup = new RibbonGroup();
            helpRibbonGroup.Items.Add(HelpResourceHelper.GetHelpButton("EditorPluginHelp", HelpButtonMode.RibbonButton));
            helpRibbonGroup.GroupSizeDefinitions.Add(new RibbonGroupSizeDefinition() { IsCollapsed = false });
            helpRibbonGroup.SetResourceReference(RibbonGroup.HeaderProperty, "HelpHeader");

            editingToolsEntry = new RibbonEntry();
            editingToolsEntry.RibbonGroup = editingToolsRibbonGroup;
            editingToolsEntry.RibbonTabIndex = RibbonTabOrder.Edit;
            editingToolsEntry.RibbonTabName = "EditRibbonTabHeader";

            snappingToolsEntry = new RibbonEntry();
            snappingToolsEntry.RibbonGroup = snappingToolsRibbonGroup;
            snappingToolsEntry.RibbonTabIndex = RibbonTabOrder.Edit;
            snappingToolsEntry.RibbonTabName = "EditRibbonTabHeader";

            shapeOperationsEntry = new RibbonEntry();
            shapeOperationsEntry.RibbonGroup = shapeOperationsRibbonGroup;
            shapeOperationsEntry.RibbonTabName = "EditRibbonTabHeader";
            shapeOperationsEntry.RibbonTabIndex = RibbonTabOrder.Edit;

            helpEntry = new RibbonEntry();
            helpEntry.RibbonGroup = helpRibbonGroup;
            helpEntry.RibbonTabName = "EditRibbonTabHeader";
            helpEntry.RibbonTabIndex = RibbonTabOrder.Edit;
        }

        protected override SettingUserControl GetSettingsUICore()
        {
            if (optionUI == null)
            {
                optionUI = new EditorOptionUserControl();
                optionUI.DataContext = new EditorOptionViewModel(Singleton<EditorSetting>.Instance);
            }
            return optionUI;
        }

        protected override void LoadCore()
        {
            base.LoadCore();

            if (!RibbonEntries.Contains(editingToolsEntry))
            {
                RibbonEntries.Add(editingToolsEntry);
            }

            if (!RibbonEntries.Contains(snappingToolsEntry))
            {
                RibbonEntries.Add(snappingToolsEntry);
            }

            if (!RibbonEntries.Contains(shapeOperationsEntry))
            {
                RibbonEntries.Add(shapeOperationsEntry);
            }

            if (!RibbonEntries.Contains(helpEntry))
            {
                RibbonEntries.Add(helpEntry);
            }

            GisEditor.UIManager.GottenMapContextMenuItems -= new EventHandler<GottenMapContextMenuItemsUIPluginManagerEventArgs>(UIManager_GottenMapContextMenuItems);
            GisEditor.UIManager.GottenMapContextMenuItems += new EventHandler<GottenMapContextMenuItemsUIPluginManagerEventArgs>(UIManager_GottenMapContextMenuItems);
        }

        protected override void UnloadCore()
        {
            base.UnloadCore();
            RibbonEntries.Clear();
            GisEditor.UIManager.GottenMapContextMenuItems -= new EventHandler<GottenMapContextMenuItemsUIPluginManagerEventArgs>(UIManager_GottenMapContextMenuItems);
        }

        protected override void RefreshCore(GisEditorWpfMap currentMap, RefreshArgs refreshArgs)
        {
            if (!(refreshArgs != null && refreshArgs.Description.Equals(RefreshArgsDescription.EditLayerChangedDescription)))
            {
                base.RefreshCore(currentMap, refreshArgs);
                EditingToolsViewModel.Instance.Refresh(currentMap);
                snappingToolsRibbonGroup.ViewModel.EditOverlay = EditingToolsViewModel.Instance.EditOverlay;
                snappingToolsRibbonGroup.ViewModel.Refresh(currentMap);
            }
        }

        protected override Collection<MenuItem> GetLayerListItemContextMenuItemsCore(GetLayerListItemContextMenuParameters parameters)
        {
            Collection<MenuItem> menuItems = base.GetLayerListItemContextMenuItemsCore(parameters);
            if (parameters.LayerListItem == null)
            {
                return menuItems;
            }
            FeatureLayer featureLayer = parameters.LayerListItem.ConcreteObject as FeatureLayer;
            if (featureLayer != null && featureLayer.FeatureSource.IsEditable)
            {
                bool isInEditing = featureLayer == GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer;
                MenuItem editLayerMenuItem = new MenuItem() { Header = GisEditor.LanguageManager.GetStringResource("LayerListUIPluginEditLayerText"), Name = "EditLayer" };
                editLayerMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/sketchTools.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };

                MenuItem startMenuItem = new MenuItem() { Header = GisEditor.LanguageManager.GetStringResource("LayerListMenuItemHelperStartEditingText"), IsEnabled = !isInEditing };
                startMenuItem.Click += StartEditMenuItem_Click;
                MenuItem cancelMenuItem = new MenuItem() { Header = GisEditor.LanguageManager.GetStringResource("LayerListMenuItemHelperCancelEditingText") };
                cancelMenuItem.Command = EditingToolsViewModel.Instance.CancelCommand;
                MenuItem saveMenuItem = new MenuItem() { Header = GisEditor.LanguageManager.GetStringResource("LayerListMenuItemHelperSaveEditingText") };
                saveMenuItem.Command = EditingToolsViewModel.Instance.SaveEditingCommand;
                MenuItem endMenuItem = new MenuItem()
                {
                    Header = GisEditor.LanguageManager.GetStringResource("EndEditSelectionWindowTitle"),
                    IsEnabled = isInEditing || cancelMenuItem.Command.CanExecute(null) || saveMenuItem.Command.CanExecute(null)
                };
                endMenuItem.Click += EndMenuItem_Click;

                editLayerMenuItem.Items.Add(startMenuItem);
                editLayerMenuItem.Items.Add(cancelMenuItem);
                editLayerMenuItem.Items.Add(saveMenuItem);
                editLayerMenuItem.Items.Add(endMenuItem);

                menuItems.Add(editLayerMenuItem);
            }
            return menuItems;
        }

        private void StartEditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            FeatureLayer featurelayer = GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer;
            var selectionItem = EditingToolsViewModel.Instance.AvailableLayers.FirstOrDefault(t => t.Value == featurelayer);
            if (selectionItem != null) EditingToolsViewModel.Instance.EditingLayerChangedCommand.Execute(selectionItem);
        }

        private void EndMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (GisEditor.ActiveMap.FeatureLayerEditOverlay.EditTargetLayer == GisEditor.LayerListManager.SelectedLayerListItem.ConcreteObject as FeatureLayer)
            {
                var nonSelectionItem = EditingToolsViewModel.Instance.AvailableLayers.FirstOrDefault(t => t.Value == null);
                if (nonSelectionItem != null) EditingToolsViewModel.Instance.EditingLayerChangedCommand.Execute(nonSelectionItem);
            }
        }

        protected override Collection<MenuItem> GetMapContextMenuItemsCore(GetMapContextMenuParameters parameters)
        {
            cursorWorldPoint = parameters.WorldCoordinates;
            Collection<MenuItem> menuItems = new Collection<MenuItem>();
            GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (editOverlay != null && !editOverlay.IsEmpty)
            {
                var intersectingFeatures = editOverlay.GetEditingFeaturesInterseting();
                if (intersectingFeatures.Count > 0)
                {
                    selectedFeature = intersectingFeatures.OrderBy(f => f.GetShape().GetDistanceTo(editOverlay.CurrentWorldCoordinate, GeographyUnit.Meter, DistanceUnit.Meter)).FirstOrDefault();

                    foreach (var item in GetEditMenuItems(selectedFeature, editOverlay.CanReshape, intersectingFeatures.Count))
                    {
                        menuItems.Add(item);
                    }
                }
                else if (clickCopy)
                {
                    InitializeCopyPasteMenuItems();
                    menuItems.Add(copyFeatureMenuItem);
                    menuItems.Add(pasteFeatureMenuItem);
                }
                else selectedFeature = null;
            }
            else selectedFeature = null;
            return menuItems;
        }

        protected override StorableSettings GetSettingsCore()
        {
            var settings = base.GetSettingsCore();
            foreach (var item in Singleton<EditorSetting>.Instance.SaveState())
            {
                settings.GlobalSettings[item.Key] = item.Value;
            }

            try
            {
                if (CalculatedColumns.Count > 0)
                {
                    XElement root = new XElement("Root");
                    foreach (var item in CalculatedColumns)
                    {
                        XElement featureSource = new XElement("FeatureSource");
                        featureSource.SetAttributeValue("ID", item.Key);
                        foreach (var column in item.Value)
                        {
                            XmlSerializer s = new XmlSerializer(column.GetType());
                            MemoryStream stream = new MemoryStream();
                            s.Serialize(stream, column);
                            stream.Seek(0, SeekOrigin.Begin);
                            stream.Position = 0;
                            XElement content = XElement.Load(stream);
                            featureSource.Add(content);
                        }
                        root.Add(featureSource);
                    }
                    settings.ProjectSettings["CalculatedColumns"] = root.ToString();
                }
            }
            catch (Exception ex)
            {
            }
            return settings;
        }

        protected override void ApplySettingsCore(StorableSettings settings)
        {
            base.ApplySettingsCore(settings);
            Singleton<EditorSetting>.Instance.LoadState(settings.GlobalSettings);
            if (settings.ProjectSettings.ContainsKey("CalculatedColumns"))
            {
                string value = settings.ProjectSettings["CalculatedColumns"];
                try
                {
                    XElement root = XElement.Parse(value);
                    var featureSources = root.Elements("FeatureSource");
                    foreach (var featureSource in featureSources)
                    {
                        var id = featureSource.Attribute("ID");
                        Collection<CalculatedDbfColumn> columns = new Collection<CalculatedDbfColumn>();
                        var columnsX = featureSource.Elements("CalculatedDbfColumn");
                        foreach (var item in columnsX)
                        {
                            XmlSerializer s = new XmlSerializer(typeof(CalculatedDbfColumn));
                            object result = s.Deserialize(item.CreateReader());
                            CalculatedDbfColumn temp = result as CalculatedDbfColumn;
                            if (temp != null)
                            {
                                columns.Add(temp);
                            }
                        }
                        if (columns.Count > 0)
                        {
                            CalculatedColumns[id.Value] = columns;
                        }
                    }
                }
                catch { }
            }
        }

        private bool ApplyHintSettings(StorableSettings settings, string key)
        {
            bool result = false;
            if (settings.GlobalSettings.ContainsKey(key))
            {
                bool.TryParse(settings.GlobalSettings[key], out result);
            }
            return result;
        }

        public static void UpdateCalculatedRecords(IEnumerable<CalculatedDbfColumn> updateColumns, IEnumerable<Feature> features)
        {
            foreach (var calculateColumn in updateColumns)
            {
                foreach (var feature in features)
                {
                    Feature featureInDecimalDegree = ConvertToWgs84Projection(feature);
                    var calculatedValue = string.Empty;

                    switch (calculateColumn.CalculationType)
                    {
                        case CalculatedDbfColumnType.Perimeter:
                            var perimeterShape = featureInDecimalDegree.GetShape() as AreaBaseShape;
                            if (perimeterShape != null)
                            {
                                calculatedValue = perimeterShape.GetPerimeter(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit).ToString();
                            }
                            break;

                        case CalculatedDbfColumnType.Area:
                            var areaShape = featureInDecimalDegree.GetShape() as AreaBaseShape;
                            if (areaShape != null)
                            {
                                calculatedValue = areaShape.GetArea(GeographyUnit.DecimalDegree, calculateColumn.AreaUnit).ToString();
                            }
                            break;

                        case CalculatedDbfColumnType.Length:
                            var lineShape = featureInDecimalDegree.GetShape() as LineBaseShape;
                            if (lineShape != null)
                            {
                                calculatedValue = lineShape.GetLength(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit).ToString();
                            }
                            break;

                        default:
                            break;
                    }

                    if (calculatedValue.Length > calculateColumn.Length)
                    {
                        calculatedValue = calculatedValue.Substring(0, calculateColumn.Length);
                    }

                    if (!feature.ColumnValues.ContainsKey(calculateColumn.ColumnName))
                    {
                        feature.ColumnValues.Add(calculateColumn.ColumnName, calculatedValue);
                    }
                }
            }
        }

        public static void UpdateCalculatedRecords(FeatureLayer featureLayer, IEnumerable<FeatureSourceColumn> updateColumns, bool needCalculate)
        {
            featureLayer.SafeProcess(() =>
            {
                try
                {
                    featureLayer.CloseInOverlay();
                    featureLayer.SetLayerAccess(LayerAccessMode.ReadWrite);
                    featureLayer.Open();

                    foreach (var column in updateColumns)
                    {
                        var calculateColumn = column as CalculatedDbfColumn;
                        if (calculateColumn == null)
                        {
                            EditorUIPlugin editorPlugin = GisEditor.UIManager.GetActiveUIPlugins<EditorUIPlugin>().FirstOrDefault();
                            if (editorPlugin != null)
                            {
                                string id = featureLayer.FeatureSource.Id;
                                if (editorPlugin.CalculatedColumns.ContainsKey(id))
                                {
                                    calculateColumn = editorPlugin.CalculatedColumns[id].FirstOrDefault(c => c.ColumnName == column.ColumnName);
                                }
                            }
                        }

                        if (calculateColumn != null)
                        {
                            EditorUIPlugin editorPlugin = GisEditor.UIManager.GetActiveUIPlugins<EditorUIPlugin>().FirstOrDefault();
                            if (editorPlugin != null)
                            {
                                string id = featureLayer.FeatureSource.Id;
                                if (editorPlugin.CalculatedColumns.ContainsKey(id) && !editorPlugin.CalculatedColumns[id].Any(c => c.ColumnName == calculateColumn.ColumnName))
                                {
                                    editorPlugin.CalculatedColumns[id].Add(calculateColumn);
                                }
                                else
                                {
                                    editorPlugin.CalculatedColumns[id] = new Collection<CalculatedDbfColumn>();
                                    editorPlugin.CalculatedColumns[id].Add(calculateColumn);
                                }
                            }

                            if (needCalculate)
                            {
                                var features = featureLayer.FeatureSource.GetAllFeatures(new Collection<string>() { calculateColumn.ColumnName });
                                foreach (var feature in features)
                                {
                                    Feature featureInDecimalDegree = ConvertToWgs84Projection(feature);
                                    var calculatedValue = string.Empty;
                                    double value = 0;

                                    switch (calculateColumn.CalculationType)
                                    {
                                        case CalculatedDbfColumnType.Perimeter:
                                            var perimeterShape = featureInDecimalDegree.GetShape() as AreaBaseShape;
                                            if (perimeterShape != null)
                                            {
                                                value = perimeterShape.GetPerimeter(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit);
                                            }
                                            break;

                                        case CalculatedDbfColumnType.Area:
                                            var areaShape = featureInDecimalDegree.GetShape() as AreaBaseShape;
                                            if (areaShape != null)
                                            {
                                                value = areaShape.GetArea(GeographyUnit.DecimalDegree, calculateColumn.AreaUnit);
                                            }
                                            break;

                                        case CalculatedDbfColumnType.Length:
                                            var lineShape = featureInDecimalDegree.GetShape() as LineBaseShape;
                                            if (lineShape != null)
                                            {
                                                value = lineShape.GetLength(GeographyUnit.DecimalDegree, calculateColumn.LengthUnit);
                                            }
                                            break;

                                        default:
                                            break;
                                    }
                                    calculatedValue = string.Format("{0:N" + calculateColumn.DecimalLength + "}", value);

                                    if (calculatedValue.Length > calculateColumn.Length)
                                    {
                                        calculatedValue = calculatedValue.Substring(0, calculateColumn.Length);
                                    }
                                    featureLayer.EditTools.BeginTransaction();

                                    //calculatedValue = calculatedValue.Substring(0, column.MaxLength);

                                    feature.ColumnValues[calculateColumn.ColumnName] = calculatedValue;
                                    featureLayer.EditTools.Update(feature);
                                    featureLayer.EditTools.CommitTransaction();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                }
                finally
                {
                    featureLayer.Close();
                    featureLayer.SetLayerAccess(LayerAccessMode.Read);
                }
            });
        }

        private static Feature ConvertToWgs84Projection(Feature feature)
        {
            Feature featureInDecimalDegree = feature;
            Proj4Projection projection = new Proj4Projection();
            projection.InternalProjectionParametersString = Proj4Projection.GetEpsgParametersString(4326);
            projection.ExternalProjectionParametersString = GisEditor.ActiveMap.DisplayProjectionParameters;
            projection.SyncProjectionParametersString();
            if (projection.CanProject())
            {
                projection.Open();
                featureInDecimalDegree = projection.ConvertToInternalProjection(featureInDecimalDegree);
                projection.Close();
            }
            return featureInDecimalDegree;
        }

        private IEnumerable<MenuItem> GetEditMenuItems(Feature feature, bool canReshape, int editCount)
        {
            if (editCount == 1 && canReshape)
            {
                removeVertexMenuItem = new MenuItem();
                removeVertexMenuItem.Header = GisEditor.LanguageManager.GetStringResource("EditorPluginRomveVertexHeader");
                removeVertexMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/MapSuiteGisEditor;component/Images/document-delete.png", UriKind.RelativeOrAbsolute)) };
                removeVertexMenuItem.Click += new RoutedEventHandler(removeVertexMenuItem_Click);
                yield return removeVertexMenuItem;

                addVertexMenuItem = new MenuItem();
                addVertexMenuItem.Header = GisEditor.LanguageManager.GetStringResource("EditorPluginAddVertexHeader");
                addVertexMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/MapSuiteGisEditor;component/Images/document-new.png", UriKind.RelativeOrAbsolute)) };
                addVertexMenuItem.Click += new RoutedEventHandler(addVertexMenuItem_Click);
                yield return addVertexMenuItem;
            }

            if (feature.GetShape() is AreaBaseShape)
            {
                MenuItem areaResizeMenuItem = new MenuItem();
                areaResizeMenuItem.Header = GisEditor.LanguageManager.GetStringResource("AreaResizeWindowTitle");
                areaResizeMenuItem.Tag = feature;
                areaResizeMenuItem.Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/GisEditorPluginCore;component/Images/View.png", UriKind.RelativeOrAbsolute)), Width = 16, Height = 16 };
                areaResizeMenuItem.Click += new RoutedEventHandler(AreaResizeMenuItem_Click);
                yield return areaResizeMenuItem;
            }
        }

        private void InitializeCopyPasteMenuItems()
        {
            copyFeatureMenuItem = new MenuItem();
            copyFeatureMenuItem.Header = "Copy";
            copyFeatureMenuItem.IsEnabled = !clickCopy;
            copyFeatureMenuItem.Click += new RoutedEventHandler(CopyFeatureMenuItemClick);

            pasteFeatureMenuItem = new MenuItem();
            pasteFeatureMenuItem.IsEnabled = clickCopy;
            pasteFeatureMenuItem.Header = "Paste";
            pasteFeatureMenuItem.Click += new RoutedEventHandler(PasteFeatureMenuItemClick);
        }

        private void CopyFeatureMenuItemClick(object sender, RoutedEventArgs e)
        {
            clickCopy = true;
        }

        private void PasteFeatureMenuItemClick(object sender, RoutedEventArgs e)
        {
            clickCopy = false;
            var sourceControlPoint = selectedFeature.GetBoundingBox().GetCenterPoint();

            double offsetDistanceX = cursorWorldPoint.X - sourceControlPoint.X;
            double offsetDistanceY = cursorWorldPoint.Y - sourceControlPoint.Y;

            BaseShape baseShape = BaseShape.TranslateByOffset(selectedFeature.GetShape(), offsetDistanceX, offsetDistanceY, GeographyUnit.Meter, DistanceUnit.Meter);
            AddFeaturesToEditingLayer(new Feature[] { new Feature(baseShape, selectedFeature.ColumnValues) });
        }

        private static void AddFeaturesToEditingLayer(IEnumerable<Feature> features)
        {
            GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (editOverlay != null)
            {
                FeatureLayer editingLayer = editOverlay.EditTargetLayer;

                foreach (var feature in features)
                {
                    feature.Id = Guid.NewGuid().ToString();
                    editOverlay.NewFeatureIds.Add(feature.Id);
                    editOverlay.EditShapesLayer.InternalFeatures.Add(feature);
                }
                editOverlay.TakeSnapshot();
                editOverlay.Refresh();
            }
        }

        private void AreaResizeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            Feature feature = (Feature)item.Tag;
            AreaBaseShape areaShape = (AreaBaseShape)feature.GetShape();
            AreaResizeWindow window = new AreaResizeWindow(areaShape);
            window.Owner = Application.Current.MainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (window.ShowDialog().GetValueOrDefault())
            {
                double orignalAcreage = window.OriginalAcreage;
                double acreage = window.ResultAcreage;

                double difference = 1;

                while (difference > 0.05)
                {
                    if (acreage > orignalAcreage)
                    {
                        double percentage = (Math.Sqrt(acreage) / Math.Sqrt(orignalAcreage) * 100) % 100;
                        areaShape.ScaleUp(percentage);
                    }
                    else if (acreage == orignalAcreage)
                    {
                    }
                    else
                    {
                        double percentage = (Math.Sqrt(orignalAcreage) / Math.Sqrt(acreage) * 100) % 100;
                        areaShape.ScaleDown(percentage);
                    }
                    orignalAcreage = areaShape.GetArea(GisEditor.ActiveMap.MapUnit, AreaResizeWindow.DefaultAreaUnit);
                    difference = Math.Abs(orignalAcreage - acreage) / acreage;
                }

                Feature tempfeature = GisEditor.ActiveMap.FeatureLayerEditOverlay.EditShapesLayer.InternalFeatures.FirstOrDefault(f => f.Id.Equals(feature.Id));
                if (tempfeature != null)
                {
                    GisEditor.ActiveMap.FeatureLayerEditOverlay.EditShapesLayer.InternalFeatures.Remove(tempfeature);
                    GisEditor.ActiveMap.FeatureLayerEditOverlay.EditShapesLayer.InternalFeatures.Add(tempfeature.Id, new Feature(areaShape, tempfeature.ColumnValues));
                    GisEditor.ActiveMap.FeatureLayerEditOverlay.TakeSnapshot();
                    GisEditor.ActiveMap.FeatureLayerEditOverlay.Refresh();
                }
            }
        }

        private void addVertexMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (editOverlay != null && selectedFeature.GetShape() != null)
            {
                editOverlay.AddVertex(cursorWorldPoint);
                editOverlay.CalculateVertexControlPoints();
                GisEditor.ActiveMap.Refresh();
            }
        }

        private void removeVertexMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (editOverlay != null && selectedFeature.GetShape() != null)
            {
                editOverlay.RemoveVertex(cursorWorldPoint);
                editOverlay.CalculateVertexControlPoints();
                GisEditor.ActiveMap.Refresh();
            }
        }

        private void RemoveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (editOverlay != null && selectedFeature.GetShape() != null)
            {
                lock (editOverlay.EditShapesLayer)
                {
                    if (!editOverlay.EditShapesLayer.IsOpen) editOverlay.EditShapesLayer.Open();
                    editOverlay.EditShapesLayer.EditTools.BeginTransaction();
                    editOverlay.EditShapesLayer.EditTools.Delete(selectedFeature.Id);
                    editOverlay.EditShapesLayer.EditTools.CommitTransaction();
                }

                editOverlay.ClearVertexControlPoints();
                editOverlay.TakeSnapshot();
                editOverlay.Refresh();
            }
        }

        private void IdentifyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            GisEditorEditInteractiveOverlay editOverlay = GisEditor.ActiveMap.FeatureLayerEditOverlay;
            if (editOverlay != null && selectedFeature.GetShape() != null)
            {
                var identifyWindow = FeatureInfoWindow.Instance;
                identifyWindow.Show(DockWindowPosition.Right);
                Dictionary<FeatureLayer, Collection<Feature>> features = new Dictionary<FeatureLayer, Collection<Feature>>();
                features[editOverlay.EditTargetLayer] = new Collection<Feature>() { selectedFeature };
                identifyWindow.Refresh(features);
            }
        }

        private static void CancelLastestTracking(TrackInteractiveOverlay trackOverlay)
        {
            if (trackOverlay != null && trackOverlay.TrackMode != TrackMode.None && trackOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
            {
                trackOverlay.TrackShapeLayer.InternalFeatures.RemoveAt(trackOverlay.TrackShapeLayer.InternalFeatures.Count - 1);
                trackOverlay.Refresh();
                if (trackOverlay.TrackMode == TrackMode.Polygon ||
                    trackOverlay.TrackMode == TrackMode.Line)
                {
                    trackOverlay.MouseDoubleClick(new InteractionArguments());
                }
                else
                {
                    trackOverlay.MouseUp(new InteractionArguments());
                }
            }
        }

        private void UIManager_GottenMapContextMenuItems(object sender, GottenMapContextMenuItemsUIPluginManagerEventArgs e)
        {
            if (selectedFeature != null)
            {
                var startFlag = "Clear selected features";
                MenuItem clearItem = e.MenuItems.FirstOrDefault(m => m.Header.Equals(startFlag));
                if (clearItem != null)
                {
                    e.MenuItems.Remove(clearItem);
                }
                //var endFlag = "--";
                //var startRemoving = false;
                //for (int i = 0; i < e.MenuItems.Count; i++)
                //{
                //    if (e.MenuItems[i].Header.Equals(startFlag))
                //    {
                //        startRemoving = true;
                //    }

                //    if (startRemoving)
                //    {
                //        e.MenuItems.RemoveAt(i);
                //        if (e.MenuItems.Count > i && e.MenuItems[i].Header.Equals(endFlag))
                //        {
                //            e.MenuItems.RemoveAt(i);
                //            startRemoving = false;
                //            break;
                //        }

                //        i--;
                //    }
                //}
            }

            if (GisEditor.ActiveMap.TrackOverlay.TrackMode != TrackMode.None
                && GisEditor.ActiveMap.TrackOverlay.TrackShapeLayer.InternalFeatures.Count > 0)
            {
                MenuItem removeLastVertexMenuItem = new MenuItem();
                removeLastVertexMenuItem.Header = GisEditor.LanguageManager.GetStringResource("EditorUIPluginRemoveLastVertexLabel");
                removeLastVertexMenuItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(RemoveLastVertexMenuItem_Click);

                MenuItem cancelDrawingMenuItem = new MenuItem();
                cancelDrawingMenuItem.Header = GisEditor.LanguageManager.GetStringResource("EditorUIPluginCancelDrawingLabel");
                cancelDrawingMenuItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(CancelDrawingMenuItem_Click);

                MenuItem stopDrawingMenuItem = new MenuItem();
                stopDrawingMenuItem.Header = GisEditor.LanguageManager.GetStringResource("EditorUIPluginSaveDrawingLabel");
                stopDrawingMenuItem.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(StopDrawingMenuItem_Click);

                e.MenuItems.Clear();
                e.MenuItems.Add(new MenuItem { Header = "--" });
                e.MenuItems.Add(removeLastVertexMenuItem);
                e.MenuItems.Add(cancelDrawingMenuItem);
                e.MenuItems.Add(stopDrawingMenuItem);
            }
        }

        private void StopDrawingMenuItem_Click(object sender, MouseButtonEventArgs e)
        {
            InteractionArguments interactionArguments = new InteractionArguments();
            interactionArguments.WorldX = cursorWorldPoint.X;
            interactionArguments.WorldY = cursorWorldPoint.Y;
            GisEditor.ActiveMap.TrackOverlay.MouseDoubleClick(interactionArguments);
        }

        private void CancelDrawingMenuItem_Click(object sender, MouseButtonEventArgs e)
        {
            TrackMode tmpTrackMode = GisEditor.ActiveMap.TrackOverlay.TrackMode;
            GisEditor.ActiveMap.TrackOverlay.TrackMode = TrackMode.None;
            GisEditor.ActiveMap.TrackOverlay.TrackMode = tmpTrackMode;

            var circle = GisEditor.ActiveMap.TrackOverlay.OverlayCanvas.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
            if (circle != null)
                GisEditor.ActiveMap.TrackOverlay.OverlayCanvas.Children.Remove(circle);

            EditingToolsViewModel.Instance.CancelCommand.Execute(null);
        }

        private void RemoveLastVertexMenuItem_Click(object sender, MouseButtonEventArgs e)
        {
            GisEditorTrackInteractiveOverlay gisEditorTrackInteractiveOverlay = GisEditor.ActiveMap.TrackOverlay as GisEditorTrackInteractiveOverlay;
            if (gisEditorTrackInteractiveOverlay != null)
            {
                gisEditorTrackInteractiveOverlay.RemoveLastVertex();
            }
        }
    }
}