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
using System.Diagnostics;
using System.Linq;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    internal class MessageBoxHelper
    {
        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="messageBoxButton">The message box button.</param>
        /// <param name="messageBoxImage">The message box image.</param>
        [Conditional(CompileConditions.Release)]
        public static void ShowMessage(string content, string caption, System.Windows.Forms.MessageBoxButtons messageBoxButton, System.Windows.Forms.MessageBoxIcon messageBoxImage)
        {
            System.Windows.Forms.MessageBox.Show(content, caption, messageBoxButton, messageBoxImage);
        }

        public static bool ShowWarningMessageIfSoManyCount(IEnumerable<FeatureLayer> featureLayers)
        {
            bool isContinue = true;
            featureLayers.ForEach(f => f.Open());
            int sum = featureLayers.Where(f => f.FeatureSource.CanGetCountQuickly()).Sum(s => s.FeatureSource.GetCount());

            if (sum > 50000)
            {
                //MessageBoxResult result = MessageBox.Show(string.Format("Layer(s) contain(s) a large amount of records, it might spend too much time to process. Do you want to continue?"), "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                //if (result == MessageBoxResult.No)
                //{
                //    isContinue = false;
                //}
            }

            return isContinue;
        }

        public static bool ShowWarningMessageIfSoManyCount(FeatureLayer featureLayer)
        {
            bool isContinue = true;

            featureLayer.SafeProcess(() =>
            {
                if (featureLayer.FeatureSource.CanGetCountQuickly())
                {
                    int count = featureLayer.FeatureSource.GetCount();
                    if (count > 50000)
                    {
                        //MessageBoxResult result = MessageBox.Show(string.Format("{0} contains a large amount of records, it might spend too much time to process. Do you want to continue?", featureLayer.Name), "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        //if (result == MessageBoxResult.No)
                        //{
                        //    isContinue = false;
                        //}
                    }
                }
            });

            return isContinue;
        }
    }
}
