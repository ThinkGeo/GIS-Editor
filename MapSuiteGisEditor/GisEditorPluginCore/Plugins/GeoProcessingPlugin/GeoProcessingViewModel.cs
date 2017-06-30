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
using System.Windows;
using GalaSoft.MvvmLight.Command;
using System.Collections.ObjectModel;
using System.Linq;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class GeoProcessingViewModel
    {
        private static GeoProcessingViewModel instance;

        private Dictionary<string, Action<ExceptionInfo>> runningTasks;
        private Dictionary<string, Collection<ExceptionInfo>> runningErrors;

        private ObservedCommand clipWizardCommand;
        private ObservedCommand gridWizardCommand;
        private ObservedCommand blendWizardCommand;
        private ObservedCommand mergeWizardCommand;
        private ObservedCommand splitWizardCommand;
        private ObservedCommand bufferWizardCommand;
        private ObservedCommand explodeWizardCommand;
        private ObservedCommand simplifyWizardCommand;
        private ObservedCommand dissolveWizardCommand;
        private ObservedCommand dataJoinWizardCommand;
        private RelayCommand rebuildIndexWizardCommand;
        private ObservedCommand exportSelectedFeaturesCommand;
        private ObservedCommand exportMeasuredFeaturesCommand;
        private ObservedCommand reprojectionCommand;

        protected GeoProcessingViewModel()
        {
            runningTasks = new Dictionary<string, Action<ExceptionInfo>>();
            runningErrors = new Dictionary<string, Collection<ExceptionInfo>>();
            gridWizardCommand = GetCommand<GridWizardWindow>();
            explodeWizardCommand = GetCommand<ExplodeWizardWindow>();
            blendWizardCommand = GetCommand<BlendWizardWindow>();
            mergeWizardCommand = GetCommand<MergeWizardWindow>();
            splitWizardCommand = GetCommand<SplitWizardWindow>();
            bufferWizardCommand = GetCommand<BufferWizardWindow>();
            clipWizardCommand = GetCommand<ClippingWizardWindow>();
            dissolveWizardCommand = GetCommand<DissolveWizardWindow>();
            simplifyWizardCommand = GetCommand<SimplifyWizardWindow>();
            dataJoinWizardCommand = GetCommand<DataJoinWizardWindow>();
            exportSelectedFeaturesCommand = GetExportSelectedFeaturesCommand();
            exportMeasuredFeaturesCommand = GetExportMeasuredFeaturesCommand();
            reprojectionCommand = GetCommand<ReprojectionWindow>();
            rebuildIndexWizardCommand = new RelayCommand(() =>
            {
                BuildIndexWizardWindow window = new BuildIndexWizardWindow();
                window.Show();
            });
        }

        public static GeoProcessingViewModel Instance
        {
            get
            {
                if (instance == null) instance = new GeoProcessingViewModel();
                return instance;
            }
        }

        public ObservedCommand GridWizardCommand { get { return gridWizardCommand; } }

        public ObservedCommand ExplodeWizardCommand { get { return explodeWizardCommand; } }

        public ObservedCommand ClipWizardCommand { get { return clipWizardCommand; } }

        public ObservedCommand BlendWizardCommand { get { return blendWizardCommand; } }

        public ObservedCommand MergeWizardCommand { get { return mergeWizardCommand; } }

        public ObservedCommand SplitWizardCommand { get { return splitWizardCommand; } }

        public ObservedCommand BufferWizardCommand { get { return bufferWizardCommand; } }

        public ObservedCommand DissolveWizardCommand { get { return dissolveWizardCommand; } }

        public ObservedCommand SimplifyWizardCommand { get { return simplifyWizardCommand; } }

        public ObservedCommand DataJoinWizardCommand { get { return dataJoinWizardCommand; } }

        public RelayCommand RebuildIndexWizardCommand { get { return rebuildIndexWizardCommand; } }

        public ObservedCommand ExportMeasuredFeaturesCommand { get { return exportMeasuredFeaturesCommand; } }

        public ObservedCommand ExportSelectedFeaturesCommand { get { return exportSelectedFeaturesCommand; } }

        public ObservedCommand ReprojectionCommand
        {
            get { return reprojectionCommand; }
        }

        public ObservedCommand GetCommand<T>() where T : Window, IGeoProcessWizard, new()
        {
            return new ObservedCommand(() =>
            {
                T window = new T();
                if (window.ShowDialog().Value)
                {
                    RunTask(window);
                }
            }, () => GisEditor.ActiveMap != null);
        }

        private ObservedCommand GetExportSelectedFeaturesCommand()
        {
            return new ObservedCommand(() =>
            {
                ExportWizardWindow window = new ExportWizardWindow(ExportMode.ExportSelectedFeatures, GisEditor.SelectionManager.GetSelectionOverlay());
                if (window.ShowDialog().Value)
                {
                    RunTask(window);
                }
            }, () => GisEditor.ActiveMap != null);
        }

        private ObservedCommand GetExportMeasuredFeaturesCommand()
        {
            return new ObservedCommand(() =>
            {
                MeasureTrackInteractiveOverlay measurementOverlay = null;
                if (GisEditor.ActiveMap != null && GisEditor.ActiveMap.InteractiveOverlays.Contains("MeasurementOverlay"))
                {
                    measurementOverlay = GisEditor.ActiveMap.InteractiveOverlays["MeasurementOverlay"] as MeasureTrackInteractiveOverlay;
                }

                if (measurementOverlay != null)
                {
                    ExportWizardWindow window = new ExportWizardWindow(ExportMode.ExportMeasuredFeatures, measurementOverlay);
                    if (window.ShowDialog().Value)
                    {
                        RunTask(window);
                    }
                }
            }, () => GisEditor.ActiveMap != null);
        }

        private void RunTask(IGeoProcessWizard window)
        {
            WizardShareObject shareObject = window.GetShareObject();
            TaskPlugin taskPlugin = shareObject.GetTaskPlugin();
            GisEditor.TaskManager.UpdatingProgress -= new EventHandler<UpdatingTaskProgressEventArgs>(TaskManager_UpdatingProgress);
            GisEditor.TaskManager.UpdatingProgress += new EventHandler<UpdatingTaskProgressEventArgs>(TaskManager_UpdatingProgress);
            var taskToken = GisEditor.TaskManager.RunTask(taskPlugin);
            runningTasks.Add(taskToken, shareObject.LoadToMap);
        }

        private void TaskManager_UpdatingProgress(object sender, UpdatingTaskProgressEventArgs e)
        {
            if (e.TaskState == TaskState.Completed && runningTasks.ContainsKey(e.TaskToken) && runningTasks[e.TaskToken] != null)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        FinalProcess(e);
                    });
                }
                else
                {
                    FinalProcess(e);
                }
            }
            else if (e.TaskState == TaskState.Canceled && runningTasks.ContainsKey(e.TaskToken))
            {
                if (e.Error != null && !string.IsNullOrEmpty(e.Error.Message))
                {
                    System.Windows.Forms.MessageBox.Show(e.Error.Message, "Task Canceled", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Stop);
                }
                runningTasks.Remove(e.TaskToken);
            }
            else if (e.TaskState == TaskState.Error)
            {
                if (!runningErrors.ContainsKey(e.TaskToken))
                {
                    runningErrors.Add(e.TaskToken, new Collection<ExceptionInfo> { e.Error });
                }
                else
                {
                    runningErrors[e.TaskToken].Add(e.Error);
                }
            }
        }

        private void FinalProcess(UpdatingTaskProgressEventArgs e)
        {
            ExceptionInfo exceptionInfo = new ExceptionInfo();
            if (runningErrors.ContainsKey(e.TaskToken))
            {
                exceptionInfo.Message = string.Join("\r\n", runningErrors[e.TaskToken].Select(s => s.Message));
                runningErrors.Remove(e.TaskToken);
            }

            runningTasks[e.TaskToken](exceptionInfo);
            runningTasks.Remove(e.TaskToken);
        }
    }
}