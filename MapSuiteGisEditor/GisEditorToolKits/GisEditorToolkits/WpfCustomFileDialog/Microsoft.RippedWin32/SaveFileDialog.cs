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
    using MS.Win32;
    using System;
    using System.IO;
    using System.Security;
    using System.Security.Permissions;
    using System.Windows;
    using System.Runtime.InteropServices;
    using System.Windows.Controls;

    public sealed class SaveFileDialog<T> : FileDialogExt<T> where T : ContentControl, IWindowExt, new()
    {


        [SecurityCritical]
        public SaveFileDialog()
        {
            this.Initialize();
        }

        [SecurityCritical]
        private void Initialize()
        {
            base.SetOption(2, true);
        }

        [SecurityCritical]
        public Stream OpenFile()
        {
            string str = (base.FileNamesInternal.Length > 0) ? base.FileNamesInternal[0] : null;
            if (string.IsNullOrEmpty(str))
            {
                throw new InvalidOperationException("FileNameMustNotBeNull");
            }
            new FileIOPermission(FileIOPermissionAccess.Append | FileIOPermissionAccess.Write | FileIOPermissionAccess.Read, str).Assert();
            return new FileStream(str, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        [SecurityCritical]
        private bool PromptFileCreate(string fileName)
        {
            return base.MessageBoxWithFocusRestore(string.Format("Do you want to create {0} {1}?",Environment.NewLine,fileName) , MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
        }

        [SecurityCritical]
        private bool PromptFileOverwrite(string fileName)
        {
            return base.MessageBoxWithFocusRestore(string.Format("Do you want to overwite {0} {1}?", Environment.NewLine, fileName), MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
        }

        [SecurityCritical]
        internal override bool PromptUserIfAppropriate(string fileName)
        {
            bool flag;
            if (!base.PromptUserIfAppropriate(fileName))
            {
                return false;
            }
            new FileIOPermission(PermissionState.Unrestricted).Assert();
            try
            {
                flag = File.Exists(Path.GetFullPath(fileName));
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
            if ((this.CreatePrompt && !flag) && !this.PromptFileCreate(fileName))
            {
                return false;
            }
            if ((this.OverwritePrompt && flag) && !this.PromptFileOverwrite(fileName))
            {
                return false;
            }
            return true;
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
            bool saveFileName = false;
            saveFileName = NativeMethods.GetSaveFileName(ofn);
            if (!saveFileName)
            {
                switch (NativeMethods.CommDlgExtendedError())
                {
                    case 0x3001:
                        throw new InvalidOperationException("FileDialogSubClassFailure");

                    case 0x3002:
                        throw new InvalidOperationException("FileDialogInvalidFileName"+ base.SafeFileName );

                    case 0x3003:
                        throw new InvalidOperationException("FileDialogBufferTooSmall");
                }
            }
            return saveFileName;
        }

        public bool CreatePrompt
        {
            get
            {
                return base.GetOption(0x2000);
            }
            [SecurityCritical]
            set
            {
      
                base.SetOption(0x2000, value);
            }
        }

        public bool OverwritePrompt
        {
            get
            {
                return base.GetOption(2);
            }
            [SecurityCritical]
            set
            {
                base.SetOption(2, value);
            }
        }
    }
}

