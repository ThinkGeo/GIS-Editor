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


// Copyright � Decebal Mihailescu 2010
// Some code was obtained by reverse engineering the PresentationFramework.dll using Reflector

// All rights reserved.
// This code is released under The Code Project Open License (CPOL) 1.02
// The full licensing terms are available at http://www.codeproject.com/info/cpol10.aspx
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
// REMAINS UNCHANGED.
namespace ThinkGeo.MapSuite.GisEditor
{
    //using MS.Internal.PresentationFramework;
    using MS.Win32;
    using System;
    using System.IO;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Reflection;
    using System.Windows.Controls;

    public sealed class OpenFileDialog<T> : FileDialogExt<T> where T : ContentControl, IWindowExt, new()
    {
        [SecurityCritical]
        public OpenFileDialog()
        {
            this.Initialize();
        }

        [SecurityCritical]
        private void Initialize()
        {
            base.SetOption(0x1000, true);
        }

        [SecurityCritical]
        public Stream OpenFile()
        {
            string str = base.FileNamesInternal[0];
            if (string.IsNullOrEmpty(str))
            {
                throw new InvalidOperationException("FileNameMustNotBeNull");
            }
            FileStream stream = null;
            new FileIOPermission(FileIOPermissionAccess.Read, str).Assert();
            try
            {
                stream = new FileStream(str, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
            return stream;
        }

        [SecurityCritical]
        public Stream[] OpenFiles()
        {
            string[] fileNamesInternal = base.FileNamesInternal;
            Stream[] streamArray = new Stream[fileNamesInternal.Length];
            for (int i = 0; i < fileNamesInternal.Length; i++)
            {
                string str = fileNamesInternal[i];
                if (string.IsNullOrEmpty(str))
                {
                    throw new InvalidOperationException("FileNameMustNotBeNull");
                }
                FileStream stream = null;
                new FileIOPermission(FileIOPermissionAccess.Read, str).Assert();
                try
                {
                    stream = new FileStream(str, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
                finally
                {
                    CodeAccessPermission.RevertAssert();
                }
                streamArray[i] = stream;
            }
            return streamArray;
        }

        [SecurityCritical]
        public override void Reset()
        {
            base.Reset();
            this.Initialize();
        }


        [SecurityCritical]
        internal override bool RunFileDialog(OPENFILENAME_I ofn)
        {
            
            bool openFileName = false;
            openFileName = NativeMethods.GetOpenFileName(ofn);
            if (!openFileName)
            {

                switch (NativeMethods.CommDlgExtendedError())

                {
                    case 0x3001:
                        throw new InvalidOperationException("FileDialogSubClassFailure");

                    case 0x3002:
                        throw new InvalidOperationException("FileDialogInvalidFileName" + base.SafeFileName);

                    case 0x3003:
                        throw new InvalidOperationException("FileDialogBufferTooSmall");
                }
            }
            return openFileName;
        }

        public bool Multiselect
        {
            get
            {
                return base.GetOption(0x200);
            }
            [SecurityCritical]
            set
            {
                base.SetOption(0x200, value);
            }
        }

        public bool ReadOnlyChecked
        {
            get
            {
                return base.GetOption(1);
            }
            [SecurityCritical]
            set
            {
                base.SetOption(1, value);
            }
        }

        public bool ShowReadOnly
        {
            get
            {
                return !base.GetOption(4);
            }
            [SecurityCritical]
            set
            {
                base.SetOption(4, !value);
            }
        }
    }
}

