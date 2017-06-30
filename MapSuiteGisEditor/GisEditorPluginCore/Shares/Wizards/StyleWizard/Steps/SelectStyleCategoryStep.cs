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
using System.Collections.ObjectModel;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SelectStyleCategoryStep : WizardStep<StyleWizardSharedObject>
    {
        private static readonly string styleLibraryOptionName = "Style Library";

        [NonSerialized]
        private SelectStyleCategoryUserControl content;

        public SelectStyleCategoryStep()
        {
            Title = GisEditor.LanguageManager.GetStringResource("GeneralStepOne");
            Header = GisEditor.LanguageManager.GetStringResource("SelectStyleCategoryStepTitle");
            Description = GisEditor.LanguageManager.GetStringResource("SelectStyleCategoryStepDesc");
            content = new SelectStyleCategoryUserControl();
            Content = content;
        }

        protected override void EnterCore(StyleWizardSharedObject parameter)
        {
            base.EnterCore(parameter);
            InitStyleCategories(parameter);
            parameter.SelectedStyleCategory = parameter.StyleCategories[0];
            content.DataContext = parameter;
        }

        private void InitStyleCategories(StyleWizardSharedObject parameter)
        {
            parameter.StyleCategories.Clear();
            bool hasPoint = (parameter.TargetStyleCategories & StyleCategories.Point) != 0;
            bool hasLine = (parameter.TargetStyleCategories & StyleCategories.Line) != 0;
            bool hasArea = (parameter.TargetStyleCategories & StyleCategories.Area) != 0;

            string styleLibraryDescription = GisEditor.LanguageManager.GetStringResource("StyleLibraryDescription");
            LibraryStyleCheckableItemModel styleLibraryCheckableItemModel = new LibraryStyleCheckableItemModel(styleLibraryOptionName, styleLibraryDescription);
            parameter.StyleCategories.Add(styleLibraryCheckableItemModel);
            parameter.AllStyleSources[styleLibraryCheckableItemModel.Name] = new Collection<StyleCheckableItemModel>();

            if (hasPoint)
            {
                StyleCheckableItemModel pointStyleCheckableItemModel = new StyleCheckableItemModel();
                pointStyleCheckableItemModel.Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginPointHeader");
                pointStyleCheckableItemModel.Description = GisEditor.LanguageManager.GetStringResource("PointStyleDescription"); 
                //content.FindResource("PointStyleDescription") as string;

                parameter.StyleCategories.Add(pointStyleCheckableItemModel);
                parameter.AllStyleSources[pointStyleCheckableItemModel.Name] = new PointStylesModel();
            }

            if (hasLine)
            {
                StyleCheckableItemModel lineStyleCheckableItemModel = new StyleCheckableItemModel();
                lineStyleCheckableItemModel.Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginLineHeader");
                lineStyleCheckableItemModel.Description = GisEditor.LanguageManager.GetStringResource("LineStyleDescription"); 
                // content.FindResource("LineStyleDescription") as string;

                parameter.StyleCategories.Add(lineStyleCheckableItemModel);
                parameter.AllStyleSources[lineStyleCheckableItemModel.Name] = new LineStylesModel();
            }

            if (hasArea)
            {
                StyleCheckableItemModel areaStyleCheckableItemModel = new StyleCheckableItemModel();
                areaStyleCheckableItemModel.Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginAreaHeader");
                areaStyleCheckableItemModel.Description = GisEditor.LanguageManager.GetStringResource("AreaStyleDescription");
                // content.FindResource("AreaStyleDescription") as string;

                parameter.StyleCategories.Add(areaStyleCheckableItemModel);
                parameter.AllStyleSources[areaStyleCheckableItemModel.Name] = new AreaStylesModel();
            }

            parameter.StyleCategories.Add(new StyleCheckableItemModel
            {
                Name = GisEditor.LanguageManager.GetStringResource("MapElementsListPluginTextHeader"),
                Description = GisEditor.LanguageManager.GetStringResource("TextStyleDescription")
                //content.FindResource("TextStyleDescription") as string
            });
        }
    }
}
