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
using System.IO;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    internal class RecordAnnotation
    {
        private string textLocationLonlat;
        private string textLocationXY;
        private string textAngle;
        private string textSize;
        private string textFont;
        private string text;
        private string numberOfChars;

        public string TextLocationLonlat
        {
            get { return textLocationLonlat; }
            set { textLocationLonlat = value; }
        }

        public string TextLocationXY
        {
            get { return textLocationXY; }
            set { textLocationXY = value; }
        }

        public string TextAngle
        {
            get { return textAngle; }
            set { textAngle = value; }
        }

        public string TextSize
        {
            get { return textSize; }
            set { textSize = value; }
        }

        public string TextFont
        {
            get { return textFont; }
            set { textFont = value; }
        }

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public string NumberOfChars
        {
            get { return numberOfChars; }
            set { numberOfChars = value; }
        }

        public void Read(BinaryReader reader)
        {
            textLocationLonlat = new string(reader.ReadChars(15));
            textLocationXY = new string(reader.ReadChars(18));

            textAngle = new string(reader.ReadChars(5));
            textSize = new string(reader.ReadChars(6));
            numberOfChars = new string(reader.ReadChars(2));
            textFont = new string(reader.ReadChars(1));
            text = new string(reader.ReadChars(32));
        }
    }
}