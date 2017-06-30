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


// Copyright © Decebal Mihailescu 2010
// Some code was obtained by reverse engineering the PresentationFramework.dll using Reflector

// All rights reserved.
// This code is released under The Code Project Open License (CPOL) 1.02
// The full licensing terms are available at http://www.codeproject.com/info/cpol10.aspx
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
// PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
// REMAINS UNCHANGED.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using System.ComponentModel;

namespace ThinkGeo.MapSuite.GisEditor
{
    public interface IFileDlgExt : IWin32Window
    {
        event PathChangedEventHandler EventFileNameChanged;
        event PathChangedEventHandler EventFolderNameChanged;
        event FilterChangedEventHandler EventFilterChanged;
        AddonWindowLocation FileDlgStartLocation
        {
            set;
            get;
        }
        string FileDlgOkCaption
        {
            set;
            get;
        }
        bool FileDlgEnableOkBtn
        {
            set;
            get;
        }
        bool FixedSize
        {
            set;
        }
        NativeMethods.FolderViewMode FileDlgDefaultViewMode
        {
            set;
            get;
        }
    }
    //consider http://geekswithblogs.net/lbugnion/archive/2007/03/02/107747.aspx instead
    public interface IWindowExt //: IWin32Window
    {

        HwndSource Source
        {
            set;
        }
        IFileDlgExt ParentDlg
        {
            set;
        }
    }

}
