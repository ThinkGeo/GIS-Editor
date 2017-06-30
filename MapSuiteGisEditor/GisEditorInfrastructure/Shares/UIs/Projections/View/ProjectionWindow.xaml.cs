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
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using GalaSoft.MvvmLight.Messaging;
using System.Globalization;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ProjectionWindow : Window
    {
        private string proj4Parameters;
        private ProjectionSelectionViewModel viewModel;
        private CommonProjectionViewModel commonProjectionViewModel;
        private OtherProjectionViewModel otherProjectionViewModel;

        public ProjectionWindow()
            : this("", "", "")
        { }

        public ProjectionWindow(string initializeProj4String)
            : this(initializeProj4String, "", "")
        { }

        public ProjectionWindow(string initializeProj4String, string descriptionText, string checkBoxContent)
        {
            proj4Parameters = initializeProj4String;

            InitializeComponent();
            commonProjectionViewModel = commonProjection.DataContext as CommonProjectionViewModel;
            otherProjectionViewModel = otherProjection.DataContext as OtherProjectionViewModel;
            commonProjectionViewModel.SelectedProj4ProjectionParameters = initializeProj4String;

            viewModel = new ProjectionSelectionViewModel(descriptionText, checkBoxContent);
            DataContext = viewModel;

            Messenger.Default.Register<bool>(this, viewModel, (result) =>
            {
                if (result)
                {
                    string proj4ProjectionParameter = viewModel.SelectedProj4Parameter;
                    var selectedOtherProjectionViewModel = viewModel.SelectedViewModel as OtherProjectionViewModel;
                    if (selectedOtherProjectionViewModel != null && selectedOtherProjectionViewModel.SelectedProjectionType == SearchProjectionType.Custom)
                    {
                        proj4ProjectionParameter = viewModel.SelectedProj4Parameter;
                        string projectionWkt = Proj4Projection.ConvertProj4ToPrj(proj4ProjectionParameter);
                        if (!string.IsNullOrEmpty(projectionWkt))
                        {
                            DialogResult = result;
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(GisEditor.LanguageManager.GetStringResource("ProjectionSelectionWindowProj4InvalidText"), GisEditor.LanguageManager.GetStringResource("ProjectionSelectionWindowProj4InvalidCaption"), System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                        }
                    }
                    else DialogResult = result;

                    SaveProjectionWindowStateToXml();
                    if (!string.IsNullOrEmpty(proj4ProjectionParameter))
                    {
                        viewModel.SelectedProj4Parameter = proj4ProjectionParameter;
                    }
                }
                else DialogResult = result;
            });
            Closing += (s, e) => Messenger.Default.Unregister(this);
            HelpContainer.Content = GetHelpButton();
        }

        public string Proj4ProjectionParameters
        {
            get { return viewModel.SelectedProj4Parameter; }
        }

        public bool SyncProj4ProjectionForAll
        {
            get { return viewModel.ApplyForAll; }
        }

        private static string ProjectionWindowStatePathFileName
        {
            get
            {
                return Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "ProjectionWindowState.xml");
            }
        }

        internal static ProjectionsState GetProjectionsState(string projectionParameters)
        {
            if (File.Exists(ProjectionWindowStatePathFileName))
            {
                XElement rootX = XElement.Load(ProjectionWindowStatePathFileName);
                XElement stateX = rootX.Element("state");
                if (stateX != null && stateX.Element("key").Value.Equals(projectionParameters))
                {
                    XElement projectionX = stateX.Element("projectionsstate");
                }
            }

            return null;
        }

        [Obfuscation]
        private void TabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var tabControl = sender as TabControl;
            if (tabControl != null && tabControl.SelectedItem != null)
            {
                viewModel.SelectedViewModel = ((tabControl.SelectedItem as TabItem).Content as FrameworkElement).DataContext as IProjectionViewModel;
            }
        }

        [Obfuscation]
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProjectionsState state = GetStateByProj4ProjectionParameters(proj4Parameters);
            if (state is CommonProjectionsState)
            {
                tabControl.SelectedIndex = 0;
                CommonProjectionsState commonProjectionsState = (CommonProjectionsState)state;
                commonProjectionViewModel.SelectedUnitType = commonProjectionsState.SelectedUnit;
                commonProjectionViewModel.SelectedProj4ProjectionParameters = commonProjectionsState.ProjectionParameters;
                commonProjectionViewModel.SelectedProjectionType = commonProjectionsState.SelectedProjection;
                commonProjectionViewModel.SelectedDatumType = commonProjectionsState.SelectedDatum;
                commonProjectionViewModel.SelectedZone = commonProjectionViewModel.SupportedZones[commonProjectionsState.SelectedZoneIndex];
            }
            else if (state is CustomProjectionsState)
            {
                tabControl.SelectedIndex = 1;
                CustomProjectionsState customProjectionsState = (CustomProjectionsState)state;
                otherProjectionViewModel.SelectedProjectionType = customProjectionsState.SelectedSearchProjectionType;
                if (customProjectionsState.SelectedProj4ModelIndex > 0)
                {
                    otherProjectionViewModel.SelectedProj4Model = otherProjectionViewModel.SearchedResult[customProjectionsState.SelectedProj4ModelIndex];
                }
                if (otherProjectionViewModel.SelectedProj4Model != null)
                {
                    otherProjectionViewModel.SelectedProj4Model.Proj4Parameter = customProjectionsState.ProjectionParameters;
                    Proj4ParameterTextBox.Text = customProjectionsState.ProjectionParameters;
                    otherProjection.SearchedResultListView.ScrollIntoView(otherProjectionViewModel.SelectedProj4Model);
                }
            }
        }

        private static ProjectionsState GetStateByProj4ProjectionParameters(string proj4ProjectionParameters)
        {
            string xmlPathFileName = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "Projections", "State.xml");
            if (File.Exists(xmlPathFileName))
            {
                XElement rootX = XElement.Load(xmlPathFileName);
                IEnumerable<XElement> statesX = rootX.Elements("state");
                foreach (var item in statesX)
                {
                    if (item.FirstAttribute.Value.Equals(proj4ProjectionParameters))
                    {
                        XElement commonProjectionsStateX = item.Element("common");
                        XElement customProjectionsStateX = item.Element("custom");
                        if (commonProjectionsStateX != null)
                        {
                            string parameters = item.FirstAttribute.Value;
                            string value = commonProjectionsStateX.Value;
                            string[] values = value.Split(',');
                            string projection = values[0];
                            string datum = values[1];
                            string zone = values[2];
                            string unit = values[3];
                            CommonProjectionsState state = new CommonProjectionsState(parameters);
                            state.SelectedDatum = (DatumType)Enum.Parse(typeof(DatumType), datum);
                            state.SelectedProjection = (ProjectionType)Enum.Parse(typeof(ProjectionType), projection);
                            state.SelectedUnit = (UnitType)Enum.Parse(typeof(UnitType), unit);
                            state.SelectedZoneIndex = int.Parse(zone);
                            return state;
                        }
                        else if (customProjectionsStateX != null)
                        {
                            string parameters = item.FirstAttribute.Value;
                            string value = customProjectionsStateX.Value;
                            string[] values = value.Split(',');
                            string searchProjectionType = values[0];
                            string proj4Model = values[1];
                            CustomProjectionsState state = new CustomProjectionsState(parameters);
                            state.SelectedSearchProjectionType = (SearchProjectionType)int.Parse(searchProjectionType);
                            state.SelectedProj4ModelIndex = int.Parse(proj4Model);
                            return state;
                        }
                    }
                }
            }
            return null;
        }

        private void SaveProjectionWindowStateToXml()
        {
            XElement projectionWindowStateX = new XElement("ProjectionState");
            XElement stateX = GetStateXElement();

            string folderName = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "Projections");
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            string xmlPathFileName = Path.Combine(folderName, "State.xml");
            if (File.Exists(xmlPathFileName))
            {
                XElement rootX = XElement.Load(xmlPathFileName);
                IEnumerable<XElement> statesX = rootX.Elements("state");
                foreach (var item in statesX)
                {
                    if (!item.FirstAttribute.Value.Equals(stateX.FirstAttribute.Value))
                    {
                        projectionWindowStateX.Add(item);
                    }
                }
            }
            projectionWindowStateX.Add(stateX);
            projectionWindowStateX.Save(xmlPathFileName);
        }

        private XElement GetStateXElement()
        {
            XElement stateX = new XElement("state");
            stateX.SetAttributeValue("key", Proj4ProjectionParameters);
            if (viewModel.SelectedViewModel is CommonProjectionViewModel)
            {
                CommonProjectionViewModel commonProjectionViewModel = (CommonProjectionViewModel)viewModel.SelectedViewModel;
                DatumType datumType = commonProjectionViewModel.SelectedDatumType;
                string selectedZone = commonProjectionViewModel.SelectedZone;
                UnitType unitType = commonProjectionViewModel.SelectedUnitType;
                ProjectionType projectionType = commonProjectionViewModel.SelectedProjectionType;
                //int index1 = commonProjectionViewModel.SupportedDatumTypes.IndexOf(datumType);
                //int index2 = commonProjectionViewModel.SupportedUnits.IndexOf(commonProjectionViewModel.SelectedUnitType);
                //int index3 = (int)commonProjectionViewModel.SelectedProjectionType;
                commonProjectionViewModel.SelectedDatumType = datumType;
                int index4 = commonProjectionViewModel.SupportedZones.IndexOf(selectedZone);
                string value = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", projectionType, datumType, index4, unitType);
                XElement commonProjectionsStateX = new XElement("common", value);
                stateX.Add(commonProjectionsStateX);
            }
            else if (viewModel.SelectedViewModel is OtherProjectionViewModel)
            {
                OtherProjectionViewModel otherProjectionViewModel = (OtherProjectionViewModel)viewModel.SelectedViewModel;
                int index1 = (int)otherProjectionViewModel.SelectedProjectionType;
                int index2 = otherProjectionViewModel.SearchedResult.IndexOf(otherProjectionViewModel.SelectedProj4Model);
                string value = string.Format(CultureInfo.InvariantCulture, "{0},{1}", index1, index2);
                XElement customProjectionsStateX = new XElement("custom", value);
                stateX.Add(customProjectionsStateX);
            }

            return stateX;
        }

        #region help button

        private Button GetHelpButton()
        {
            string helpUri = GisEditor.LanguageManager.GetStringResource("MapProjectionHelp");
            string helpContentKey = "HelpHeader";
            Button button = new Button { Content = GetButtonContainer(helpUri, helpContentKey, new Uri("pack://application:,,,/GisEditorInfrastructure;component/Images/help.png", UriKind.Absolute)) };
            button.SetResourceReference(FrameworkElement.ToolTipProperty, helpContentKey);
            button.Tag = helpUri;
            button.Click += NavigateToHelpUri_Click;
            return button;
        }

        private static void NavigateToHelpUri_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).Tag is string)
            {
                Process.Start(((sender as Button).Tag).ToString());
            }
        }

        private static StackPanel GetButtonContainer(string helpUri, string helpContentKey, Uri iconSource)
        {
            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;
            stackPanel.Children.Add(GetImageButton(iconSource));
            TextBlock textBlock = new TextBlock() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, -1, 0, 0) };
            textBlock.SetResourceReference(TextBlock.TextProperty, helpContentKey);
            stackPanel.Children.Add(textBlock);
            return stackPanel;
        }

        private static Image GetImageButton(Uri iconSource)
        {
            return new Image { Source = new BitmapImage(iconSource), Width = 16, Height = 16 };
        }

        #endregion help button
    }
}