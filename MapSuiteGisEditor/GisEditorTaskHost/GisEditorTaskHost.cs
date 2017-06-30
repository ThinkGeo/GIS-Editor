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
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;

namespace ThinkGeo.MapSuite.GisEditor
{
    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.Single,
        ConcurrencyMode = ConcurrencyMode.Reentrant,
        IncludeExceptionDetailInFaults = true)]
    public class GisEditorTaskHost : ITaskHost
    {
        private static string taskToken;
        private static ITaskCallbackReceiver receiver;
        private TaskCommand currentTaskCommand;
        private Thread taskThread;
        private TaskPlugin executingTask;

        static GisEditorTaskHost()
        {
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
        }

        public GisEditorTaskHost()
        {
        }

        public void RunTask(byte[] pluginBuffer)
        {
            if (pluginBuffer != null)
            {
                TraceMessage("Parsing parameters.");
                string tempParameterPathFileName = Encoding.UTF8.GetString(pluginBuffer);
                if (File.Exists(tempParameterPathFileName))
                {
                    try
                    {
                        TaskPlugin plugin = null;

                        BinaryFormatter serializer = new BinaryFormatter();
                        using (var pluginStream = File.OpenRead(tempParameterPathFileName))
                        {
                            plugin = serializer.Deserialize(pluginStream) as TaskPlugin;
                            TraceMessage("Looking for task.");
                            if (plugin != null)
                            {
                                TraceMessage("Task found and ready to start.");
                                RunTaskCore(plugin);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TraceException(ex);
                    }
                    finally
                    {
                        File.Delete(tempParameterPathFileName);
                    }
                }
            }
        }

        protected virtual void RunTaskCore(TaskPlugin plugin)
        {
            executingTask = plugin;
            executingTask.UpdatingProgress += new EventHandler<UpdatingTaskProgressEventArgs>(task_UpdatingProgress);
            Task.Factory.StartNew(() =>
            {
                taskThread = Thread.CurrentThread;
                try
                {
                    executingTask.Run();
                }
                catch (Exception ex)
                {
                    TraceException(ex);
                }
                finally
                {
                    TraceMessage("Task Exiting.");
                    AppDomain.CurrentDomain.AssemblyResolve -= new ResolveEventHandler(CurrentDomain_AssemblyResolve);
                    Environment.Exit(0);
                }
            });
        }

        public void UpdateTask(TaskCommand taskCommand)
        {
            if (executingTask != null)
            {
                UpdateTaskCore(taskCommand);
            }
        }

        protected virtual void UpdateTaskCore(TaskCommand taskCommand)
        {
            currentTaskCommand = taskCommand;
            if (taskCommand == TaskCommand.Cancel)
            {
                double timeToWait = 0;
                double tempTimeToWait = 0;
                if (double.TryParse(ConfigurationManager.AppSettings["TimeToWaitBeforeKillingProcess"], out tempTimeToWait))
                {
                    timeToWait = tempTimeToWait;
                }

                if (taskThread.IsAlive)
                {
                    System.Timers.Timer timer = new System.Timers.Timer(timeToWait * 1000);
                    timer.Start();
                    timer.Elapsed += (object sender, ElapsedEventArgs e) =>
                    {
                        timer.Stop();
                        if (taskThread.IsAlive)
                        {
                            Environment.Exit(0);
                        }
                    };
                }
            }
        }

        private void task_UpdatingProgress(object sender, UpdatingTaskProgressEventArgs e)
        {
            e.TaskToken = taskToken;
            while (currentTaskCommand == TaskCommand.Pause)
            {
                TryUpdateProgress(e);
                Thread.Sleep(500);
            }

            TryUpdateProgress(e);
            if (currentTaskCommand == TaskCommand.Cancel)
            {
                e.TaskState = TaskState.Canceled;
            }
        }

        internal static void Initialize(string callBackReceiverAddress, string currentTaskToken)
        {
            EndpointAddress endpointAddress = new EndpointAddress(callBackReceiverAddress);
            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            receiver = ChannelFactory<ITaskCallbackReceiver>.CreateChannel(binding, endpointAddress);
            taskToken = currentTaskToken;
        }

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyName = args.Name.Split(',').FirstOrDefault().Trim() + ".dll";
            string directoryRoot = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] pluginPathFileNames = Directory.GetFiles(Path.Combine(directoryRoot, "Plugins"), "*.dll", SearchOption.AllDirectories);

            foreach (string assemblyPath in pluginPathFileNames)
            {
                if (Path.GetFileName(assemblyPath).Equals(assemblyName, StringComparison.OrdinalIgnoreCase))
                {
                    Assembly assembly = Assembly.LoadFile(assemblyPath);
                    return assembly;
                }
            }

            return null;
        }

        private static void TryUpdateProgress(UpdatingTaskProgressEventArgs e)
        {
            try
            {
                if (receiver != null) receiver.UpdateProgress(e);
            }
            catch
            {
                receiver = null;
            }
        }

        private static void TraceMessage(string message)
        {
            Trace.WriteLine("TaskHost: " + message);
        }

        private static void TraceException(Exception ex)
        {
            TraceMessage(ex.Message);
            if (ex.InnerException != null)
            {
                TraceMessage(ex.InnerException.Message);
            }
        }
    }
}