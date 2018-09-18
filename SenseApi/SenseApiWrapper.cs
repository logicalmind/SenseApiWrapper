﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SenseApi.Models;

namespace SenseApi
{
    public class SenseApiWrapper
    {
        public AuthorizationResponse AuthorizationResponse { get; set; }
        public MonitorStatus MonitorStatus { get; set; }
        public List<Device> DeviceList { get; set; }
        public static IConfigurationRoot Config { get; private set; }
        private readonly string apiAddress;

        public SenseApiWrapper()
        {
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            apiAddress = Config["api-url"];
        }


        /// <summary>
        /// Authenticate with the Sense API using your email and password
        /// </summary>
        /// <param name="email">Email to use to authenticate</param>
        /// <param name="password">Password to use to authenticate</param>
        /// <returns>Authorization Response object</returns>
        public async Task<AuthorizationResponse> Authenticate(string email, string password)
        {
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("email", email),
                new KeyValuePair<string, string>("password", password)
            };

            using (var httpClient = new HttpClient())
            {
                using (var content = new FormUrlEncodedContent(postData))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    var response = await httpClient.PostAsync($"{apiAddress}/authenticate", content);
                    var json = await response.Content.ReadAsStringAsync();

                    AuthorizationResponse = JsonConvert.DeserializeObject<AuthorizationResponse>(json);
                    return AuthorizationResponse;
                }
            }
        }

        /// <summary>
        /// Get the status of the Sense Monitor from the Sense API
        /// </summary>
        /// <param name="monitorId">Monitor ID to check</param>
        /// <returns>Monitor Status Object</returns>
        public async Task<MonitorStatus> GetMonitorStatus(int monitorId)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationResponse.AccessToken);

                var response = await httpClient.GetAsync($"{apiAddress}/app/monitors/{monitorId}/status");
                var json = await response.Content.ReadAsStringAsync();

                MonitorStatus = JsonConvert.DeserializeObject<MonitorStatus>(json);

                return MonitorStatus;
            }
        }

        public async Task<List<Device>> GetDeviceList(int monitorId)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationResponse.AccessToken);

                var response = await httpClient.GetAsync($"{apiAddress}/app/monitors/{monitorId}/devices");
                var json = await response.Content.ReadAsStringAsync();

                DeviceList = JsonConvert.DeserializeObject<List<Device>>(json);

                return DeviceList;
            }
        }

        public async Task<DeviceDetails> GetDeviceDetails(int monitorId, string deviceId)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AuthorizationResponse.AccessToken);

                var response = await httpClient.GetAsync($"{apiAddress}/app/monitors/{monitorId}/devices/{deviceId}");
                var json = await response.Content.ReadAsStringAsync();

                var deviceDetails = JsonConvert.DeserializeObject<DeviceDetails>(json);

                if (DeviceList == null || DeviceList.Count <= 0) return deviceDetails;

                var device = DeviceList.FirstOrDefault(x => x.Id == deviceId);
                if (device == null) return deviceDetails;

                var pos = DeviceList.FindIndex(x => x.Id == deviceId);
                DeviceList.RemoveAt(pos);
                device = deviceDetails.Device;
                DeviceList.Insert(pos, device);

                return deviceDetails;
            }
        }
    }
}
