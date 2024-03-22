using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testPosLatLong : MonoBehaviour
{

    public float radius;
    public Transform marker;
    public Transform marker2;
    public Collider earth;
    public Collider sphere;

    private float temp_lng;
    private float temp_lat;
    private float dir;
    private float alt;
    private float Alt;

    //private GetApiData apiData;

    // Use this for initialization
    void Start()
    {
        //apiData = new GetApiData();
        //flights Flights = new flights();
        //Flights = apiData.FlightResponse;
        //int NumFlights = apiData.FlightResponse.response.Count;
        //int flightsCount = Flights.response.Count;

        int NumFlights = 1;
        // Transfer to Radians from Degrees
        for (int flight = 0; flight < NumFlights; flight++)
        {
            //temp_lng = apiData.FlightResponse.response[flight].lng * Mathf.PI / 180;
            //temp_lat = apiData.FlightResponse.response[flight].lat * Mathf.PI / 180;

            //"lat":37.017921,"lng":-94.32115,"alt":10363,"dir":256.9

            temp_lng = -94.32115f * Mathf.PI / 180;
            temp_lat = 37.017921f * Mathf.PI / 180;
            dir = 256f;
            alt = 34363f;
            Alt = alt * radius / 2.1e7f;
            Debug.Log(message: $"Radius {Alt}");

            float Xpos = radius * Mathf.Cos(temp_lat) * Mathf.Cos(temp_lng);
            float Ypos = radius * Mathf.Cos(temp_lat) * Mathf.Sin(temp_lng);
            float Zpos = radius * Mathf.Sin(temp_lat);

            Debug.Log("X, Y, Z" + Xpos + " " + Ypos + " " + Zpos);

            // Set the X,Y,Z pos from the long and lat
            //Instantiate(marker);
            marker.position = new Vector3(Xpos, Zpos, Ypos);
           
            Vector3 center = new Vector3(0, 0, 0);
            //marker2.position = new Vector3(marker.position.x, marker.position.y, marker.position.z);
            marker2.position = marker.position;
            marker2.rotation = Quaternion.Euler(0, 0, dir);
            marker2.LookAt(center);
            marker2.Translate(0, 0, -Alt);

            Debug.Log(message: $"test Position { marker.position} and {Vector3.forward}");
        }
    }

    public void OnCollisionEnter(Collision collision)
    {

        var NewVector = new Vector3(collision.contacts[1].normal.z, collision.contacts[1].normal.x, collision.contacts[1].normal.y);
        marker2.position = Vector3.Cross(NewVector, collision.contacts[1].normal);


    }
    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        //Gizmos.color = Color.blue;
       // Gizmos.DrawLine(marker.position, marker2.position);

       //Gizmos.color = Color.green;
       // Gizmos.DrawLine(marker.position, new Vector3(0, 0, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(marker2.position, new Vector3(0, 0, 0));
    }

    // Update is called once per frame
    void Update()
    {

    }
}