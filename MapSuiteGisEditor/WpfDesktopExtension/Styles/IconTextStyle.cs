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


using CSScriptLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using ThinkGeo.Core;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class IconTextStyle : IconStyle
    {
        internal static readonly string DefaultSeparator = "|||";

        [Obfuscation(Exclude = true)]
        private bool enableHalo;
        [Obfuscation(Exclude = true)]
        private bool enableMask;
        [Obfuscation(Exclude = true)]
        private bool enableScript;
        [Obfuscation(Exclude = true)]
        private string labelFunctionsScript;
        [Obfuscation(Exclude = true)]
        private Dictionary<string, string> labelFunctionColumnNames;

        public IconTextStyle()
            : base()
        {
            Initialize();
        }

        public IconTextStyle(GeoImage iconImage, string textColumnName, GeoFont textFont, GeoSolidBrush textSolidBrush)
            : base(iconImage, textColumnName, textFont, textSolidBrush)
        {
            Initialize();
        }

        public IconTextStyle(string iconPathFilename, string textColumnName, GeoFont textFont, GeoSolidBrush textSolidBrush)
            : base(iconPathFilename, textColumnName, textFont, textSolidBrush)
        {
            Initialize();
        }

        [Obsolete("This property is obsoleted, please use the property IsHaloEnabled instead. This property is obsolete and may be removed in or after version 9.0.")]
        public Boolean EnableHalo
        {
            get { return IsHaloEnabled; }
            set { IsHaloEnabled = value; }
        }

        [Obsolete("This property is obsoleted, please use the property IsMaskEnabled instead. This property is obsolete and may be removed in or after version 9.0.")]
        public Boolean EnableMask
        {
            get { return IsMaskEnabled; }
            set { IsMaskEnabled = value; }
        }

        public bool IsHaloEnabled
        {
            get { return enableHalo; }
            set { enableHalo = value; }
        }

        public bool IsMaskEnabled
        {
            get { return enableMask; }
            set { enableMask = value; }
        }

        public bool IsLabelFunctionEnabled
        {
            get { return enableScript; }
            set { enableScript = value; }
        }

        public string LabelFunctionsScript
        {
            get { return labelFunctionsScript; }
            set { labelFunctionsScript = value; }
        }

        public Dictionary<string, string> LabelFunctionColumnNames
        {
            get { return labelFunctionColumnNames; }
        }

        public bool BestPlacement
        {
            get { return TextPlacement == TextPlacement.AutoPlacement; }
            set
            {
                if (value)
                {
                    TextPlacement = TextPlacement.AutoPlacement;
                }
                else if (TextPlacement == TextPlacement.AutoPlacement)
                {
                    TextPlacement = TextPlacement.Center;
                }
            }
        }

        protected override Style CloneDeepCore()
        {
            var style = new IconTextStyle();

            style.Name = Name;
            style.IsActive = IsActive;
            style.DrawingLevel = DrawingLevel;

            style.TextColumnName = TextColumnName;
            style.TextContent = TextContent;
            style.TextFormat = TextFormat;
            style.TextPlacement = TextPlacement;
            style.Alignment = Alignment;
            style.Font = CloneGeoFont(Font);
            style.TextBrush = CloneGeoBrush(TextBrush);
            style.HaloPen = HaloPen == null ? null : HaloPen.CloneDeep();
            style.Mask = Mask == null ? null : (AreaStyle)Mask.CloneDeep();
            style.MaskMargin = MaskMargin;
            style.MaskType = MaskType;
            style.IsHaloEnabled = IsHaloEnabled;
            style.IsMaskEnabled = IsMaskEnabled;

            //style.BasePoint = BasePoint == null ? null : (PointStyle)BasePoint.CloneDeep();
            style.IconImage = CloneGeoImage(IconImage);
            if (IconPathFilename != null)
                style.IconPathFilename = IconPathFilename;
            style.IconImageScale = IconImageScale;

            style.RotationAngle = RotationAngle;
            style.XOffsetInPixel = XOffsetInPixel;
            style.YOffsetInPixel = YOffsetInPixel;
            //style.Spacing = Spacing;
            style.WrapWidth = WrapWidth;
            style.TextLineSegmentRatio = TextLineSegmentRatio;

            style.AllowLabelNudging = AllowLabelNudging;
            style.NudgingIntervalInPixel = NudgingIntervalInPixel;
            style.MaxNudgingInPixel = MaxNudgingInPixel;
            style.MinDistance = MinDistance;
            style.OverlappingRule = OverlappingRule;
            style.DuplicateRule = DuplicateRule;
            style.GridSize = GridSize;

            style.AllowLineCarriage = AllowLineCarriage;
            style.ForceLineCarriage = ForceLineCarriage;
            style.ForceHorizontalLabelForLine = ForceHorizontalLabelForLine;
            style.LabelAllLineParts = LabelAllLineParts;
            style.LabelAllPolygonParts = LabelAllPolygonParts;
            style.PolygonLabelingLocationMode = PolygonLabelingLocationMode;
            style.FittingPolygon = FittingPolygon;
            style.FittingPolygonInScreen = FittingPolygonInScreen;
            style.FittingPolygonFactor = FittingPolygonFactor;
            style.FittingLineInScreen = FittingLineInScreen;

            style.LeaderLineRule = LeaderLineRule;
            style.LeaderLineStyle = LeaderLineStyle == null ? null : (LineStyle)LeaderLineStyle.CloneDeep();
            style.LeaderLineMinimumLengthInPixels = LeaderLineMinimumLengthInPixels;

            style.NumericFormat = NumericFormat;
            style.DateFormat = DateFormat;
            style.LetterCase = LetterCase;
            style.SplineType = SplineType;

            style.IsLabelFunctionEnabled = IsLabelFunctionEnabled;
            style.LabelFunctionsScript = LabelFunctionsScript;

            style.BestPlacementSymbolWidth = BestPlacementSymbolWidth;
            style.BestPlacementSymbolHeight = BestPlacementSymbolHeight;
            style.BestPlacement = BestPlacement;

            style.SuppressPartialLabels = SuppressPartialLabels;

            if (AbbreviationDictionary != null)
            {
                style.AbbreviationDictionary = new Dictionary<string, string>(AbbreviationDictionary);
            }

            if (Filters != null)
            {
                foreach (var filter in Filters)
                {
                    style.Filters.Add(filter);
                }
            }

            //if (CustomTextStyles != null)
            //{
            //    foreach (var textStyle in CustomTextStyles)
            //    {
            //        if (textStyle != null)
            //        {
            //            style.CustomTextStyles.Add((TextStyle)textStyle.CloneDeep());
            //        }
            //    }
            //}

            if (LabelFunctionColumnNames != null)
            {
                foreach (var item in LabelFunctionColumnNames)
                {
                    style.LabelFunctionColumnNames[item.Key] = item.Value;
                }
            }

            //if (LabelPositions != null)
            //{
            //    foreach (var item in LabelPositions)
            //    {
            //        style.LabelPositions[item.Key] = item.Value;
            //    }
            //}

            return style;
        }

        private static GeoBrush CloneGeoBrush(GeoBrush brush)
        {
            if (brush == null) return null;
            var solidBrush = brush as GeoSolidBrush;
            if (solidBrush != null)
            {
                return new GeoSolidBrush(solidBrush.Color);
            }
            return brush;
        }

        private static GeoFont CloneGeoFont(GeoFont font)
        {
            if (font == null) return null;
            return new GeoFont(font.FontName, font.Size, font.Style, font.Unit);
        }

        private static GeoImage CloneGeoImage(GeoImage image)
        {
            if (image == null) return null;

            if (!string.IsNullOrEmpty(image.PathFilename))
            {
                var cloned = new GeoImage(image.PathFilename);
                cloned.Opacity = image.Opacity;
                return cloned;
            }

            if (image.NativeImage != null)
            {
                var cloned = new GeoImage(image.NativeImage);
                cloned.Opacity = image.Opacity;
                return cloned;
            }

            return image;
        }

        protected override void DrawSampleCore(GeoCanvas canvas, DrawingRectangleF drawingRectangleF)
        {
            RectangleShape rectangle = ToWorldCoordinate(canvas, drawingRectangleF);

            Feature feature = new Feature(rectangle.GetCenterPoint());
            feature.ColumnValues.Add(TextColumnName, "A");
            Feature[] features = new Feature[1] { feature };
            var a = CloneDeep();
            IconTextStyle style = (IconTextStyle)a;
            style.SuppressPartialLabels = false;
            style.TextPlacement = TextPlacement.Center;
            style.IsLabelFunctionEnabled = false;
            style.Draw(features, canvas, new Collection<SimpleCandidate>(), new Collection<SimpleCandidate>());
        }

        protected override Collection<string> GetRequiredColumnNamesCore()
        {
            if (enableScript && !string.IsNullOrEmpty(labelFunctionsScript))
            {
                Collection<string> requiredFieldNames = new Collection<string>();
                foreach (var item in labelFunctionColumnNames)
                {
                    requiredFieldNames.Add(item.Value);
                }
                return requiredFieldNames;
            }
            else
            {
                return base.GetRequiredColumnNamesCore();
            }
        }

        private RectangleShape ToWorldCoordinate(GeoCanvas canvas, DrawingRectangleF drawingRectangle)
        {
            PointShape upperLeftPoint = MapUtil.ToWorldCoordinate(canvas.CurrentWorldExtent, drawingRectangle.CenterX - drawingRectangle.Width / 2, drawingRectangle.CenterY - drawingRectangle.Height / 2, canvas.Width, canvas.Height);
            PointShape lowerRightPoint = MapUtil.ToWorldCoordinate(canvas.CurrentWorldExtent, drawingRectangle.CenterX + drawingRectangle.Width / 2, drawingRectangle.CenterY + drawingRectangle.Height / 2, canvas.Width, canvas.Height);

            RectangleShape worldRectangle = new RectangleShape(upperLeftPoint, lowerRightPoint);
            return worldRectangle;
        }

        protected override string FormatCore(string text, BaseShape labeledShape)
        {
            text = base.FormatCore(text, labeledShape);
            text = FormalizeLinkColumnValue(text, DefaultSeparator);
            text = FormalizeLabelFunctions(text);
            return text;
        }

        private static string FormalizeLabelFunctions(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                #region LastIndexOf function

                if (text.ToLowerInvariant().Contains("lastindexof(") && text.Contains(")") && text.Contains(",") && text.Contains("\""))
                {
                    var items = GetFunctionItemsWithQuotes(text, "lastindexof");
                    foreach (var item in items)
                    {
                        string result = GetLastIndexOfResultString(item);
                        text = text.Replace(item, result);
                    }
                }

                #endregion

                #region IndexOf function

                if (text.ToLowerInvariant().Contains("indexof(") && text.Contains(")") && text.Contains(","))
                {
                    var items = GetFunctionItemsWithQuotes(text, "indexof");
                    foreach (var item in items)
                    {
                        string result = GetIndexOfResultString(item);
                        text = text.Replace(item, result);
                    }
                }

                #endregion

                #region LEFT function

                if (text.ToLowerInvariant().Contains("left(") && text.Contains(")") && text.Contains(","))
                {
                    var items = GetFunctionItems(text, "left");
                    foreach (var item in items)
                    {
                        string result = GetLeftResultString(item);
                        text = text.Replace(item, result);
                    }
                }

                #endregion

                #region RIGHT function

                if (text.ToLowerInvariant().Contains("right(") && text.Contains(")") && text.Contains(","))
                {
                    var items = GetFunctionItems(text, "right");
                    foreach (var item in items)
                    {
                        string result = GetRightResultString(item);
                        text = text.Replace(item, result);
                    }
                }

                #endregion

                #region SUBSTRING function

                if (text.ToLowerInvariant().Contains("substring(") && text.Contains(")") && text.Contains(","))
                {
                    var items = GetFunctionItems(text, "substring");
                    foreach (var item in items)
                    {
                        string result = GetSubstringResultString(item);
                        text = text.Replace(item, result);
                    }
                }

                #endregion

                #region FORMAT function: support number/date

                if (text.ToLowerInvariant().Contains("format(") && text.Contains(")") && text.Contains(","))
                {
                    var items = GetFunctionItemsWithQuotes(text, "format");
                    foreach (var item in items)
                    {
                        string result = GetFormatResultString(item);
                        text = text.Replace(item, result);
                    }
                }

                #endregion

                #region TRIM function

                if (text.ToLowerInvariant().Contains("trim(") && text.Contains(")"))
                {
                    var items = GetFunctionItems(text, "trim");
                    foreach (var item in items)
                    {
                        string result = GetTrimResultString(item);
                        text = text.Replace(item, result);
                    }
                }

                #endregion
            }
            return text;
        }

        #region Label functions
        private static string GetLeftResultString(string text)
        {
            if (text.StartsWith("left(", StringComparison.InvariantCultureIgnoreCase) && text.EndsWith(")", StringComparison.InvariantCultureIgnoreCase) && text.Contains(","))
            {
                int commaIndex = text.LastIndexOf(',');
                if (commaIndex != -1)
                {
                    string number = text.Substring(commaIndex + 1, text.Length - commaIndex - 2).Trim();
                    int charactersReturnedCount = 0;
                    if (int.TryParse(number, out charactersReturnedCount))
                    {
                        int startIndex = 5; //left(
                        text = text.Substring(startIndex, commaIndex - startIndex);
                        if (text.Length > charactersReturnedCount)
                        {
                            text = text.Substring(0, charactersReturnedCount);
                        }
                    }
                }
            }
            return text;
        }

        private static string GetRightResultString(string text)
        {
            if (text.StartsWith("right(", StringComparison.InvariantCultureIgnoreCase) && text.EndsWith(")") && text.Contains(","))
            {
                int commaIndex = text.LastIndexOf(',');
                if (commaIndex != -1)
                {
                    string number = text.Substring(commaIndex + 1, text.Length - commaIndex - 2).Trim();
                    int charactersReturnedCount = 0;
                    if (int.TryParse(number, out charactersReturnedCount))
                    {
                        int startIndex = 6; //right(
                        text = text.Substring(startIndex, commaIndex - startIndex);
                        if (text.Length > charactersReturnedCount)
                        {
                            text = text.Substring(text.Length - charactersReturnedCount, charactersReturnedCount);
                        }
                    }
                }
            }
            return text;
        }

        private static string GetSubstringResultString(string text)
        {
            if (text.StartsWith("substring(", StringComparison.InvariantCultureIgnoreCase) && text.EndsWith(")") && text.Contains(","))
            {
                int secondCommaIndex = text.LastIndexOf(',');
                string subText = text.Substring(0, secondCommaIndex);
                int fisrtCommaIndex = subText.LastIndexOf(',');
                bool onlyOneNumber = false;
                if (secondCommaIndex != -1 && fisrtCommaIndex != -1)
                {
                    string firstNumber = text.Substring(fisrtCommaIndex + 1, secondCommaIndex - fisrtCommaIndex - 1).Trim();
                    string secondNumber = text.Substring(secondCommaIndex + 1, text.Length - secondCommaIndex - 2).Trim();
                    int startPosition = 0;
                    int charactersReturnedCount = 0;
                    bool isFirstSuccess = int.TryParse(firstNumber, out startPosition);
                    bool isSecondSuccess = int.TryParse(secondNumber, out charactersReturnedCount);
                    if (isFirstSuccess && isSecondSuccess)
                    {
                        int startIndex = 10; //substring(
                        text = text.Substring(startIndex, fisrtCommaIndex - startIndex);
                        if (text.Length > startPosition)
                        {
                            if (text.Length - startPosition >= charactersReturnedCount)
                            {
                                text = text.Substring(startPosition - 1, charactersReturnedCount);
                            }
                            else
                            {
                                text = text.Substring(startPosition - 1, text.Length - startPosition + 1);
                            }
                        }
                    }
                    else if (isSecondSuccess && !isFirstSuccess)
                    {
                        onlyOneNumber = true;
                    }
                }
                else if (secondCommaIndex != -1 && fisrtCommaIndex == -1)
                {
                    onlyOneNumber = true;
                }

                if (onlyOneNumber)
                {
                    string secondNumber = text.Substring(secondCommaIndex + 1, text.Length - secondCommaIndex - 2).Trim();
                    int startPosition = 0;
                    if (int.TryParse(secondNumber, out startPosition))
                    {
                        int startIndex = 10; //substring(
                        text = text.Substring(startIndex, secondCommaIndex - startIndex);
                        if (text.Length > startPosition)
                        {
                            text = text.Substring(startPosition, text.Length - startPosition);
                        }
                    }
                }
            }
            return text;
        }

        private static string GetFormatResultString(string text)
        {
            if (text.StartsWith("format(", StringComparison.InvariantCultureIgnoreCase) && text.EndsWith(")", StringComparison.InvariantCultureIgnoreCase) && text.Contains(","))
            {
                int firstQuoteIndex = text.IndexOf('"');
                int lastQuoteIndex = text.LastIndexOf('"');
                string firstText = text.Substring(0, firstQuoteIndex);
                int commaIndex = firstText.LastIndexOf(',');
                string format = text.Substring(firstQuoteIndex + 1, lastQuoteIndex - firstQuoteIndex - 1).Trim();
                if (!string.IsNullOrEmpty(format))
                {
                    int startIndex = 7; //format(
                    text = text.Substring(startIndex, commaIndex - startIndex);
                    double doubleValue = 0;
                    DateTime dateTimeValue = DateTime.Now;
                    if (double.TryParse(text, out doubleValue))
                    {
                        text = string.Format("{0:" + format + "}", doubleValue);
                    }
                    else if (DateTime.TryParse(text, out dateTimeValue))
                    {
                        text = string.Format("{0:" + format + "}", dateTimeValue);
                    }
                    else
                    {
                        text = string.Format("{0:" + format + "}", text);
                    }
                }
            }

            return text;
        }

        private static string GetIndexOfResultString(string text)
        {
            if (text.StartsWith("indexof(", StringComparison.InvariantCultureIgnoreCase) && text.EndsWith(")") && text.Contains(",") && text.Contains("\""))
            {
                int commaIndex = text.LastIndexOf(',');
                int firstQuoteIndex = text.IndexOf('"');
                int lastQuoteIndex = text.LastIndexOf('"');
                string indexCharacters = text.Substring(firstQuoteIndex + 1, lastQuoteIndex - firstQuoteIndex - 1).Trim();
                if (!string.IsNullOrEmpty(indexCharacters))
                {
                    int startIndex = 8; //IndexOf(
                    text = text.Substring(startIndex, commaIndex - startIndex);
                    if (text.ToUpperInvariant().Contains(indexCharacters.ToUpperInvariant()))
                    {
                        int index = text.IndexOf(indexCharacters, StringComparison.InvariantCultureIgnoreCase);
                        index++;
                        text = index.ToString();
                    }
                    else
                    {
                        text = "0";
                    }
                }
            }
            return text;
        }

        private static string GetLastIndexOfResultString(string text)
        {
            if (text.StartsWith("LastIndexOf(", StringComparison.InvariantCultureIgnoreCase) && text.EndsWith(")", StringComparison.InvariantCultureIgnoreCase) && text.Contains(",") && text.Contains("\""))
            {
                int commaIndex = text.LastIndexOf(',');
                string indexCharacters = text.Substring(commaIndex + 1, text.Length - commaIndex - 2).Trim();
                if (!string.IsNullOrEmpty(indexCharacters))
                {
                    int startIndex = 12; //LastIndexOf(
                    text = text.Substring(startIndex, commaIndex - startIndex);
                    if (text.ToUpperInvariant().Contains(indexCharacters.ToUpperInvariant()))
                    {
                        int index = text.LastIndexOf(indexCharacters, StringComparison.InvariantCultureIgnoreCase);
                        index++;
                        text = index.ToString();
                    }
                    else
                    {
                        text = "0";
                    }
                }
            }
            return text;
        }

        private static string GetTrimResultString(string text)
        {
            if (text.StartsWith("trim(", StringComparison.InvariantCultureIgnoreCase) && text.EndsWith(")", StringComparison.InvariantCultureIgnoreCase))
            {
                int startIndex = 5; //trim(
                text = text.Substring(startIndex, text.Length - startIndex - 1);
                if (!string.IsNullOrEmpty(text))
                {
                    text = text.Trim();
                }
            }
            return text;
        }

        private static Collection<string> GetFunctionItems(string text, string keyword)
        {
            Collection<string> results = new Collection<string>();

            int firstFormatIndex = 0;
            string restText = text;
            while (restText.ToLowerInvariant().Contains(keyword + "(") && restText.Contains(")") && restText.Contains(","))
            {
                firstFormatIndex = restText.ToLowerInvariant().IndexOf(keyword + "(");
                string substring = restText.Substring(firstFormatIndex);
                int firstBracketIndex = substring.IndexOf(')');
                string oneFormat = substring.Substring(0, firstBracketIndex + 1);
                results.Add(oneFormat);

                restText = restText.Replace(oneFormat, "");
            }

            return results;
        }

        private static Collection<string> GetFunctionItemsWithQuotes(string text)
        {
            Collection<string> results = new Collection<string>();
            string[] functions = { "lastindexof", "indexof", "left", "right", "substring", "format", "trim" };
            foreach (var item in functions)
            {
                Collection<string> values = GetFunctionItemsWithQuotes(text, item);
                foreach (var value in values)
                {
                    results.Add(value);
                }
            }

            return results;
        }

        private static Collection<string> GetFunctionItemsWithQuotes(string text, string keyword)
        {
            Collection<string> results = new Collection<string>();

            int firstFormatIndex = 0;
            string restText = text;
            while (restText.ToLowerInvariant().Contains(keyword + "(") && restText.Contains(")") && restText.Contains(",") && restText.Contains("\""))
            {
                firstFormatIndex = restText.ToLowerInvariant().IndexOf(keyword + "(");
                string substring = restText.Substring(firstFormatIndex);
                int firstBracketIndex = substring.IndexOf(')');
                string oneFormat = substring.Substring(0, firstBracketIndex + 1);
                results.Add(oneFormat);

                restText = restText.Replace(oneFormat, "");
            }

            Console.WriteLine(results);

            return results;
        }
        #endregion

        protected override void DrawCore(IEnumerable<Feature> features, GeoCanvas canvas, Collection<SimpleCandidate> labelsInThisLayer, Collection<SimpleCandidate> labelsInAllLayers)
        {
            string orignalTextColumnName = TextColumnName;
            GeoPen tempHalo = null;
            AreaStyle tempMask = null;
            if (!IsMaskEnabled && Mask != null)
            {
                tempMask = Mask;
                Mask = null;
            }
            if (!IsHaloEnabled)
            {
                tempHalo = HaloPen;
                HaloPen = new GeoPen();
            }

            Collection<Feature> normalFeatures = new Collection<Feature>();

            foreach (var feature in features)
            {
                Feature clonedFeature = new Feature(feature.GetWellKnownBinary(), feature.Id, feature.ColumnValues);
                normalFeatures.Add(clonedFeature);
            }

            if (enableScript && !string.IsNullOrEmpty(labelFunctionsScript))
            {
                normalFeatures = InvokeLabelFunction(normalFeatures);
            }

            base.DrawCore(normalFeatures, canvas, labelsInThisLayer, labelsInAllLayers);

            if (tempHalo != null)
            {
                HaloPen = tempHalo;
            }
            if (tempMask != null)
            {
                Mask = tempMask;
            }
            TextColumnName = orignalTextColumnName;
        }

        public static Collection<Feature> ReplaceColumnValues(Collection<Feature> orginalFeatures, IEnumerable<string> originalColumnNames)
        {
            Collection<Feature> resultFeatures = new Collection<Feature>();

            for (int i = 0; i < orginalFeatures.Count; i++)
            {
                Feature tempFeature = orginalFeatures[i];
                Dictionary<string, string> columnValues = new Dictionary<string, string>();
                foreach (var item in tempFeature.ColumnValues)
                {
                    columnValues.Add(item.Key, item.Value);
                }
                resultFeatures.Add(new Feature(tempFeature.GetWellKnownBinary(), tempFeature.Id, CombineFieldValues(columnValues, originalColumnNames)));
            }

            return resultFeatures;
        }

        private static Dictionary<string, string> CombineFieldValues(Dictionary<string, string> columnValues, IEnumerable<string> originalColumnNames)
        {
            Dictionary<string, string> combineColumnValues = new Dictionary<string, string>();

            foreach (string columnName in originalColumnNames)
            {
                if (columnName.IndexOf('[') == -1 && columnValues.ContainsKey(columnName.ToLowerInvariant()))
                {
                    combineColumnValues[columnName] = columnValues[columnName.ToLowerInvariant()];//fix bug for MGIS-8051
                }
                else
                {
                    Collection<string> fields = SplitMultiFieldNameIncludeBlacket(columnName);
                    if (fields.Count == 0) { continue; }
                    string result = columnName;
                    foreach (string field in fields)
                    {
                        string trimFieldName = field.ToLowerInvariant().Trim(new char[] { '[', ']' });

                        if (columnValues.ContainsKey(trimFieldName))
                        {
                            result = result.Replace(field, columnValues[trimFieldName].Trim());
                        }
                        else
                        {
                            result = result.Replace(field, string.Empty);
                        }
                    }

                    if (result == columnName)
                    {
                        result = string.Empty;
                    }
                    combineColumnValues[columnName] = result.Trim();
                }
            }

            return combineColumnValues;
        }

        private static Collection<string> SplitMultiFieldNameIncludeBlacket(string multiFieldName)
        {
            Collection<string> fields = new Collection<string>();

            int startIndex = 0;

            while (true)
            {
                int openBlacketPosition = startIndex = multiFieldName.IndexOf('[', startIndex);
                if (startIndex == -1) { break; }
                int closeBlacketPosition = startIndex = multiFieldName.IndexOf(']', startIndex);
                if (startIndex == -1) { throw new NotSupportedException("The format of Multi-FieldNames isn't correct."); }

                string field = multiFieldName.Substring(openBlacketPosition, closeBlacketPosition - openBlacketPosition + 1);
                fields.Add(field);
            }

            return fields;
        }

        private void Initialize()
        {
            DrawingLevel = DrawingLevel.LabelLevel;
            HaloPen = new GeoPen(GeoColors.White, 1f);
            labelFunctionColumnNames = new Dictionary<string, string>();
        }

        private Collection<Feature> InvokeLabelFunction(Collection<Feature> features)
        {
            TextColumnName = "LabelFunction";
            Collection<Feature> resultFeatures = new Collection<Feature>();

            string parameters = "";
            foreach (var item in labelFunctionColumnNames)
            {
                parameters += "string ";
                parameters += item.Key;
                parameters += ",";
            }
            parameters = parameters.TrimEnd(',');

            string code = LoadString();
            code = code.Replace("#CODE#", labelFunctionsScript);
            code = code.Replace("#PARAMETERS#", parameters);

            Assembly assembly = CSScript.LoadCode(code);
            using (AsmHelper helper = new AsmHelper(assembly))
            {
                foreach (var feature in features)
                {
                    if (labelFunctionColumnNames.Count == 1)
                    {
                        KeyValuePair<string, string> pair = labelFunctionColumnNames.First();
                        if (feature.ColumnValues.ContainsKey(pair.Value))
                        {
                            string parameter = feature.ColumnValues[pair.Value];
                            object resultObject = helper.Invoke("ScriptTemplate.Execute", parameter);
                            feature.ColumnValues.Clear();
                            feature.ColumnValues[TextColumnName] = resultObject.ToString();
                            resultFeatures.Add(feature);
                        }
                    }
                    else if (labelFunctionColumnNames.Count == 2)
                    {
                        List<string> values = labelFunctionColumnNames.Select(l => l.Value).ToList();
                        if (feature.ColumnValues.ContainsKey(values[0]) && feature.ColumnValues.ContainsKey(values[1]))
                        {
                            string parameter1 = feature.ColumnValues[values[0]];
                            string parameter2 = feature.ColumnValues[values[1]];
                            object resultObject = helper.Invoke("ScriptTemplate.Execute", parameter1, parameter2);
                            feature.ColumnValues.Clear();
                            feature.ColumnValues[TextColumnName] = resultObject.ToString();
                            resultFeatures.Add(feature);
                        }
                    }
                    else if (labelFunctionColumnNames.Count == 3)
                    {
                        List<string> values = labelFunctionColumnNames.Select(l => l.Value).ToList();
                        if (feature.ColumnValues.ContainsKey(values[0]) && feature.ColumnValues.ContainsKey(values[1]) && feature.ColumnValues.ContainsKey(values[2]))
                        {
                            string parameter1 = feature.ColumnValues[values[0]];
                            string parameter2 = feature.ColumnValues[values[1]];
                            string parameter3 = feature.ColumnValues[values[2]];
                            object resultObject = helper.Invoke("ScriptTemplate.Execute", parameter1, parameter2, parameter3);
                            feature.ColumnValues.Clear();
                            feature.ColumnValues[TextColumnName] = resultObject.ToString();
                            resultFeatures.Add(feature);
                        }
                    }
                }
            }

            return resultFeatures;
        }

        private static string LoadString()
        {
            return @"using System;
                    using System.Linq;

                    public class ScriptTemplate
                    {
                        public static string Execute(#PARAMETERS#)
                        {
		                   #CODE#
                        }
                    }";
        }

        private static string FormalizeLinkColumnValue(string text, string separator)
        {
            string temp = string.Empty;
            Collection<string> results = new Collection<string>();
            if (!string.IsNullOrEmpty(text) && (text.Contains('{') && text.Contains('}')))
            {
                bool isStart = false;
                bool isEnd = false;
                foreach (var item in text)
                {
                    if (item == '{')
                    {
                        isStart = true;
                        continue;
                    }
                    if (item == '}')
                    {
                        isEnd = true;
                        isStart = false;
                    }
                    if (item != '}' && isStart)
                    {
                        temp += item.ToString();
                    }
                    else if (isEnd)
                    {
                        results.Add(temp);
                        temp = string.Empty;
                        isEnd = false;
                    }
                }
            }

            if (results.All(r => r != null))
            {
                for (int i = 0; i < results.Count; i++)
                {
                    string tempValue = "|";
                    for (int j = 0; j < i; j++)
                    {
                        tempValue += "|";
                    }
                    text = text.Replace("{" + results[i] + "}", "{" + tempValue + "}");
                }

                if (results.Count > 0)
                {
                    int count = 0;
                    foreach (var item in results)
                    {
                        string[] values = item.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                        if (values.Length > count)
                        {
                            count = values.Length;
                        }
                    }

                    Collection<Collection<string>> allValues = new Collection<Collection<string>>();
                    for (int i = 0; i < count; i++)
                    {
                        Collection<string> elements = new Collection<string>();
                        foreach (var item in results)
                        {
                            string[] values = item.Split(new string[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                            if (values.Length > i)
                            {
                                elements.Add(values[i]);
                            }
                            else
                            {
                                elements.Add(values.FirstOrDefault());
                            }
                        }
                        allValues.Add(elements);
                    }

                    string result = text;
                    string value = string.Empty;
                    foreach (var values in allValues)
                    {
                        for (int i = 0; i < values.Count; i++)
                        {
                            string tempValue = "|";
                            for (int k = 0; k < i; k++)
                            {
                                tempValue += "|";
                            }
                            result = result.Replace("{" + tempValue + "}", values[i]);
                        }
                        value += result + "\r\n";
                        result = text;
                    }

                    text = value;
                }
            }

            if (text.Contains(DefaultSeparator))
            {
                string[] values = text.Split(new string[] { DefaultSeparator }, StringSplitOptions.RemoveEmptyEntries);
                string value = string.Empty;
                foreach (var item in values)
                {
                    value += item + "\r\n";
                }
                text = value;
            }

            return text;
        }
    }
}
