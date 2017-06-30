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


using System.Windows;
using System.Windows.Documents;
using GalaSoft.MvvmLight;

namespace ThinkGeo.MapSuite.GisEditor
{
    public class GisEditorMessageBoxViewModel : ViewModelBase
    {
        private Visibility isOkVisibility;
        private Visibility isCancelVisiblity;
        private Visibility isYesOrNoVisibility;
        private string errorMessage;
        private object detailContent;
        private string viewDetailHeader;
        private string noteMessage;

        public GisEditorMessageBoxViewModel()
        {
        }

        public Visibility IsOkVisibility
        {
            get { return isOkVisibility; }
            set
            {
                isOkVisibility = value;
                RaisePropertyChanged(() => IsOkVisibility);
            }
        }

        public Visibility IsCancelVisiblity
        {
            get { return isCancelVisiblity; }
            set
            {
                isCancelVisiblity = value;
                RaisePropertyChanged(() => IsCancelVisiblity);
            }
        }

        public Visibility IsYesOrNoVisibility
        {
            get { return isYesOrNoVisibility; }
            set
            {
                isYesOrNoVisibility = value;
                RaisePropertyChanged(() => IsYesOrNoVisibility);
            }
        }

        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                errorMessage = value;
                RaisePropertyChanged(() => ErrorMessage);
                RaisePropertyChanged(() => ErrorMessageVisible);
            }
        }

        public object DetailContent
        {
            get { return detailContent; }
            set { detailContent = value; }
        }

        public Visibility ErrorMessageVisible
        {
            get { return string.IsNullOrEmpty(errorMessage) && detailContent == null ? Visibility.Hidden : Visibility.Visible; }
        }

        public Visibility NoteMessageVisible
        {
            get { return string.IsNullOrEmpty(noteMessage) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public string ViewDetailHeader
        {
            get
            {
                if (string.IsNullOrEmpty(viewDetailHeader))
                {
                    viewDetailHeader = "View error";
                }
                return viewDetailHeader;
            }
            set
            {
                viewDetailHeader = value;
                RaisePropertyChanged(() => ViewDetailHeader);
            }
        }

        public string NoteMessage
        {
            get { return noteMessage; }
            set
            {
                noteMessage = value;
                RaisePropertyChanged(() => NoteMessage);
            }
        }
    }
}