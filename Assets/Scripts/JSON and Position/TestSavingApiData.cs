using UnityEngine;
using System.Collections;
using RestSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace APIData
{
    public class TestSavingApiData : MonoBehaviour
    {

        private RestResponse response;
        private string filePath;
        private StreamWriter writer;

        private flights flightResponse;

        private float interval = 0.1f;
        private float time = 0.0f;

        public void Start()
        {

            filePath = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_data_next.json";

            if (!File.Exists(filePath))
            {
                writer = new StreamWriter(filePath, true);
                writer.Write("");
                writer.Close();
            }
            GetDataFromAirLabApi();
            //GetDataFromAirLabApi();
            //GetDataFromAirLabApi();
        }

        public void Update()
        {
            //time += Time.deltaTime;
            //while (time >= interval)
            //{
                //GetDataFromAirLabApi();
            //    time -= interval;
            //    count = 0;
            //}
            GetDataFromAirLabApi();
        }

        public void GetDataFromAirLabApi()
        {

            //var client = new 
            //    RestClient("https://airlabs.co/api/v9/flights?zoom=11&airline_iata=UA&status=en-route&fields=reg_number,lat,lng,dir,alt,arr_iata&bbox=37,-79.5,40.5,-73&api_key=a206d42c-783a-494c-a21b-86bfaccdd9fd");
            var client = new
                RestClient("https://airlabs.co/api/v9/flights?zoom=5&airline_iata=UA&fields=reg_number,lat,lng,dir,alt,arr_iata&bbox=37,-79.5,39.5,-75.5&api_key=a206d42c-783a-494c-a21b-86bfaccdd9fd");

            var request = new RestRequest();
            response = client.Execute(request);

            flightResponse = JsonConvert.DeserializeObject<flights>(response.Content);

            writer = new StreamWriter(filePath, true);

            string json = JsonUtility.ToJson(flightResponse);

            writer.Write(json);
            writer.Close();
        }



    }
}
