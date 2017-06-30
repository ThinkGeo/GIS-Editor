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
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ThinkGeo.MapSuite.Serialize;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class GisEditorXmlGeoSerializationFormatter : XmlGeoSerializationFormatter
    {
        private static readonly string symbolPointStyleFullTypeName = "ThinkGeo.MapSuite.WpfDesktopEdition.Extension.SymbolPointStyle, WpfDesktopEditionExtension,";

        protected override GeoObjectModel LoadCore(System.IO.Stream stream)
        {
            try
            {
                string content = string.Empty;
                using (StreamReader reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }

                Dictionary<string, string> dic = new Dictionary<string, string>();

                Stream gisEditorFixResourceStream = this.GetType().Assembly.GetManifestResourceStream("ThinkGeo.MapSuite.GisEditor.Shares.Serialize.GisEditorResolveSerializedIssue.xml");
                XElement gisEditorFixElement = XElement.Load(gisEditorFixResourceStream);
                foreach (var item in gisEditorFixElement.Descendants("Pair"))
                {
                    string guid = Guid.NewGuid().ToString();
                    string oldContent = item.Element("Old").Value;
                    string newContent = item.Element("New").Value;
                    content = content.Replace(oldContent, guid);
                    dic.Add(guid, newContent);
                }

                Stream fixResourceStream = typeof(XmlGeoSerializationFormatter).Assembly.GetManifestResourceStream("ThinkGeo.MapSuite.Serialize.Serializer.ResolveSerializedIssue.xml");
                XElement fixElement = XElement.Load(fixResourceStream);
                foreach (var item in fixElement.Descendants("Pair"))
                {
                    string oldContent = item.Element("Old").Value;
                    string newContent = item.Element("New").Value;

                    content = FixSymbolPointStyleTypeIssue(content, oldContent);
                    content = content.Replace(oldContent, newContent);
                }

                foreach (var item in dic)
                {
                    content = content.Replace(item.Key, item.Value);
                }

                content = FixCompatibleIssues(content);
                Stream newStream = new MemoryStream();
                XElement.Parse(content).Save(newStream);
                newStream.Seek(0, SeekOrigin.Begin);
                stream = newStream;
            }
            catch { }
            return base.LoadCore(stream);
        }

        private static string FixSymbolPointStyleTypeIssue(string content, string oldContent)
        {
            if (oldContent.Equals(symbolPointStyleFullTypeName) && content.Contains(oldContent))
            {
                XElement rootElement = XElement.Parse(content);
                List<XElement> elements = rootElement.Descendants("Element")
                    .Where(x =>
                    {
                        bool isMatched = false;
                        XAttribute typeAttr = x.Attribute("type");
                        XElement imageElmt = x.Element("image");
                        XElement pointTypeElmt = x.Element("pointType");
                        if (typeAttr != null
                            && typeAttr.Value.StartsWith(oldContent)
                            && imageElmt != null
                            && imageElmt.Element("imageBytes") != null
                            && pointTypeElmt == null)
                        {
                            isMatched = true;
                        }
                        return isMatched;
                    }).ToList();

                if (elements.Count > 0)
                {
                    foreach (var symbolPointStyleElement in elements)
                    {
                        symbolPointStyleElement.Add(new XElement("pointType", "Bitmap"));
                    }

                    content = rootElement.ToString();
                }
            }
            return content;
        }

        private string FixCompatibleIssues(string content)
        {
            XElement xml = XElement.Parse(content);
            IEnumerable<XElement> entityElements = xml.Descendants("Element");
            foreach (var entityElement in entityElements)
            {
                XAttribute entityTypeElement = entityElement.Attribute("type");
                FixFilterExpressionCompatibleIssue(entityElement, entityTypeElement);
                FixSymbolPointStyleWithoutPointTypeIssue(entityElement, entityTypeElement);
                FixPointStyleWithCustomPointTypeIssue(entityElement, entityTypeElement);
            }

            content = xml.ToString();
            return content;
        }

        // We removed "Custom" from PointType, now we need to replace Custom with WellPointStype.
        private static void FixPointStyleWithCustomPointTypeIssue(XElement entityElement, XAttribute entityTypeElement)
        {
            if (entityElement.ToString().Contains("<pointType>Custom</pointType>")
                && entityTypeElement != null
                && entityTypeElement.Value.Contains("ThinkGeo.MapSuite.Styles.PointStyle"))
            {
                IEnumerable<XElement> customPointStyleElements = entityElement.Descendants("customPointStyles");
                foreach (var item in customPointStyleElements) item.Remove();

                IEnumerable<XElement> characterFontElements = entityElement.Descendants("characterFont");
                foreach (var item in characterFontElements) item.Remove();

                XElement characterIndexX = entityElement.Descendants("characterIndex").FirstOrDefault();
                int index = 1;
                if (characterIndexX != null)
                {
                    index = int.Parse(characterIndexX.Value);
                    characterIndexX.Remove();
                }

                IEnumerable<XElement> characterBrushElements = entityElement.Descendants("characterSolidBrush");
                foreach (var item in characterBrushElements) item.Remove();

                IEnumerable<XElement> windingFontCacheElements = entityElement.Descendants("wingdingsFontCache");
                foreach (var item in windingFontCacheElements) item.Remove();

                entityTypeElement.Value = entityTypeElement.Value.Replace("ThinkGeo.MapSuite.Styles.PointStyle", "ThinkGeo.MapSuite.Styles.WellPointStyle");
                XElement pointTypeElement = entityElement.Element("pointType");
                if (pointTypeElement != null && "Custom".Equals(pointTypeElement.Value, StringComparison.OrdinalIgnoreCase))
                {
                    pointTypeElement.ReplaceWith(new XElement("wellPointIndex", index));
                }
            }
        }

        private static void FixSymbolPointStyleWithoutPointTypeIssue(XElement entityElement, XAttribute attributes)
        {
            if (attributes != null
                && attributes.Value != null
                && attributes.Value.StartsWith(symbolPointStyleFullTypeName))
            {
                XElement imageNode = entityElement.Element("image");
                if (imageNode != null
                    && imageNode.Element("imageBytes") != null
                    && entityElement.Element("pointType") == null)
                {
                    imageNode.Add(new XElement("pointType", "Bitmap"));
                }
            }
        }

        private static void FixFilterExpressionCompatibleIssue(XElement entityElement, XAttribute attributes)
        {
            if (attributes != null
                && attributes.Value != null
                && attributes.Value.StartsWith("ThinkGeo.MapSuite.Styles.FilterCondition")
                && entityElement.Element("filterExpression") != null)
            {
                XElement expressionElement = entityElement.Element("filterExpression");
                expressionElement.Name = "expression";
            }
        }
    }
}