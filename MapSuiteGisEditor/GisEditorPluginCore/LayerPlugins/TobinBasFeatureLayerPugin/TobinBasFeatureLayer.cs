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
using System.Linq;
using System.Reflection;
using ThinkGeo.MapSuite.Drawing;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class TobinBasFeatureLayer : FeatureLayer
    {
        [Obfuscation(Exclude = true)]
        private int progressiveDrawingRecordsCount;

        [Obfuscation(Exclude = true)]
        private string tobinBasFilePathName;

        [Obfuscation(Exclude = true)]
        private int minAnnotationFontSize;

        public TobinBasFeatureLayer(string tobinBasFileName)
        {
            minAnnotationFontSize = 5;
            progressiveDrawingRecordsCount = 12000;
            this.tobinBasFilePathName = tobinBasFileName;
            FeatureSource = new TobinBasFeatureSource(tobinBasFileName);
        }

        public string TobinBasFilePathName
        {
            get { return ((TobinBasFeatureSource)FeatureSource).TobinBasFileName; }
            set { ((TobinBasFeatureSource)FeatureSource).TobinBasFileName = value; }
        }

        public int ProgressiveDrawingRecordsCount
        {
            get { return progressiveDrawingRecordsCount; }
            set { progressiveDrawingRecordsCount = value; }
        }

        public int MinAnnotationFontSize
        {
            get { return minAnnotationFontSize; }
            set { minAnnotationFontSize = value; }
        }

        protected override void DrawCore(GeoCanvas canvas, Collection<SimpleCandidate> labelsInAllLayers)
        {
            // Draw shape like area and point
            base.DrawCore(canvas, labelsInAllLayers);

            // Draw annotation label
            if (!ZoomLevelSet.CustomZoomLevels.Any(z => z.CustomStyles.OfType<CompositeStyle>().SelectMany(c => c.Styles).Any(s => s is TextStyle)))
            {
                Collection<Feature> annotationFeatures = ((TobinBasFeatureSource)FeatureSource).AnnotationFeatures;
                foreach (var item in annotationFeatures)
                {
                    float textSize = float.Parse(item.ColumnValues["TextSize"].ToString());

                    double latDiff = DecimalDegreesHelper.GetLatitudeDifferenceFromDistance(textSize, DistanceUnit.Feet);
                    PointShape startPoint = (PointShape)item.GetShape();
                    PointShape endPoint = new PointShape(startPoint.X, startPoint.Y + latDiff);
                    float fontSize = ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, startPoint, canvas.Width, canvas.Height).Y -
                        ExtentHelper.ToScreenCoordinate(canvas.CurrentWorldExtent, endPoint, canvas.Width, canvas.Height).Y;

                    if (fontSize > minAnnotationFontSize)
                    {
                        TextStyle textStyle = new TextStyle("TextString", new GeoFont(), new GeoSolidBrush(GeoColor.SimpleColors.Black));
                        textStyle.DuplicateRule = LabelDuplicateRule.UnlimitedDuplicateLabels;
                        textStyle.OverlappingRule = LabelOverlappingRule.AllowOverlapping;
                        textStyle.YOffsetInPixel = 1 * fontSize;
                        textStyle.RotationAngle = double.Parse(item.ColumnValues["TextAngle"]);
                        textStyle.Font = new GeoFont("Arial", fontSize);
                        textStyle.Draw(new Collection<Feature> { item }, canvas, new Collection<SimpleCandidate>(), labelsInAllLayers);
                    }
                }
            }
        }

        public override bool HasBoundingBox
        {
            get
            {
                return true;
            }
        }

        public static void BuildIndexFile(string pathFilename)
        {
            //Validators.CheckParameterIsNotNull(pathFilename, "pathFileName");
            //Validators.CheckShapeFileNameIsValid(pathFilename, "pathFileName");

            TobinBasFeatureSource.BuildIndexFile(pathFilename);
        }
    }
}