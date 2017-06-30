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

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public class BoundingBoxSelectCompletedEventArgs : EventArgs
    {
        private RectangleShape resultBoundingBox;
        private double resultScale;
        private bool cancel;

        public BoundingBoxSelectCompletedEventArgs(RectangleShape resultBoundingBox, double resultScale = -1)
        {
            this.resultBoundingBox = resultBoundingBox;
            this.resultScale = resultScale;
        }

        public BoundingBoxSelectCompletedEventArgs(bool cancel)
        {
            this.cancel = cancel;
        }

        public RectangleShape ResultBoundingBox
        {
            get { return resultBoundingBox; }
        }

        public double ResultScale
        {
            get { return resultScale; }
        }

        public bool Cancel
        {
            get { return cancel; }
        }

        public byte[] ImageBytes { get; set; }
    }
}