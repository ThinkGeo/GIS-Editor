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


using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.WpfDesktop.Extension
{
    public abstract class Proj4ProjectionInfo
    {
        private Projection projection;

        protected Proj4ProjectionInfo(Projection projection)
        {
            this.projection = projection;
        }

        public static Proj4ProjectionInfo CreateInstance(Projection projection)
        {
            Proj4Projection unManagedProjection = projection as Proj4Projection;
            Proj4Projection managedProjection = projection as Proj4Projection;
            if (unManagedProjection != null)
            {
                return new UnManagedProj4ProjectionInfo(unManagedProjection);
            }
            else if (managedProjection != null)
            {
                return new ManagedProj4ProjectionInfo(managedProjection);
            }
            else return null;
        }

        public Projection Projection
        {
            get { return projection; }
        }

        public bool IsOpen
        {
            get { return projection.IsOpen; }
        }

        public abstract string InternalProjectionParametersString
        {
            get;
            set;
        }

        public abstract string ExternalProjectionParametersString
        {
            get;
            set;
        }

        public abstract bool CanProject
        {
            get;
        }

        public abstract void SyncProjectionParametersString();

        public Feature ConvertToExternalProjection(Feature sourceFeature)
        {
            return projection.ConvertToExternalProjection(sourceFeature);
        }

        public Feature ConvertToInternalProjection(Feature sourceFeature)
        {
            return projection.ConvertToInternalProjection(sourceFeature);
        }

        public GeographyUnit GetInternalGeographyUnit()
        {
            return projection.GetInternalGeographyUnit();
        }

        public void Open()
        {
            projection.Open();
        }

        public void Close()
        {
            projection.Close();
        }
    }
}