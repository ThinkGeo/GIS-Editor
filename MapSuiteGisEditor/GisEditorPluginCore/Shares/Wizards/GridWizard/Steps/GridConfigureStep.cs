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
using System.Linq;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GridConfigureStep : WizardStep<GridWizardShareObject>
    {
        private GridConfigureUserControl content;
        private int result;

        public GridConfigureStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepThree");
            Header = GisEditor.LanguageManager.GetStringResource("ToolsGridWizardStepThreeHeaderConfigure");
            Description = GisEditor.LanguageManager.GetStringResource("ToolsGridWizardStepThreeHeaderDescription");
            content = new GridConfigureUserControl();
            Content = content;
        }

        protected override void EnterCore(GridWizardShareObject parameter)
        {
            content.DataContext = parameter;
        }

        protected override bool LeaveCore(GridWizardShareObject parameter)
        {
            try
            {
                InitGridDefinition();
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GridConfigureStepsmallerMessage"), "Warning!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
                return false;
            }

            if (CheckIsOverflow())
            {
                System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GridConfigureStepkargerMessage"), "Warning!", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
                return false;
            }
            else
            {
                if (CheckCellSizeIsTooLarge())
                {
                    return true;
                }
                else
                {
                    if (System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("GridConfigureStepsmallcostMessage"),
                        "Warning!", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.Yes)
                        return true;
                    else
                        return false;
                }
            }
        }

        private bool CheckIsOverflow()
        {
            var entity = content.DataContext as GridWizardShareObject;
            try
            {
                int columnNumber = Convert.ToInt32(Math.Ceiling(Math.Round((entity.GridDefinition.GridExtent.LowerRightPoint.X - entity.GridDefinition.GridExtent.UpperLeftPoint.X) / entity.GridDefinition.CellSize, 8)));
                int rowNumber = Convert.ToInt32(Math.Ceiling(Math.Round((entity.GridDefinition.GridExtent.UpperLeftPoint.Y - entity.GridDefinition.GridExtent.LowerRightPoint.Y) / entity.GridDefinition.CellSize, 8)));

                result = checked(columnNumber * rowNumber);
                return false;
            }
            catch (Exception ex)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, ex.Message, new ExceptionInfo(ex));
                return true;
            }
        }

        private bool CheckCellSizeIsTooLarge()
        {
            if (result > 50000) return false;
            else return true;
        }

        private void InitGridDefinition()
        {
            var entity = content.DataContext as GridWizardShareObject;

            RectangleShape gridExtent = new RectangleShape();
            List<Feature> features = new List<Feature>();
            entity.SelectedFeatureLayer.SafeProcess(() =>
            {
                if (entity.HasSelectedFeatures && entity.OnlyUseSelectedFeatures)
                {
                    features = GisEditor.SelectionManager.GetSelectedFeatures().Where(f => f.Tag != null && f.Tag == entity.SelectedFeatureLayer).ToList();
                    gridExtent = ExtentHelper.GetBoundingBoxOfItems(features);
                    gridExtent.ScaleUp(0.05);
                }
                else
                {
                    features = entity.SelectedFeatureLayer.FeatureSource.GetAllFeatures(entity.SelectedFeatureLayer.FeatureSource.GetDistinctColumnNames()).ToList();
                    gridExtent = ExtentHelper.GetBoundingBoxOfItems(features);
                }
            });

            Dictionary<PointShape, double> dataPoints = new Dictionary<PointShape, double>();
            foreach (var item in features)
            {
                double columnValue = double.NaN;
                if (double.TryParse(item.ColumnValues[entity.SelectedDataColumn.ColumnName], out columnValue))
                {
                    var shape = item.GetShape();
                    if (shape.GetType().Equals(typeof(MultipointShape)))
                    {
                        foreach (var point in ((MultipointShape)shape).Points)
                        {
                            point.Tag = null;
                            dataPoints.Add(point, columnValue);
                        }
                    }
                    else
                    {
                        PointShape pointShape = (PointShape)shape;
                        pointShape.Tag = null;
                        dataPoints.Add(pointShape, columnValue);
                    }
                }
            }

            double tmpCellSize = 0.0;
            switch (GisEditor.ActiveMap.MapUnit)
            {
                case GeographyUnit.DecimalDegree:
                    tmpCellSize = DecimalDegreesHelper.GetLongitudeDifferenceFromDistance(entity.CellSize, entity.SelectedCellSizeDistanceUnit, gridExtent.GetCenterPoint().Y);
                    break;
                case GeographyUnit.Feet:
                    tmpCellSize = Conversion.ConvertMeasureUnits(entity.CellSize, entity.SelectedCellSizeDistanceUnit, DistanceUnit.Feet);
                    break;
                case GeographyUnit.Meter:
                    tmpCellSize = Conversion.ConvertMeasureUnits(entity.CellSize, entity.SelectedCellSizeDistanceUnit, DistanceUnit.Meter);
                    break;
                default:
                    break;
            }

            entity.GridDefinition = new GridDefinition(gridExtent, tmpCellSize, -9999, dataPoints);
        }
    }
}