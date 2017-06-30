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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThinkGeo.MapSuite.GisEditor
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    [InheritedExport(typeof(TaskPluginManager))]
    public partial class TaskPluginManager : PluginManager, ITaskCallbackReceiver
    {
        private readonly static string longRunningTaskAddressBase = "net.pipe://localhost/LongRunningTask{0}";
        private readonly static string callBackReceiverAddress = "net.pipe://localhost/CallBackReceiver";
        private readonly static string argumentFormatString = "{0} {1} {2} {3}";
        private static bool isServiceLaunched;
        private bool runProcessesLocally = false;

        private static event EventHandler<UpdatingTaskProgressEventArgs> UpdatingProgressInternal;

        static TaskPluginManager()
        {
            callBackReceiverAddress += String.Format(CultureInfo.InvariantCulture, "_{0}", Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskPluginManager" /> class.
        /// </summary>
        public TaskPluginManager()
        {
            inProcessTaskInformations = new Collection<InProcessTaskInfo>();
        }

        /// <summary>
        /// Occurs when [updating progress].
        /// </summary>
        public event EventHandler<UpdatingTaskProgressEventArgs> UpdatingProgress
        {
            add { UpdatingProgressInternal += value; }
            remove { UpdatingProgressInternal -= value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [run processes locally].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [run processes locally]; otherwise, <c>false</c>.
        /// </value>
        public bool RunProcessesLocally
        {
            get { return runProcessesLocally; }
            set { runProcessesLocally = value; }
        }

        /// <summary>
        /// Gets the plugins core.
        /// </summary>
        /// <returns></returns>
        protected override Collection<Plugin> GetPluginsCore()
        {
            return CollectPlugins<TaskPlugin>();
        }

        public Collection<TaskPlugin> GetTaskPlugins()
        {
            return new Collection<TaskPlugin>(GetPlugins().Cast<TaskPlugin>().ToList());
        }

        public Collection<T> GetActiveTaskPlugins<T>() where T : TaskPlugin
        {
            return new Collection<T>(GetActiveTaskPlugins().OfType<T>().ToList());
        }

        public Collection<TaskPlugin> GetActiveTaskPlugins()
        {
            var activePlugins = from p in GetTaskPlugins()
                                where p.IsActive
                                orderby p.Index
                                select p;

            return new Collection<TaskPlugin>(activePlugins.ToList());
        }

        /// <summary>
        /// Raises the <see cref="E:UpdatingProgress" /> event.
        /// </summary>
        /// <param name="e">The <see cref="UpdatingTaskProgressEventArgs" /> instance containing the event data.</param>
        protected void OnUpdatingProgress(UpdatingTaskProgressEventArgs e)
        {
            EventHandler<UpdatingTaskProgressEventArgs> handler = UpdatingProgressInternal;
            if (handler != null)
            {
                handler(null, e);
            }
        }

        /// <summary>
        /// Runs the task.
        /// </summary>
        /// <param name="taskPlugin">The task plugin.</param>
        /// <returns></returns>
        public string RunTask(TaskPlugin taskPlugin)
        {
            StartCallBackReceiverService();
            return RunTaskCore(taskPlugin);
        }

        /// <summary>
        /// Runs the task core.
        /// </summary>
        /// <param name="taskPlugin">The task plugin.</param>
        /// <returns></returns>
        protected virtual string RunTaskCore(TaskPlugin taskPlugin)
        {
            string currentTaskToken = Guid.NewGuid().ToString();
            if (taskPlugin != null)
            {
                taskPlugin.Load();
                RaiseFirstUpdatingProgress(taskPlugin, currentTaskToken);

                if (RunProcessesLocally)
                {
                    RunTaskInProcess(taskPlugin, currentTaskToken);
                }
                else
                {
                    RunTaskOutProcess(taskPlugin, currentTaskToken);
                }
            }

            return currentTaskToken;
        }

        /// <summary>
        /// Updates the task.
        /// </summary>
        /// <param name="taskToken">The task token.</param>
        /// <param name="taskCommand">The task command.</param>
        public void UpdateTask(string taskToken, TaskCommand taskCommand)
        {
            StartCallBackReceiverService();
            UpdateTaskCore(taskToken, taskCommand);
        }

        /// <summary>
        /// Updates the task core.
        /// </summary>
        /// <param name="taskToken">The task token.</param>
        /// <param name="taskCommand">The task command.</param>
        protected virtual void UpdateTaskCore(string taskToken, TaskCommand taskCommand)
        {
            UpdateTaskInternal(taskToken, taskCommand);
        }

        /// <summary>
        /// Updates the progress core.
        /// </summary>
        /// <param name="e">The <see cref="UpdatingTaskProgressEventArgs" /> instance containing the event data.</param>
        protected virtual void UpdateProgressCore(UpdatingTaskProgressEventArgs e)
        {
            OnUpdatingProgress(e);
        }

        private void UpdateTaskInternal(string taskToken, TaskCommand taskCommand)
        {
            if (RunProcessesLocally)
            {
                UpdateTaskInProcess(taskToken, taskCommand);
            }
            else
            {
                UpdateTaskOutProcess(taskToken, taskCommand);
            }
        }

        void ITaskCallbackReceiver.UpdateProgress(UpdatingTaskProgressEventArgs e)
        {
            UpdateProgressCore(e);
        }

        private static void UpdateTaskOutProcess(string taskToken, TaskCommand taskCommand)
        {
            EndpointAddress endpointAddress = new EndpointAddress(string.Format(CultureInfo.InvariantCulture, longRunningTaskAddressBase, taskToken));
            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            ITaskHost host = ChannelFactory<ITaskHost>.CreateChannel(binding, endpointAddress);
            IContextChannel channel = (IContextChannel)host;
            channel.OperationTimeout = TimeSpan.MaxValue;
            try
            {
                host.UpdateTask(taskCommand);
            }
            catch (Exception e)
            {
                GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
            }
        }

        private void StartCallBackReceiverService()
        {
            if (!runProcessesLocally && !isServiceLaunched)
            {
                ServiceHost host = new ServiceHost(typeof(TaskPluginManager));
                host.AddServiceEndpoint(typeof(ITaskCallbackReceiver), new NetNamedPipeBinding(), callBackReceiverAddress);
                host.Open();
                isServiceLaunched = true;
            }
        }

        private void RaiseFirstUpdatingProgress(TaskPlugin plugin, string currentTaskToken)
        {
            UpdatingTaskProgressEventArgs e = new UpdatingTaskProgressEventArgs(TaskState.Updating);
            e.TaskToken = currentTaskToken;
            e.Parameters.Add("Name", plugin.Name);
            e.Parameters.Add("Description", plugin.Description);
            OnUpdatingProgress(e);
        }

        private void RunTaskOutProcess(TaskPlugin plugin, string taskToken)
        {
            string waitHandleId = Guid.NewGuid().ToString();
            EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, waitHandleId);
            Task.Factory.StartNew(() =>
            {
                string address = string.Format(longRunningTaskAddressBase, taskToken);
#if GISEditorUnitTest
                string exePath = Assembly.GetExecutingAssembly().Location;
#else
                string exePath = Assembly.GetEntryAssembly().Location;
#endif
                string exeDirectory = Path.GetDirectoryName(exePath);

                ProcessStartInfo info = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    Arguments = string.Format(CultureInfo.InvariantCulture, argumentFormatString, waitHandleId, address, callBackReceiverAddress, taskToken),
                    WorkingDirectory = exeDirectory,
                    FileName = "GisEditorTaskHost"
                };

                Process hostProcess = Process.Start(info);
                waitHandle.WaitOne();

                EndpointAddress endpointAddress = new EndpointAddress(string.Format(CultureInfo.InvariantCulture, longRunningTaskAddressBase, taskToken));
                NetNamedPipeBinding binding = new NetNamedPipeBinding();
                binding.ReaderQuotas.MaxBytesPerRead = int.MaxValue;
                binding.ReaderQuotas.MaxDepth = int.MaxValue;
                binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
                binding.ReaderQuotas.MaxArrayLength = int.MaxValue;
                binding.ReceiveTimeout = TimeSpan.MaxValue;
                binding.MaxReceivedMessageSize = int.MaxValue;
                ITaskHost host = ChannelFactory<ITaskHost>.CreateChannel(binding, endpointAddress);
                IContextChannel channel = (IContextChannel)host;
                channel.OperationTimeout = TimeSpan.MaxValue;
                try
                {
                    string tempParameterPathFileName = Path.Combine(GisEditor.InfrastructureManager.TemporaryPath, "TaskParameters", taskToken + ".setting");
                    string tempParameterPathName = Path.GetDirectoryName(tempParameterPathFileName);
                    if (File.Exists(tempParameterPathFileName))
                    {
                        File.Delete(tempParameterPathFileName);
                    }
                    else if (!Directory.Exists(tempParameterPathName))
                    {
                        Directory.CreateDirectory(tempParameterPathName);
                    }

                    using (var pluginStream = new MemoryStream())
                    {
                        BinaryFormatter serializer = new BinaryFormatter();
                        serializer.Serialize(pluginStream, plugin);
                        File.WriteAllBytes(tempParameterPathFileName, pluginStream.ToArray());
                    }

                    if (File.Exists(tempParameterPathFileName))
                    {
                        host.RunTask(Encoding.UTF8.GetBytes(tempParameterPathFileName));
                    }
                }
                catch (Exception e)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                }
            });
        }
    }
}