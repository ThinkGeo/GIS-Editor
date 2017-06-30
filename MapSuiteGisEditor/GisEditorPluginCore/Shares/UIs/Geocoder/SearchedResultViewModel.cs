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
using GalaSoft.MvvmLight;
using ThinkGeo.MapSuite.GeocodeServerSdk;
using ThinkGeo.MapSuite.Shapes;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    [Serializable]
    public class SearchedResultViewModel : ViewModelBase
    {
        private string city;
        private string state;
        private string county;
        private string zipcode;
        private string address;
        private string internalProjection;
        private RectangleShape boundingBox;
        private PointShape centroidPoint;
        private string searchSegment;

        public SearchedResultViewModel()
        { }

        public string City
        {
            get { return city; }
            set
            {
                city = value;
                RaisePropertyChanged(() => City);
            }
        }

        public string State
        {
            get { return state; }
            set
            {
                state = value;
                RaisePropertyChanged(() => State);
            }
        }

        public string County
        {
            get { return county; }
            set
            {
                county = value;
                RaisePropertyChanged(() => County);
            }
        }

        public string Zipcode
        {
            get { return zipcode; }
            set
            {
                zipcode = value;
                RaisePropertyChanged(() => Zipcode);
            }
        }

        public string PlaceName
        {
            get
            {
                string placeName = AppendItem("", Address);
                //if (!city.Equals("Loading..."))
                {
                    placeName = AppendItem(placeName, City);
                }

                placeName = AppendItem(placeName, State);
                placeName = AppendItem(placeName, County);
                placeName = AppendItem(placeName, Zipcode);
                if (!string.IsNullOrEmpty(placeName)) placeName = placeName.TrimEnd(',');
                return placeName;
            }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(PlaceName); }
        }

        public RectangleShape BoundingBox
        {
            get
            {
                return boundingBox;
            }
            set { boundingBox = value; }
        }

        public PointShape CentroidPoint
        {
            get
            {
                return centroidPoint;
            }
        }

        public string Address
        {
            get { return address; }
            set
            {
                address = value;
                RaisePropertyChanged(() => Address);
            }
        }

        public string InternalProjection
        {
            get { return internalProjection; }
        }

        public string SearchSegment
        {
            get { return searchSegment; }
        }

        public void Load(GeocodeMatch match)
        {
            centroidPoint = null;
            boundingBox = null;

            if (match.MatchResults.Count > 0
                && (match.MatchResults.ContainsKey("City")
                || match.MatchResults.ContainsKey("State")
                || match.MatchResults.ContainsKey("County")
                || match.MatchResults.ContainsKey("Zip")
                || match.MatchResults.ContainsKey("Street")
                || match.MatchResults.ContainsKey("CentroidPoint")
                || match.MatchResults.ContainsKey("BoundingBox")))
            {
                if (match.MatchResults.ContainsKey("City"))
                    City = match.MatchResults["City"];
                if (match.MatchResults.ContainsKey("State"))
                    State = match.MatchResults["State"];
                if (match.MatchResults.ContainsKey("County"))
                    County = match.MatchResults["County"];
                if (match.MatchResults.ContainsKey("Zip"))
                    Zipcode = match.MatchResults["Zip"];
                if (match.MatchResults.ContainsKey("CentroidPoint"))
                    centroidPoint = new PointShape(match.MatchResults["CentroidPoint"]);
                if (match.MatchResults.ContainsKey("BoundingBox"))
                    boundingBox = new RectangleShape(match.MatchResults["BoundingBox"]);
                if (match.MatchResults.ContainsKey("Street"))
                {
                    if (match.MatchResults.ContainsKey("HouseNumber"))
                    {
                        Address = match.MatchResults["HouseNumber"] + " " + match.MatchResults["Street"];
                    }
                    else
                        Address = match.MatchResults["Street"];
                }

                if (match.MatchResults.ContainsKey(SearchPlaceViewModel.InternalProjectionKey))
                {
                    internalProjection = match.MatchResults[SearchPlaceViewModel.InternalProjectionKey];
                }
                if (match.MatchResults.ContainsKey("SearchSegment"))
                {
                    searchSegment = match.MatchResults["SearchSegment"];
                }
            }
            else if (match.MatchResults.ContainsKey("mtrs")
                || match.MatchResults.ContainsKey("COUNTYNA")
                || match.MatchResults.ContainsKey("ABSTRACT"))
            {
                if (match.MatchResults.ContainsKey("CenterWkt"))
                    centroidPoint = new PointShape(match.MatchResults["CenterWkt"]);
                if (match.MatchResults.ContainsKey("BoundingBoxWkt"))
                {
                    PolygonShape polygon = new PolygonShape(match.MatchResults["BoundingBoxWkt"]);
                    boundingBox = polygon.GetBoundingBox();
                }

                string addressText = string.Empty;
                if (match.MatchResults.ContainsKey("mtrs"))
                {
                    addressText = match.MatchResults["mtrs"];
                }
                else
                {
                    if (match.MatchResults.ContainsKey("COUNTYNAME"))
                    {
                        addressText = match.MatchResults["COUNTYNAME"];
                    }
                    if (match.MatchResults.ContainsKey("ABSTRACT"))
                    {
                        addressText += "," + match.MatchResults["ABSTRACT"];
                    }
                    if (match.MatchResults.ContainsKey("SECTION"))
                    {
                        addressText += "," + match.MatchResults["SECTION"];
                    }
                    if (match.MatchResults.ContainsKey("FID"))
                    {
                        addressText += "," + match.MatchResults["FID"];
                    }
                    if (match.MatchResults.ContainsKey("SURVEY"))
                    {
                        addressText += "," + match.MatchResults["SURVEY"];
                    }
                    if (match.MatchResults.ContainsKey("BLOCK"))
                    {
                        addressText += "," + match.MatchResults["BLOCK"];
                    }
                    if (match.MatchResults.ContainsKey("SUBSURVEY"))
                    {
                        addressText += "," + match.MatchResults["SUBSURVEY"];
                    }
                    if (match.MatchResults.ContainsKey("COUNTYID"))
                    {
                        addressText += "," + match.MatchResults["COUNTYID"];
                    }
                }
                Address = addressText;
            }
            RaisePropertyChanged(() => IsEmpty);
        }

        private string AppendItem(string item, string appendItem)
        {
            if (!string.IsNullOrEmpty(appendItem))
            {
                item += appendItem + ",";
            }

            return item;
        }
    }
}
