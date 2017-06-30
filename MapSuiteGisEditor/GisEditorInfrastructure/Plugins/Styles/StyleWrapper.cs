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


using System.Xml.Linq;
using ThinkGeo.MapSuite.Styles;

namespace ThinkGeo.MapSuite.GisEditor
{
    internal class StyleWrapper
    {
        private double upperScale;
        private double lowerScale;
        private CompositeStyle style;

        public StyleWrapper(XElement styleXElement)
        {
            Parse(styleXElement);
        }

        public StyleWrapper(double upperScale, double lowerScale, CompositeStyle style)
        {
            this.upperScale = upperScale;
            this.lowerScale = lowerScale;
            this.style = style;
        }

        public double UpperScale
        {
            get { return upperScale; }
        }

        public double LowerScale
        {
            get { return lowerScale; }
        }

        public CompositeStyle Style
        {
            get { return style; }
        }

        public void Parse(XElement styleXElement)
        {
            this.lowerScale = 0;
            this.upperScale = 1000000000;
            XElement upperScaleXElement = styleXElement.Element("UpperScale");
            if (upperScaleXElement != null) double.TryParse(upperScaleXElement.Value, out upperScale);

            XElement lowerScaleXElement = styleXElement.Element("LowerScale");
            if (lowerScaleXElement != null) double.TryParse(lowerScaleXElement.Value, out lowerScale);

            XElement compositeStyleXElement = styleXElement.Element("CompositeStyle");
            if (compositeStyleXElement != null)
            {
                object styleObject = GisEditor.Serializer.Deserialize(compositeStyleXElement.ToString());

                if (styleObject is CompositeStyle)
                {
                    this.style = (CompositeStyle)styleObject;
                }
                else if (styleObject is Style)
                {
                    Style currentStyle = (Style)styleObject;
                    CompositeStyle compositeStyle = new CompositeStyle();
                    compositeStyle.Name = currentStyle.Name;
                    compositeStyle.Styles.Add(currentStyle);
                    this.style = compositeStyle;
                }
            }
        }

        public XElement ToXml()
        {
            string styleXml = GisEditor.Serializer.Serialize(this.style);
            return new XElement("Style"
           , new XElement("LowerScale", lowerScale)
           , new XElement("UpperScale", upperScale)
           , XElement.Parse(styleXml));
        }
    }
}
