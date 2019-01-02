﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace IntelligentDroneKiosk.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        public SettingsPage()
        {
            this.InitializeComponent();
            if (localSettings.Values["DjiAppKey"] == null)
            {
                localSettings.Values["DjiAppKey"] = "";
            }
            if (localSettings.Values["ComputerVisionSubKey"] == null)
            {
                localSettings.Values["ComputerVisionSubKey"] = "";
            }
            if (localSettings.Values["CustomVisionTrainKey"] == null)
            {
                localSettings.Values["CustomVisionTrainKey"] = "";
            }
            if (localSettings.Values["CustomVisionPredictKey"] == null)
            {
                localSettings.Values["CustomVisionPredictKey"] = "";
            }
            if (localSettings.Values["ObjDetectProjectId"] == null)
            {
                localSettings.Values["ObjDetectProjectId"] = "";
            }
            if (localSettings.Values["ComputerVisionEndpointUrl"] == null)
            {
                localSettings.Values["ComputerVisionEndpointUrl"] = "";
            }
            if (localSettings.Values["CustomVisionEndpointUrl"] == null)
            {
                localSettings.Values["CustomVisionEndpointUrl"] = "";
            }

            txtDjiAppKey.Text = localSettings.Values["DjiAppKey"].ToString();
            txtComputerVisionSubKey.Text = localSettings.Values["ComputerVisionSubKey"].ToString();
            txtCustomVisionTrainKey.Text = localSettings.Values["CustomVisionTrainKey"].ToString();
            txtCustomVisionPredictKey.Text = localSettings.Values["CustomVisionPredictKey"].ToString();
            txtObjDetectProjectId.Text = localSettings.Values["ObjDetectProjectId"].ToString();
            txtComputerVisionEndpointUrl.Text = localSettings.Values["ComputerVisionEndpointUrl"].ToString();
            txtCustomVisionEndpointUrl.Text = localSettings.Values["CustomVisionEndpointUrl"].ToString();
        }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["DjiAppKey"] = txtDjiAppKey.Text;
            localSettings.Values["ComputerVisionSubKey"] = txtComputerVisionSubKey.Text;
            localSettings.Values["CustomVisionTrainKey"] = txtCustomVisionTrainKey.Text;
            localSettings.Values["CustomVisionPredictKey"] = txtCustomVisionPredictKey.Text;
            localSettings.Values["ObjDetectProjectId"] = txtObjDetectProjectId.Text;
            localSettings.Values["ComputerVisionEndpointUrl"] = txtComputerVisionEndpointUrl.Text;
            localSettings.Values["CustomVisionEndpointUrl"] = txtCustomVisionEndpointUrl.Text;
        }
    }
}