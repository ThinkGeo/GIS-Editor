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


using GalaSoft.MvvmLight.Command;
using System;
using System.ComponentModel;
using System.Windows;

namespace ThinkGeo.MapSuite.GisEditor.Plugins
{
    public class SendMailViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string companyName;
        private string contactName;
        private string address;
        private string phoneNumber;
        private string emailAddress;
        private RelayCommand sendCommand;

        public SendMailViewModel()
        {
        }

        public string CompanyName
        {
            get { return companyName; }
            set { companyName = value; }
        }

        public string ContactName
        {
            get { return contactName; }
            set { contactName = value; }
        }

        public string Address
        {
            get { return address; }
            set { address = value; }
        }

        public string PhoneNumber
        {
            get { return phoneNumber; }
            set { phoneNumber = value; }
        }

        public string EmailAddress
        {
            get { return emailAddress; }
            set { emailAddress = value; }
        }

        public RelayCommand SendCommand
        {
            get
            {
                if (sendCommand == null)
                {
                    sendCommand = new RelayCommand(() =>
                    {
                        if (string.IsNullOrEmpty(contactName) || string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(emailAddress))
                        {
                            MessageBox.Show(GisEditor.LanguageManager.GetStringResource("SendMailWindowRequiredFieldLabel"), GisEditor.LanguageManager.GetStringResource("SendMailWindowInputInvalidLabel"));
                            return;
                        }
                        SendMail();
                    });
                }
                return sendCommand;
            }
        }

        public void SendMail()
        {
            //TODO: can replace to your email server.
        }

        private string GetMailBody()
        {
            return @"Hi," + Environment.NewLine + Environment.NewLine
                + "I’m a user of ThinkGeo’s GIS Editor and I would like to request a free 15 day trial key for access to the Parcel Atlas layer.  Please contact me at using the information below."
                + Environment.NewLine + Environment.NewLine + "Company Name: " + companyName + Environment.NewLine +
                "Contact Name: " + contactName + Environment.NewLine +
                "Address: " + address + Environment.NewLine +
                "Phone Number: " + phoneNumber + Environment.NewLine +
                "Email Address: " + emailAddress + Environment.NewLine + Environment.NewLine + "Thanks," + Environment.NewLine + contactName;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
