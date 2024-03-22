using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testPosLatLong2 : MonoBehaviour
{

    public float longi;
    public float lati;
    public float radius;
    public Transform marker;

    // Use this for initialization
    void Start()
    {

        // Transfer to Radians from Degrees
        float templongi = longi * Mathf.PI / 180;
        float templati = lati * Mathf.PI / 180;

        float Xpos = radius * Mathf.Cos(templati) * Mathf.Cos(templongi);
        float Ypos = radius * Mathf.Cos(templati) * Mathf.Sin(templongi);
        float Zpos = radius * Mathf.Sin(templati);

        Debug.Log("X, Y, Z" + Xpos + " " + Ypos + " " + Zpos);

        // Set the X,Y,Z pos from the long and lat
        //Instantiate(marker);
        marker.position = new Vector3(Xpos, Zpos, Ypos);

    }

    // Update is called once per frame
    void Update()
    {

    }
}