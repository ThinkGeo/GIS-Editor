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
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using ThinkGeo.MapSuite.GisEditor.Properties;

namespace ThinkGeo.MapSuite.GisEditor
{
    [Serializable]
    [Obfuscation]
    internal class CommonProjectionViewModel : INotifyPropertyChanged, IProjectionViewModel
    {
        private string selectedProj4ProjectionParameters;
        private const string wgs84String = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs";
        private const string mercatorString = "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +no_defs";
        private static string noSelect = GisEditor.LanguageManager.GetStringResource("ProjectionConfigurationCommonProjectionsPleaseSelect");
        private string selectedZone = noSelect;

        [NonSerialized]
        private XElement utmProjSource;

        [NonSerialized]
        private XElement statePlaneProjSource;

        private UnitType selectedUnitType;
        private DatumType selectedDatumType;
        private ProjectionType selectedProjectionType;
        private ObservableCollection<DatumType> supportedDatumTypes;
        private ObservableCollection<string> supportedZones;
        private ObservableCollection<UnitType> supportedUnits;
        private string tmpSelectedZone;

        public event PropertyChangedEventHandler PropertyChanged;

        public CommonProjectionViewModel()
        {
            using (var statePlaneProjSourceStream = new MemoryStream(Resources.projStatePlane))
            {
                statePlaneProjSource = XElement.Load(statePlaneProjSourceStream);
            }

            using (var utmProjSourceStream = new MemoryStream(Resources.projUTM))
            {
                utmProjSource = XElement.Load(utmProjSourceStream);
            }

            supportedDatumTypes = new ObservableCollection<DatumType>();
            supportedZones = new ObservableCollection<string>();
            supportedUnits = new ObservableCollection<UnitType>();
            supportedZones.Add(noSelect);
            using (StreamReader sr = new StreamReader(new MemoryStream(Resources.zoneForUtm)))
            {
                string line = String.Empty;
                while (!String.IsNullOrEmpty(line = sr.ReadLine()))
                {
                    supportedZones.Add(line);
                }
            }

            #region TestCode

            //// List no result in NAD83 + Different Zones.
            //var selectedProj4 = from projElement in statePlaneProjSource.Descendants("Proj4")
            //                    where SearchHandler(projElement)
            //                    select projElement.Attribute("Parameter").Value;

            //SelectedProjectionType = ProjectionType.StatePlane;
            //SelectedDatumType = DatumType.NAD27;

            //string noResultZone = "(";
            //foreach (var zone in SupportedZones)
            //{
            //    var result = from projElement in statePlaneProjSource.Descendants("Proj4")
            //                 where new Func<XElement, bool>(e => {
            //                     string datum = e.Attribute("Datum").Value;
            //                     string zoneString = e.Attribute("Zone").Value;
            //                     return (SelectedDatumType.ToString().Equals(datum, StringComparison.OrdinalIgnoreCase) && zoneString.Equals(zone, StringComparison.OrdinalIgnoreCase));
            //                 })(projElement)
            //                 select projElement.Attribute("Parameter").Value;

            //    if (result.Count() == 0)
            //    {
            //        noResultZone += String.Format("\"{0}\",", zone);
            //    }
            //    else if (result.Count() > 1)
            //    {
            //        Console.WriteLine(String.Format("More than one result with Datum={0} and Zone={1}", SelectedDatumType, zone));
            //    }
            //}

            //noResultZone = noResultZone.TrimEnd(',') + ")";
            //Console.WriteLine(String.Format("No result found with Datum={0} and Zone in {1}", SelectedDatumType, noResultZone));

            #endregion TestCode
        }

        public bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public ProjectionType SelectedProjectionType
        {
            get { return selectedProjectionType; }
            set
            {
                selectedProjectionType = value;
                OnPropertyChanged("SelectedProjectionType");
                OnPropertyChanged("SelectedProj4ProjectionParameters");
                OnPropertyChanged("SupportedDatumTypes");
                OnPropertyChanged("SupportedZones");
                OnPropertyChanged("SupportedUnits");
                OnPropertyChanged("IsDatumEnabled");
                OnPropertyChanged("IsUnitEnabled");
                OnPropertyChanged("IsZoneEnabled");
            }
        }

        public DatumType SelectedDatumType
        {
            get { return selectedDatumType; }
            set
            {
                selectedDatumType = value;
                OnPropertyChanged("SelectedDatumType");
                OnPropertyChanged("SelectedProj4ProjectionParameters");
                OnPropertyChanged("SupportedZones");
                OnPropertyChanged("IsZoneEnabled");
            }
        }

        public UnitType SelectedUnitType
        {
            get { return selectedUnitType; }
            set
            {
                selectedUnitType = value;
                OnPropertyChanged("SelectedUnitType");
                OnPropertyChanged("SelectedProj4ProjectionParameters");
            }
        }

        public string SelectedZone
        {
            get { return selectedZone; }
            set
            {
                selectedZone = value;
                OnPropertyChanged("SelectedZone");
                OnPropertyChanged("SelectedProj4ProjectionParameters");
            }
        }

        public ObservableCollection<DatumType> SupportedDatumTypes
        {
            get
            {
                supportedDatumTypes.Clear();
                supportedDatumTypes.Add(DatumType.None);

                if (SelectedProjectionType == ProjectionType.Geographic)
                {
                    supportedDatumTypes.Add(DatumType.WGS84);
                    SelectedDatumType = DatumType.WGS84;
                }
                else
                {
                    if (SelectedProjectionType == ProjectionType.UTM)
                    {
                        supportedDatumTypes.Add(DatumType.WGS84);
                    }

                    supportedDatumTypes.Add(DatumType.NAD27);
                    supportedDatumTypes.Add(DatumType.NAD83);
                    SelectedDatumType = DatumType.None;
                }
                return supportedDatumTypes;
            }
        }

        public ObservableCollection<string> SupportedZones
        {
            get
            {
                tmpSelectedZone = SelectedZone;
                supportedZones.Clear();
                supportedZones.Add(noSelect);

                StreamReader sr = null;
                try
                {
                    if (SelectedProjectionType == ProjectionType.StatePlane)
                        sr = new StreamReader(new MemoryStream(Resources.zoneForStatePlane));
                    else if (SelectedProjectionType == ProjectionType.UTM)
                        sr = new StreamReader(new MemoryStream(Resources.zoneForUtm));

                    if (sr != null)
                    {
                        string line = String.Empty;
                        while (!String.IsNullOrEmpty(line = sr.ReadLine()))
                        {
                            if (SelectedProjectionType == ProjectionType.StatePlane)
                            {
                                if (!String.IsNullOrEmpty(GetProj4String(statePlaneProjSource, line)))
                                {
                                    supportedZones.Add(line);
                                }
                            }
                            else
                            {
                                supportedZones.Add(line);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    GisEditor.LoggerManager.Log(LoggerLevel.Debug, e.Message, new ExceptionInfo(e));
                }
                finally
                {
                    if (sr != null)
                    {
                        sr.Close();
                        sr = null;
                    }
                }

                if (!String.IsNullOrEmpty(tmpSelectedZone) && supportedZones.Contains(tmpSelectedZone)) SelectedZone = tmpSelectedZone;
                else SelectedZone = noSelect;

                return supportedZones;
            }
        }

        public ObservableCollection<UnitType> SupportedUnits
        {
            get
            {
                UnitType tmpSelectedUnit = SelectedUnitType;
                supportedUnits.Clear();
                supportedUnits.Add(UnitType.None);
                if (SelectedProjectionType == ProjectionType.Geographic)
                {
                    supportedUnits.Add(UnitType.DecimalDegree);
                    SelectedUnitType = UnitType.DecimalDegree;
                }
                else if (SelectedProjectionType == ProjectionType.GoogleMaps)
                {
                    supportedUnits.Add(UnitType.Meters);
                    SelectedUnitType = UnitType.Meters;
                }
                else
                {
                    supportedUnits.Add(UnitType.Meters);
                    supportedUnits.Add(UnitType.Feet);

                    if (SelectedProjectionType == ProjectionType.UTM && (tmpSelectedUnit == UnitType.None || !supportedUnits.Contains(tmpSelectedUnit)))
                    {
                        SelectedUnitType = UnitType.Meters;
                    }
                    else if (supportedUnits.Contains(tmpSelectedUnit))
                    {
                        SelectedUnitType = tmpSelectedUnit;
                    }
                    else
                    {
                        SelectedUnitType = UnitType.None;
                    }
                }

                return supportedUnits;
            }
        }

        public bool IsDatumEnabled
        {
            get { return SelectedProjectionType == ProjectionType.StatePlane || SelectedProjectionType == ProjectionType.UTM; }
        }

        public bool IsZoneEnabled
        {
            get { return (IsDatumEnabled && SelectedDatumType != DatumType.None) || SelectedProjectionType == ProjectionType.UTM; }
        }

        public bool IsUnitEnabled
        {
            get { return SelectedProjectionType == ProjectionType.StatePlane || SelectedProjectionType == ProjectionType.UTM; }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public string SelectedProj4ProjectionParameters
        {
            get
            {
                if (SelectedProjectionType == ProjectionType.Geographic)
                {
                    selectedProj4ProjectionParameters = wgs84String;
                }
                else if (SelectedProjectionType == ProjectionType.GoogleMaps)
                {
                    selectedProj4ProjectionParameters = mercatorString;
                }
                else if (SelectedProjectionType == ProjectionType.StatePlane && SelectedDatumType != DatumType.None && SelectedZone != noSelect && SelectedUnitType != UnitType.None)
                {
                    selectedProj4ProjectionParameters = GetProj4String(statePlaneProjSource);
                }
                else if (SelectedProjectionType == ProjectionType.UTM && SelectedDatumType != DatumType.None && SelectedZone != noSelect)
                {
                    selectedProj4ProjectionParameters = GetProj4String(utmProjSource);
                }

                return selectedProj4ProjectionParameters;
            }
            set
            {
                selectedProj4ProjectionParameters = value;
                if (CheckIsSimpleEqual(selectedProj4ProjectionParameters, wgs84String))
                {
                    SelectedProjectionType = ProjectionType.Geographic;
                }
                else if (CheckIsSimpleEqual(selectedProj4ProjectionParameters, mercatorString))
                {
                    SelectedProjectionType = ProjectionType.GoogleMaps;
                }
                else if (CheckIsStatePlane(selectedProj4ProjectionParameters))
                {
                    var tmpProj4 = (from projElement in statePlaneProjSource.Descendants("Proj4")
                                    where new Func<XElement, bool>(xmlElement =>
                                    {
                                        return CheckIsSimpleEqual(xmlElement.Attribute("Parameter").Value, selectedProj4ProjectionParameters);
                                    })(projElement)
                                    select new
                                    {
                                        Datum = (DatumType)Enum.Parse(typeof(DatumType), projElement.Attribute("Datum").Value, true),
                                        Zone = projElement.Attribute("Zone").Value,
                                        Unit = GetGeographyUnitForStatePlane(selectedProj4ProjectionParameters)
                                    }).FirstOrDefault();

                    if (tmpProj4 != null)
                    {
                        SelectedDatumType = tmpProj4.Datum;
                        SelectedZone = tmpProj4.Zone;
                        SelectedUnitType = tmpProj4.Unit;
                    }
                }
                else if (CheckIsUTM(selectedProj4ProjectionParameters))
                {
                    var tmpProj4 = (from projElement in utmProjSource.Descendants("Proj4")
                                    where new Func<XElement, bool>(xmlElement =>
                                    {
                                        return CheckIsSimpleEqual(xmlElement.Attribute("Parameter").Value, selectedProj4ProjectionParameters);
                                    })(projElement)
                                    select new
                                    {
                                        Datum = (DatumType)Enum.Parse(typeof(DatumType), projElement.Attribute("Datum").Value, true),
                                        Zone = projElement.Attribute("Zone").Value,
                                    }).FirstOrDefault();

                    if (tmpProj4 != null)
                    {
                        SelectedDatumType = tmpProj4.Datum;
                        SelectedZone = tmpProj4.Zone;
                        SelectedUnitType = UnitType.Meters;
                    }
                }

                OnPropertyChanged("SelectedProj4ProjectionParameters");
            }
        }

        private static bool CheckIsUTM(string projectionString)
        {
            string tmpProj4 = projectionString.ToLowerInvariant();
            return tmpProj4.Contains("proj=utm");
        }

        private static UnitType GetGeographyUnitForStatePlane(string projectionString)
        {
            UnitType geographyUnit = UnitType.Meters;
            if (!String.IsNullOrEmpty(projectionString))
            {
                if (projectionString.Contains("units=m"))
                {
                    geographyUnit = UnitType.Meters;
                }
                else if (projectionString.Contains("to_meter=0.304") || projectionString.Contains("units=us-ft") || projectionString.Contains("units=ft"))
                {
                    geographyUnit = UnitType.Feet;
                }
            }
            return geographyUnit;
        }

        private static bool CheckIsStatePlane(string proj4)
        {
            string tmpProj4 = proj4.ToLowerInvariant();
            return tmpProj4.Contains("proj=lcc") || tmpProj4.Contains("proj=tmerc") || tmpProj4.Contains("proj=omerc");
        }

        private static bool CheckIsSimpleEqual(string source, string target)
        {
            return SimplifyString(source).Equals(SimplifyString(target), StringComparison.OrdinalIgnoreCase);
        }

        private static string SimplifyString(string str)
        {
            return str.ToLowerInvariant().Replace(" ", "").Replace("+", "").Replace("no_def", "");
        }

        private string GetProj4String(XElement searchSource)
        {
            return GetProj4String(searchSource, SelectedZone);
        }

        private bool SearchHandler(XElement element, string selectedZone)
        {
            string datum = element.Attribute("Datum").Value;
            string zone = element.Attribute("Zone").Value;
            if (String.IsNullOrEmpty(selectedZone)) return false;
            else return (SelectedDatumType.ToString().Equals(datum, StringComparison.OrdinalIgnoreCase) && selectedZone.Equals(zone, StringComparison.OrdinalIgnoreCase));
        }

        private string GetProj4String(XElement searchSource, string selectedZone)
        {
            var selectedProj4 = from projElement in searchSource.Descendants("Proj4")
                                where SearchHandler(projElement, selectedZone)
                                select projElement.Attribute("Parameter").Value;

            if (selectedProj4.Count() == 1)
            {
                string selectedProj4String = selectedProj4.First().Trim();
                if (IsUnitEnabled && SelectedUnitType == UnitType.Feet)
                {
                    string replaceString = selectedProj4String.Substring(selectedProj4String.LastIndexOf(" "));
                    selectedProj4String = selectedProj4String.Replace(replaceString, String.Format(CultureInfo.InvariantCulture, " +to_meter=0.3048006096012192 {0}", replaceString.Replace(" ", "")));
                }

                return selectedProj4String;
            }
            else
            {
                if (selectedProj4.Count() > 1)
                {
                }
            }

            return String.Empty;
        }
    }
}