using UnityEngine;
using System.Collections;
using RestSharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace APIData
{
    public class GetApiData : MonoBehaviour
    {
        //[SerializeField] public Transform marker;
        [SerializeField] public string apiUrl = "https://airlabs.co/api/v9/"; // Replace with the actual AirLab API endpoint URL
        [SerializeField] public string apiKey = "&api_key=a206d42c-783a-494c-a21b-86bfaccdd9fd"; // Replace with your AirLab API key
        [SerializeField] public string endpoint_airport = "airports?iata_code=IAD"; //airport
        [SerializeField] public string endpoint_flights = "flights?flag=US,flight_iata=UA";
        [SerializeField] public GameObject GlobalSystem;
        [SerializeField] public GameObject marker;
        [SerializeField] public float radius = 1.0095f; // globe ball radius (unity units)
        [SerializeField] public bool isAirport = false;
        [SerializeField] public airports AirpotsResponse;
        [SerializeField] public flights FlightResponse;
        [SerializeField] public GameObject flightButtonsTemplate;
        [SerializeField] public GameObject flightButtonsParent;

        [SerializeField] public GameObject[] flightButtons;

        //public float latitude = 38.5072f; // lat
        //public float longitude = 77.1275f; // long

        //private GameObject marker;
        private GameObject[] planes;
        private float latitude;
        private float longitude;
        private float altitude;
        private float direction;
        private RestResponse response;
        private flights flightResponse;
        private string filePath;
        private StreamWriter writer;
        public void Start()
        {

            AirpotsResponse = new airports();
            FlightResponse = new flights();

            filePath = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_data.json";

            if (!File.Exists(filePath))
            {
                writer = new StreamWriter(filePath);
                //writer.Write(filePath,"");
                writer.Write("");
            }

        }

        public void Update()
        {
            writer = new StreamWriter(filePath);
            writer.Write(response);

        }

        public void GetDataFromAirLabApi()
        {

            //var client = new RestClient("https://airlabs.co/api/v9/flight?flight_iata=UA2029&api_key=a206d42c-783a-494c-a21b-86bfaccdd9fd");
            //var client = new RestClient("https://airlabs.co/api/v9/flights?view=array&_fields=hex,flag,lat,lng,dir,alt&api_key=a206d42c-783a-494c-a21b-86bfaccdd9fd");
            //var client = new RestClient("https://airlabs.co/api/v9/flights?fields=flag,lat,lng,dir,alt&api_key=a206d42c-783a-494c-a21b-86bfaccdd9fd&bbox=30,-90,37,-70");
            var client = new RestClient("https://airlabs.co/api/v9/flights?airline_iata=UA&fields=reg_number,lat,lng,dir,alt&bbox=36,-80,40,-65&api_key=a206d42c-783a-494c-a21b-86bfaccdd9fd");

            var request = new RestRequest();

            response = client.Execute(request);

            //if (isAirport)
            //{
            //    AirpotsResponse = AirportsResponse(response);
            //    return null;
            //}
            //else
            {
                FlightResponse = PlaneLocation();

                
            }


        }

        public void AirportsResponse(RestResponse response)
        {

            airports AirpotsResponse = JsonConvert.DeserializeObject<airports>(response.Content);
            Debug.Log($"{AirpotsResponse.response.lat}");
            Debug.Log($"{AirpotsResponse.response.lng}");
            Debug.Log($"{AirpotsResponse.response.alt}");
        }

        public flights PlaneLocation()
        {
            flightResponse = JsonConvert.DeserializeObject<flights>(response.Content);

            planes = new GameObject[flightResponse.response.Count];
            flightButtons = new GameObject[flightResponse.response.Count];

            Debug.Log("Number of flights = " + flightResponse.response.Count.ToString());


            for (int i = 0; i < flightResponse.response.Count; i++)
            {

                altitude = flightResponse.response[i].alt * 5; ;
                direction = flightResponse.response[i].dir;

                Debug.Log(message: $" Plane {flightResponse.response[i].reg_number}  Location { flightResponse.response[i].lat}, { flightResponse.response[i].lng}, {flightResponse.response[i].alt}");
                latitude = Mathf.PI * flightResponse.response[i].lat / 180;
                longitude = Mathf.PI * flightResponse.response[i].lng / 180;

                // adjust position by radians	???
                //latitude -= 1.570795765134f; // subtract 90 degrees (in radians)

                // and switch z and y (since z is forward)
                float newRadius = (float)((float)(2.093e7 + altitude) * radius / 2.093e7);
                float xPos = (newRadius) * Mathf.Cos(latitude) * Mathf.Cos(longitude);
                float zPos = (newRadius) * Mathf.Cos(latitude) * Mathf.Sin(longitude);
                float yPos = (newRadius) * Mathf.Sin(latitude);

                // move marker to position
                //marker = GameObject.FindGameObjectWithTag("Plane");

                planes[i] = Instantiate(marker, new Vector3(xPos, yPos, zPos), Quaternion.identity, GlobalSystem.transform);
                planes[i].tag = "Untagged";
                planes[i].transform.LookAt(Vector3.zero);
                if (altitude < 1000f)
                {
                    planes[i].transform.Rotate(0f, 0f, 0f, Space.Self);
                }
                else
                {
                    planes[i].transform.Rotate(0f, 0f, direction, Space.Self);
                }
                Debug.Log(message: $"Plane altitude { altitude} and direction { direction}");

                flightButtons[i] = Instantiate(flightButtonsTemplate, new Vector3(flightButtonsTemplate.transform.position.x, flightButtonsTemplate.transform.position.y, flightButtonsTemplate.transform.position.z), Quaternion.identity, flightButtonsParent.transform);
            }

            return flightResponse;
        }

    }
}
