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
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    internal class ManagedProj4ProjectionInfo : Proj4ProjectionInfo
    {
        private Proj4Projection managedProj4Projection;

        public ManagedProj4ProjectionInfo(Proj4Projection managedProj4Projection)
            : base(managedProj4Projection)
        {
            this.managedProj4Projection = managedProj4Projection;
        }

        public Proj4Projection ManagedProj4Projection
        {
            get { return managedProj4Projection; }
        }

        public override string InternalProjectionParametersString
        {
            get
            {
                return managedProj4Projection.InternalProjectionParametersString;
            }
            set
            {
                managedProj4Projection.InternalProjectionParametersString = value;
            }
        }

        public override string ExternalProjectionParametersString
        {
            get
            {
                return managedProj4Projection.ExternalProjectionParametersString;
            }
            set
            {
                managedProj4Projection.ExternalProjectionParametersString = value;
            }
        }

        public override bool CanProject
        {
            get { return managedProj4Projection.CanProject(); }
        }

        public override void SyncProjectionParametersString()
        {
            string internalParameter = managedProj4Projection.InternalProjectionParametersString;
            string externalParameter = managedProj4Projection.ExternalProjectionParametersString;
            if (!ManagedProj4ProjectionExtension.CanProject(internalParameter, externalParameter) && !String.IsNullOrEmpty(internalParameter) && !String.IsNullOrEmpty(externalParameter))
            {
                managedProj4Projection.InternalProjectionParametersString = managedProj4Projection.ExternalProjectionParametersString;
            }
        }
    }
}