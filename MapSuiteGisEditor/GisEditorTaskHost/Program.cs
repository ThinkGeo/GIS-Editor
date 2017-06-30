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
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Timers;

namespace ThinkGeo.MapSuite.GisEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args != null && args.Length == 4)
            {
                Timer timer = new Timer(5000);
                timer.Elapsed += new ElapsedEventHandler(CheckIfGisEditorProcessExists);
                timer.Start();
                StartTaskHostService(args[0], args[1], args[2], args[3]);
                Console.ReadLine();
            }
        }

        private static void CheckIfGisEditorProcessExists(object sender, ElapsedEventArgs e)
        {
            if (!Process.GetProcesses().Any(p => p.ProcessName.Equals("MapSuiteGisEditor", StringComparison.Ordinal) 
                || p.ProcessName.Equals("MapSuiteGisEditor.vshost", StringComparison.Ordinal)))
            {
                Environment.Exit(-1);
            }
        }

        private static void StartTaskHostService(string waitHandleId, string serviceAddress, string callBackAddress, string taskToken)
        {
            System.Threading.EventWaitHandle eventWaitHandle = System.Threading.EventWaitHandle.OpenExisting(waitHandleId);

            GisEditorTaskHost.Initialize(callBackAddress, taskToken);

            ServiceHost host = new ServiceHost(typeof(GisEditorTaskHost));
            NetNamedPipeBinding binding = new NetNamedPipeBinding();
            binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
            binding.ReceiveTimeout = TimeSpan.MaxValue;
            binding.MaxReceivedMessageSize = int.MaxValue;
            host.AddServiceEndpoint(typeof(ITaskHost), binding, serviceAddress);
            host.Open();

            eventWaitHandle.Set();
        }
    }
}
