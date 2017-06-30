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
using System.Linq;
using ThinkGeo.MapSuite.Layers;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SplitWizardViewModel : WizardViewModel<SplitWizardShareObject>
    {
        private SplitWizardShareObject shareObject;

        public SplitWizardViewModel()
        {
            Title = GisEditor.LanguageManager.GetStringResource("ToolsSplitWizardTitle");
            HelpKey = "SplitWizardHelp";

            shareObject = new SplitWizardShareObject();
            shareObject.HighlightFeatureLayer = GisEditor.SelectionManager.GetSelectionOverlay().HighlightFeatureLayer;
            foreach (var layer in GisEditor.ActiveMap.GetFeatureLayers(true).OfType<ShapeFileFeatureLayer>())
            {
                shareObject.LayersReadyToSplit.Add(layer);
            }

            shareObject.SelectedLayerToSplit = shareObject.LayersReadyToSplit.FirstOrDefault();
            TargetObject = shareObject;

            Add(new SplitWizardChooseLayerStep());
            Add(new ChooseColumnStep());
            Add(new SplitWizardChooseOutputStep());
            Add(new SplitWizardSaveResultsStep());

            MoveNext();
        }
    }
}