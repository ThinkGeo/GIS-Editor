//******************************************************************************
//*
//* Name:     GdalConfiguration.cs
//* Project:  GDAL .NET Interface
//* Purpose:  A static configuration utility class to enable GDAL/OGR.
//* Author:   Felix Obermaier (original VB), translated to C#
//*
//******************************************************************************
//* Copyright (c) 2012-2018, Felix Obermaier
//*
//* Permission is hereby granted, free of charge, to any person obtaining a
//* copy of this software and associated documentation files (the "Software"),
//* to deal in the Software without restriction, including without limitation
//* the rights to use, copy, modify, merge, publish, distribute, sublicense,
//* and/or sell copies of the Software, and to permit persons to whom the
//* Software is furnished to do so, subject to the following conditions:
//*
//* The above copyright notice and this permission notice shall be included
//* in all copies or substantial portions of the Software.
//*
//* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//* OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
//* THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
//* DEALINGS IN THE SOFTWARE.
//*****************************************************************************/

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Gdal = OSGeo.GDAL.Gdal;
using Ogr = OSGeo.OGR.Ogr;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    /// <summary>
    /// Configuration class for GDAL/OGR
    /// </summary>
    public partial class GdalConfiguration
    {
        private static bool configuredOgr;
        private static bool configuredGdal;
        private static bool usable;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDefaultDllDirectories(uint directoryFlags);

        // LOAD_LIBRARY_SEARCH_DEFAULT_DIRS
        private const uint DllSearchFlags = 0x1000;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddDllDirectory(string lpPathName);

        /// <summary>
        /// Construction of Gdal/Ogr
        /// </summary>
        static GdalConfiguration()
        {
            string nativePath = null;
            string executingDirectory = null;
            string gdalPath = null;

            try
            {
                if (!IsWindows)
                {
                    const string notSet = "_Not_set_";
                    string tmp = Gdal.GetConfigOption("GDAL_DATA", notSet);
                    usable = tmp != notSet;
                    return;
                }

                executingDirectory = AppContext.BaseDirectory;
                if (string.IsNullOrEmpty(executingDirectory))
                {
                    throw new InvalidOperationException("cannot get executing directory");
                }

                SetDefaultDllDirectories(DllSearchFlags);
                gdalPath = Path.Combine(executingDirectory, "gdal");
                nativePath = Path.Combine(gdalPath, GetPlatform());

                if (!Directory.Exists(nativePath))
                {
                    throw new DirectoryNotFoundException($"GDAL native directory not found at '{nativePath}'");
                }

                string wrapperPath = Path.Combine(nativePath, "gdal_wrap.dll");
                if (!File.Exists(wrapperPath))
                {
                    throw new FileNotFoundException($"GDAL native wrapper file not found at '{wrapperPath}'");
                }

                AddDllDirectory(nativePath);
                AddDllDirectory(Path.Combine(nativePath, "plugins"));

                // Set the additional GDAL environment variables.
                string gdalData = Path.Combine(gdalPath, "data");
                Environment.SetEnvironmentVariable("GDAL_DATA", gdalData);
                Gdal.SetConfigOption("GDAL_DATA", gdalData);

                string driverPath = Path.Combine(nativePath, "plugins");
                Environment.SetEnvironmentVariable("GDAL_DRIVER_PATH", driverPath);
                Gdal.SetConfigOption("GDAL_DRIVER_PATH", driverPath);

                Environment.SetEnvironmentVariable("GEOTIFF_CSV", gdalData);
                Gdal.SetConfigOption("GEOTIFF_CSV", gdalData);

                string projSharePath = Path.Combine(gdalPath, "share");
                Environment.SetEnvironmentVariable("PROJ_LIB", projSharePath);
                Gdal.SetConfigOption("PROJ_LIB", projSharePath);
                OSGeo.OSR.Osr.SetPROJSearchPaths(new[] { projSharePath });

                string certificateFile = Path.Combine(gdalPath, "curl-ca-bundle.crt");
                Gdal.SetConfigOption("GDAL_CURL_CA_BUNDLE", certificateFile);

                usable = true;
            }
            catch (Exception e)
            {
                usable = false;
                Trace.WriteLine(e, "error");
                Trace.WriteLine($"Executing directory: {executingDirectory}", "error");
                Trace.WriteLine($"gdal directory: {gdalPath}", "error");
                Trace.WriteLine($"native directory: {nativePath}", "error");
                //throw;
            }
        }

        /// <summary>
        /// Gets a value indicating if the GDAL package is set up properly.
        /// </summary>
        public static bool Usable => usable;

        /// <summary>
        /// Method to ensure the static constructor is being called.
        /// </summary>
        /// <remarks>Be sure to call this function before using Gdal/Ogr/Osr</remarks>
        public static void ConfigureOgr()
        {
            if (!usable)
            {
                return;
            }

            if (configuredOgr)
            {
                return;
            }

            // Register drivers
            Ogr.RegisterAll();
            configuredOgr = true;
            PrintDriversOgr();
        }

        /// <summary>
        /// Method to ensure the static constructor is being called.
        /// </summary>
        /// <remarks>Be sure to call this function before using Gdal/Ogr/Osr</remarks>
        public static void ConfigureGdal()
        {
            if (!usable)
            {
                return;
            }

            if (configuredGdal)
            {
                return;
            }

            // Register drivers
            Gdal.AllRegister();
            configuredGdal = true;
            PrintDriversGdal();
        }

        /// <summary>
        /// Function to determine which platform we're on
        /// </summary>
        private static string GetPlatform()
        {
            return Environment.Is64BitProcess ? "x64" : "x86";
        }

        /// <summary>
        /// Gets a value indicating if we are on a windows platform
        /// </summary>
        private static bool IsWindows
        {
            get
            {
                return !(Environment.OSVersion.Platform == PlatformID.Unix ||
                         Environment.OSVersion.Platform == PlatformID.MacOSX);
            }
        }

        private static void PrintDriversOgr()
        {
#if DEBUG
            if (usable)
            {
                int num = Ogr.GetDriverCount();
                for (int i = 0; i < num; i++)
                {
                    var driver = Ogr.GetDriver(i);
                    Trace.WriteLine($"OGR {i}: {driver.GetName()}", "Debug");
                }
            }
#endif
        }

        private static void PrintDriversGdal()
        {
#if DEBUG
            if (usable)
            {
                int num = Gdal.GetDriverCount();
                for (int i = 0; i < num; i++)
                {
                    var driver = Gdal.GetDriver(i);
                    Trace.WriteLine($"GDAL {i}: {driver.ShortName}-{driver.LongName}");
                }
            }
#endif
        }
    }
}
