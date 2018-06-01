﻿/*
Copyright 2018 Microsoft
Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using Newtonsoft.Json;
using Microsoft.Azure.Devices;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DMValidator
{
    public struct DeviceMethodReturnValue
    {
        public int Status;
        public string Payload;
    }

    public struct DeviceData
    {
        public string deviceJson;
        public string tagsJson;
        public string reportedPropertiesJson;
        public string desiredPropertiesJson;
    }

    class IoTHubManager
    {
        public const int DirectMethodSuccessCode = 0;
        public const int DirectMethodFailureCode = -1;


        static private string DeviceTwinDesiredProperties = "{{ \"properties\": {{ {0} }} }}";
        static private string messageDeviceTwinFunctionalityNotFound = "Device Twin functionality not found." + Environment.NewLine + "Make sure you are using the latest Microsoft.Azure.Devices package.";

        public IoTHubManager(string iotHubConnectionString)
        {
            _iotHubConnectionString = iotHubConnectionString;
        }

        public async Task UpdateDesiredProperties(string deviceId, string name, object value)
        {
            string propertiesString = "\"" + name + "\": " + ((value != null) ? value.ToString() : "null");
            string updateJson = String.Format(DeviceTwinDesiredProperties, propertiesString);
            Debug.WriteLine("updateJson: " + updateJson);

            dynamic registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);
            if (registryManager != null)
            {
                try
                {
                    string assemblyClassName = "Twin";
                    Type typeFound = (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                                      from assemblyType in assembly.GetTypes()
                                      where assemblyType.Name == assemblyClassName
                                      select assemblyType).FirstOrDefault();

                    if (typeFound != null)
                    {
                        var deviceTwin = await registryManager.GetTwinAsync(deviceId);

                        dynamic dp = JsonConvert.DeserializeObject(updateJson, typeFound);
                        dp.DeviceId = deviceId;
                        dp.ETag = deviceTwin.ETag;
                        registryManager.UpdateTwinAsync(dp.DeviceId, dp, dp.ETag);
                    }
                    else
                    {
                        MessageBox.Show(messageDeviceTwinFunctionalityNotFound, "Device Twin Properties Update");
                    }
                }
                catch (Exception ex)
                {
                    string errMess = "Update Twin failed. Exception: " + ex.ToString();
                    MessageBox.Show(errMess, "Device Twin Desired Properties Update");
                }
            }
            else
            {
                MessageBox.Show("Registry Manager is no initialized!", "Device Twin Desired Properties Update");
            }
        }

        public async Task<DeviceMethodReturnValue> InvokeDirectMethod(string deviceId, string methodName, string methodPayload)
        {
            TimeSpan timeoutInSeconds = new TimeSpan(0, 0, 30);
            CancellationToken cancellationToken = new CancellationToken();

            DeviceMethodReturnValue deviceMethodReturnValue;
            deviceMethodReturnValue.Status = DirectMethodSuccessCode;
            deviceMethodReturnValue.Payload = "";

            var serviceClient = ServiceClient.CreateFromConnectionString(_iotHubConnectionString);
            try
            {
                var cloudToDeviceMethod = new CloudToDeviceMethod(methodName, timeoutInSeconds);
                cloudToDeviceMethod.SetPayloadJson(methodPayload);

                var result = await serviceClient.InvokeDeviceMethodAsync(deviceId, cloudToDeviceMethod, cancellationToken);

                deviceMethodReturnValue.Status = result.Status;
                deviceMethodReturnValue.Payload = result.GetPayloadAsJson();
            }
            catch (Exception ex)
            {
                deviceMethodReturnValue.Status = DirectMethodFailureCode;
                deviceMethodReturnValue.Payload = ex.Message;
            }

            return deviceMethodReturnValue;
        }

        public async Task<DeviceData> GetDeviceData(string deviceId)
        {
            DeviceData result = new DeviceData();

            dynamic registryManager = RegistryManager.CreateFromConnectionString(_iotHubConnectionString);
            try
            {
                var deviceTwin = await registryManager.GetTwinAsync(deviceId);
                if (deviceTwin != null)
                {
                    result.deviceJson = deviceTwin.ToJson();
                    result.tagsJson = deviceTwin.Tags.ToJson();
                    result.reportedPropertiesJson = deviceTwin.Properties.Reported.ToJson();
                    result.desiredPropertiesJson = deviceTwin.Properties.Desired.ToJson();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + Environment.NewLine + "Make sure you are using the latest Microsoft.Azure.Devices package.", "Device Twin Properties");
            }
            return result;
        }

        private string _iotHubConnectionString;
    }
}
