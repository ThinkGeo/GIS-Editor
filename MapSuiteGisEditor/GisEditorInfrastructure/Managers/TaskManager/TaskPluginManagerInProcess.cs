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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    public partial class TaskPluginManager : PluginManager
    {
        private Collection<InProcessTaskInfo> inProcessTaskInformations;

        private void UpdateTaskInProcess(string taskToken, TaskCommand taskCommand)
        {
            var inProcessTaskInformation = inProcessTaskInformations.FirstOrDefault(p => p.TaskToken.Equals(taskToken, StringComparison.Ordinal));
            if (inProcessTaskInformation != null)
            {
                inProcessTaskInformation.TaskCommand = taskCommand;
            }
        }

        private void RunTaskInProcess(TaskPlugin plugin, string taskToken)
        {
            plugin.Load();
            RaiseFirstUpdatingProgress(plugin, taskToken);

            TaskPlugin matchingTaskClone = null;
            using (MemoryStream pluginStream = new MemoryStream())
            {
                BinaryFormatter serializer = new BinaryFormatter();
                serializer.Serialize(pluginStream, plugin);
                pluginStream.Seek(0, SeekOrigin.Begin);
                matchingTaskClone = serializer.Deserialize(pluginStream) as TaskPlugin;
            }

            matchingTaskClone.UpdatingProgress += new EventHandler<UpdatingTaskProgressEventArgs>(Task_UpdatingProgress);
            inProcessTaskInformations.Add(new InProcessTaskInfo(matchingTaskClone, taskToken, TaskCommand.Resume));
            Task.Factory.StartNew(() =>
            {
                matchingTaskClone.Run();
            });
        }

        private void Task_UpdatingProgress(object sender, UpdatingTaskProgressEventArgs e)
        {
            var currentTaskPlugin = (TaskPlugin)sender;
            var inProcessTaskInformation = inProcessTaskInformations.FirstOrDefault(p => p.TaskPlugin.Equals(currentTaskPlugin));
            if (inProcessTaskInformation != null)
            {
                if (inProcessTaskInformation.TaskCommand == TaskCommand.Cancel)
                {
                    e.TaskState = TaskState.Canceled;
                }
                e.TaskToken = inProcessTaskInformation.TaskToken;
            }

            UpdateProgressCore(e);
        }

        private class InProcessTaskInfo
        {
            public InProcessTaskInfo(TaskPlugin taskPlugin, string taskToken, TaskCommand taskCommand)
            {
                TaskCommand = taskCommand;
                TaskToken = taskToken;
                TaskPlugin = taskPlugin;
            }

            public TaskCommand TaskCommand { get; set; }

            public TaskPlugin TaskPlugin { get; set; }

            public string TaskToken { get; set; }
        }
    }
}