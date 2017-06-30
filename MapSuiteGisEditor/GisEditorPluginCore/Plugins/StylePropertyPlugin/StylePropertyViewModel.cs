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


using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.Layers;
using ThinkGeo.MapSuite.Styles;
using ThinkGeo.MapSuite.WpfDesktop.Extension;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class StylePropertyViewModel : ViewModelBase
    {
        private FeatureLayer featureLayer;
        private ObservedCommand applyCommand;
        private StyleUserControl stylePropertyContent;

        public StylePropertyViewModel()
            : base()
        {
            InitializeCommands();
        }

        public FeatureLayer FeatureLayer
        {
            get { return featureLayer; }
            set { featureLayer = value; }
        }

        public ObservedCommand ApplyCommand
        {
            get { return applyCommand; }
            set { applyCommand = value; }
        }

        public StyleUserControl StylePropertyContent
        {
            get { return stylePropertyContent; }
            set
            {
                stylePropertyContent = value;
                RaisePropertyChanged(() => StylePropertyContent);
            }
        }

        private void InitializeCommands()
        {
            applyCommand = new ObservedCommand(() =>
            {
                if (stylePropertyContent != null)
                {
                    StyleLayerListItem styleListItem = stylePropertyContent.StyleItem;
                    LayerListItem componentLayerListItem = StylePropertyUIPlugin.GetSpecificLayerListItem<CompositeStyle>(styleListItem);
                    if (componentLayerListItem != null)
                    {
                        CompositeStyle oldStyle = (CompositeStyle)componentLayerListItem.ConcreteObject;
                        List<Style> innerStyles = oldStyle.Styles.ToList();
                        oldStyle.Styles.Clear();

                        foreach (var innerStyle in componentLayerListItem.Children.OfType<StyleLayerListItem>()
                            .Select(item => item.ConcreteObject).OfType<Style>().Reverse())
                        {
                            oldStyle.Styles.Add(innerStyle);
                        }

                        if (featureLayer != null)
                        {
                            //GisEditor.ActiveMap.GetOverlaysContaining(featureLayer).ForEach(o => o.Refresh());
                            GisEditor.ActiveMap.GetOverlaysContaining(featureLayer).ForEach(o => o.RefreshWithBufferSettings());
                            LayerListUIPlugin layerListUIPlugin = GisEditor.UIManager.GetActiveUIPlugins<LayerListUIPlugin>().FirstOrDefault();
                            if (layerListUIPlugin != null)
                            {
                                layerListUIPlugin.Refresh(new RefreshArgs(this, "StyleProperties Applied."));
                            }
                        }
                    }
                }
            }, () => stylePropertyContent != null);
        }
    }
}