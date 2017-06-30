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
using ThinkGeo.MapSuite.Shapes;
using ThinkGeo.MapSuite.Wpf;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    [Serializable]
    public class SnappingAdapter
    {
        private static readonly DistanceUnit defaultDistanceUnit = DistanceUnit.Meter;

        [NonSerialized]
        private double distance;

        [NonSerialized]
        private DistanceUnit distanceUnit;

        protected SnappingAdapter(double distance, DistanceUnit distanceUnit)
        {
            this.distance = distance;
            this.distanceUnit = distanceUnit;
        }

        public double Distance
        {
            get { return distance; }
            set { distance = value; }
        }

        public DistanceUnit DistanceUnit
        {
            get { return distanceUnit; }
            set { distanceUnit = value; }
        }

        public static SnappingAdapter Convert(double snappingDistance, SnappingDistanceUnit snappingDistanceUnit, MapArguments mapArguments, Vertex worldCoordinate)
        {
            return Convert(snappingDistance, snappingDistanceUnit, mapArguments, new PointShape(worldCoordinate));
        }

        public static SnappingAdapter Convert(double snappingDistance, SnappingDistanceUnit snappingDistanceUnit, MapArguments mapArguments, PointShape worldCoordinate)
        {
            DistanceUnit tempDistanceUnit = defaultDistanceUnit;
            if (snappingDistanceUnit != SnappingDistanceUnit.Pixel)
            {
                tempDistanceUnit = (DistanceUnit)snappingDistanceUnit;
            }

            double tempDistance = snappingDistance;
            if (snappingDistanceUnit == SnappingDistanceUnit.Pixel)
            {
                double worldDistance = snappingDistance * mapArguments.CurrentResolution;
                PointShape targetWorldCoordinate = new PointShape(worldCoordinate.X + worldDistance, worldCoordinate.Y);
                tempDistance = worldCoordinate.GetDistanceTo(targetWorldCoordinate, mapArguments.MapUnit, defaultDistanceUnit);
            }

            return new SnappingAdapter(tempDistance, tempDistanceUnit);
        }
    }
}