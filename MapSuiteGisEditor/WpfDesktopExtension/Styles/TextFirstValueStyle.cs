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
using System.IO;
using System.Linq;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class TextFirstValueStyle : ValueStyle
    {
        public TextFirstValueStyle()
        {
            this.RequiredColumnNames.Add("LinkFileName");
        }

        protected override void DrawCore(IEnumerable<Feature> features, GeoCanvas canvas
            , Collection<SimpleCandidate> labelsInThisLayer
            , Collection<SimpleCandidate> labelsInAllLayers)
        {
            foreach (Feature feature in features)
            {
                string fieldValue = string.Empty;
                if (feature.ColumnValues.ContainsKey(ColumnName))
                {
                    fieldValue = feature.ColumnValues[ColumnName].Trim();
                }
                ValueItem valueItem = GetValueItem(fieldValue);

                Feature[] tmpFeatures = new Feature[1] { feature };
                if (valueItem.CustomStyles.Count == 0)
                {
                    if (valueItem.DefaultAreaStyle != null)
                    {
                        valueItem.DefaultAreaStyle.Draw(tmpFeatures, canvas, labelsInThisLayer, labelsInAllLayers);
                    }
                    if (valueItem.DefaultLineStyle != null)
                    {
                        valueItem.DefaultLineStyle.Draw(tmpFeatures, canvas, labelsInThisLayer, labelsInAllLayers);
                    }

                    if (valueItem.DefaultTextStyle != null
                        && tmpFeatures.Any(f
                            => f.ColumnValues.ContainsKey("NoteText") && !String.IsNullOrEmpty(f.ColumnValues["NoteText"])))
                    {
                        valueItem.DefaultTextStyle.Draw(tmpFeatures, canvas, labelsInThisLayer, labelsInAllLayers);
                    }
                    else if (valueItem.DefaultPointStyle != null)
                    {
                        if (feature.ColumnValues.ContainsKey("LinkFileName") && !string.IsNullOrEmpty(feature.ColumnValues["LinkFileName"]))
                        {
                            if (valueItem.DefaultTextStyle.Name == "FileLinkStyle" && valueItem.DefaultPointStyle.Name == "FileLinkStyle")
                            {
                                TextStyle textStyle = valueItem.DefaultTextStyle;
                                textStyle.PointPlacement = PointPlacement.LowerCenter;
                                if (valueItem.DefaultPointStyle.CustomPointStyles.Count > 0)
                                {
                                    textStyle.YOffsetInPixel = -(valueItem.DefaultPointStyle.CustomPointStyles.FirstOrDefault().SymbolSize / 2);
                                }
                                else
                                {
                                    textStyle.YOffsetInPixel = -(valueItem.DefaultPointStyle.SymbolSize / 2);
                                }

                                if (textStyle.CustomTextStyles.Count > 0)
                                {
                                    foreach (var item in textStyle.CustomTextStyles)
                                    {
                                        item.YOffsetInPixel = textStyle.YOffsetInPixel;
                                    }
                                }

                                Feature cloneFeature = feature.CloneDeep();
                                string path = cloneFeature.ColumnValues["LinkFileName"];
                                string columnValue = cloneFeature.ColumnValues["LinkFileName"];
                                if (columnValue.Contains("||"))
                                {
                                    int index = columnValue.IndexOf("||");
                                    path = columnValue.Substring(index + 2, columnValue.Length - index - 2);
                                }
                                string fileName = Path.GetFileName(path);
                                cloneFeature.ColumnValues["LinkFileName"] = fileName;
                                Feature[] fileLinkFeatures = new Feature[1] { cloneFeature };
                                textStyle.Draw(fileLinkFeatures, canvas, labelsInThisLayer, labelsInAllLayers);
                                valueItem.DefaultPointStyle.Draw(tmpFeatures, canvas, labelsInThisLayer, labelsInAllLayers);
                            }
                        }
                        else
                        {
                            valueItem.DefaultPointStyle.Draw(tmpFeatures, canvas, labelsInThisLayer, labelsInAllLayers);
                            Console.Write("");
                        }
                    }
                }
                else
                {
                    foreach (Style style in valueItem.CustomStyles)
                    {
                        style.Draw(tmpFeatures, canvas, labelsInThisLayer, labelsInAllLayers);
                    }
                }
                canvas.Flush();
            }
        }

        private ValueItem GetValueItem(string columnValue)
        {
            ValueItem result = null;

            string value = columnValue;
            if (columnValue.Contains("||"))
            {
                int index = columnValue.IndexOf("||");
                value = columnValue.Substring(0, index);
            }

            foreach (ValueItem valueItem in ValueItems)
            {
                if (string.Compare(value, valueItem.Value, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    result = valueItem;
                    break;
                }
            }

            if (result == null)
            {
                result = new ValueItem();
            }

            return result;
        }
    }
}