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
using System.Globalization;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class BasFeatureEntity
    {
        private Collection<BasAnnotation> annotations;
        private Dictionary<string, string> columns;
        private BaseShape baseshape;
        private long offset;

        public long Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public string Id
        {
            get { return offset.ToString(CultureInfo.InvariantCulture); }
            set
            {
                long tmpOffset;
                if (long.TryParse(value, out tmpOffset))
                {
                    offset = tmpOffset;
                }

                if (baseshape != null)
                {
                    baseshape.Id = value;
                }
            }
        }

        public BasFeatureEntity()
        {
        }

        public Collection<BasAnnotation> Annotations
        {
            get
            {
                if (annotations == null)
                {
                    annotations = new Collection<BasAnnotation>();
                }
                return annotations;
            }
        }

        public Dictionary<string, string> Columns
        {
            get
            {
                if (columns == null)
                {
                    columns = new Dictionary<string, string>();
                };
                return columns;
            }
        }

        private RectangleShape GetAnnotationsRect()
        {
            if (annotations.Count < 1)
            {
                return null;
            }

            RectangleShape rect = annotations[0].Position.GetBoundingBox();

            for (int i = 1; i < annotations.Count; i++)
            {
                rect.ExpandToInclude(annotations[i].Position);
            }

            rect.Id = Id;

            return rect;
        }

        public BaseShape Shape
        {
            get
            {
                if (baseshape != null)
                {
                    return baseshape;
                }
                else
                {
                    return GetAnnotationsRect();
                }
            }
            set { baseshape = value; }
        }
    }
}