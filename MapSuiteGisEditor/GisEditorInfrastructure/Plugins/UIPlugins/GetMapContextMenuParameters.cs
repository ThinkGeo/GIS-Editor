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

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// This class represents an arguement to create the context menu items on the map.
    /// It will be used by the UIPlugin.GetMapContextMenuItems(MapContextMenuArguments);
    /// </summary>
    [Serializable]
    public class GetMapContextMenuParameters
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetMapContextMenuParameters" /> class.
        /// </summary>
        public GetMapContextMenuParameters()
            : this(new PointShape(), new PointShape(), double.NaN, double.NaN)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetMapContextMenuParameters" /> class.
        /// </summary>
        /// <param name="screenCoordinates">The screen coordinates.</param>
        /// <param name="worldCoordinates">The world coordinates.</param>
        public GetMapContextMenuParameters(PointShape screenCoordinates
            , PointShape worldCoordinates
            , double currentScale
            , double currentResolution)
        {
            WorldCoordinates = worldCoordinates;
            ScreenCoordinates = screenCoordinates;
            CurrentScale = currentScale;
            CurrentResolution = currentResolution;
        }

        /// <summary>
        /// Gets or sets the world coordinates.
        /// </summary>
        /// <value>
        /// The world coordinates.
        /// </value>
        public PointShape WorldCoordinates { get; set; }

        /// <summary>
        /// Gets or sets the screen coordinates.
        /// </summary>
        /// <value>
        /// The screen coordinates.
        /// </value>
        public PointShape ScreenCoordinates { get; set; }

        public double CurrentScale { get; set; }

        public double CurrentResolution { get; set; }

        public RectangleShape GetClickWorldArea()
        {
            return GetClickWorldArea(4);
        }

        public RectangleShape GetClickWorldArea(int tolerance)
        {
            double offset = CurrentResolution * tolerance;
            RectangleShape rectangle = new RectangleShape(WorldCoordinates.X - offset
                , WorldCoordinates.Y + offset
                , WorldCoordinates.X + offset
                , WorldCoordinates.Y - offset);

            return rectangle;
        }
    }
}