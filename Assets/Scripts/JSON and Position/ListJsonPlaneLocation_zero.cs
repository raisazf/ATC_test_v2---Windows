using System.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Collections;
using TMPro;
using System.IO;
using RestSharp;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;


// During the first build and run, the app won't load planes and buttons. After the first build an run, upload the data to the directory on the headset:
// "This PC\Quest Pro\Internal shared storage\Android\data\com.DefaultCompany.ATC_test_v2\files"
// The data file is stored in Assets/Resources on the local machine

// Cou
public class ListJsonPlaneLocation_zero : MonoBehaviour
{

    //[SerializeField] 

    [SerializeField] private GameObject GlobalSystem;
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private GameObject flightButtonsTemplate;

    [SerializeField] private float radiusAdjustment = 1; // globe ball radius (unity units 1m)
    [SerializeField] private float airportLatitude = 38.94846f;
    [SerializeField] private float airpotLongitude = -77.44057f;

    [SerializeField] private Material haloMat;

    [SerializeField] private float altitudeAdjustment = 1f;
    [SerializeField] private float travelDistanceAdjustment = 1f;
    [SerializeField] private int requestFlightResponsesMax = 2028;

    public static List<GameObject> flightButtons;
    public static List<GameObject> planes;


    private List<string> flightNames;
    private List<flights> flightResponses;
    List<string> registrationNumbers = new List<string>();

    //private int count = 0;
    public float interval = 600f;
    private float time = 0.0f;
    public float speed = 1f;

    private Dictionary<string, int[]> listDictionary = new Dictionary<string, int[]>();

    private Vector3 cameraPosition;
    private OVRCameraRig cameraRig;

    private FlightsEmbeddedField testTower = new FlightsEmbeddedField();

    private Vector3 stPosition;
    private int responseIndex;
    private float move;
    private bool moveForward = true;

    void Start()
    {
        planes = new List<GameObject>();
        flightButtons = new List<GameObject>();
        flightNames = new List<string>();
        flightResponses = new List<flights>();

        responseIndex = -1;

        cameraRig = FindObjectOfType<OVRCameraRig>();

        ReadJson();

    }

    public void Update()
    {

        move = speed * Time.deltaTime;

        time += Time.deltaTime;

        // Check if the timer exceeds the desired interval
        if (time >= interval)
        {
            // Reset the timer
            time = 0f;
            PlanePosition();
        }

    }

    // Remove flights that left zone of interest from the list of active flights.
    private void RemoveFlight(int indexRemove)
    {
        if (indexRemove < 0) return;
        Destroy(planes[indexRemove]);
        planes.RemoveAt(indexRemove);

        Destroy(flightButtons[indexRemove]);
        flightButtons.RemoveAt(indexRemove);
        flightNames.RemoveAt(indexRemove);
    }

    // Converts lat and lng from Json file to (x, y, z) position in Unity
    private Vector3 GetXYZPositions(FlightsEmbeddedField flight, float flightAltitudeAjustment)
    {
        Vector3 nPosition = new Vector3();
        float newRadius;
        float radius = radiusAdjustment * GlobalSystem.transform.localScale.x;

        newRadius = (float)((float)(6.3781e6 + flightAltitudeAjustment) * radius / 6.3781e6);
        nPosition[0] = (newRadius) * Mathf.Cos(flight.lat * Mathf.Deg2Rad) * Mathf.Cos(flight.lng * Mathf.Deg2Rad) + GlobalSystem.transform.position.x;
        nPosition[2] = (newRadius) * Mathf.Cos(flight.lat * Mathf.Deg2Rad) * Mathf.Sin(flight.lng * Mathf.Deg2Rad) + GlobalSystem.transform.position.z;
        nPosition[1] = newRadius * Mathf.Sin((flight.lat) * Mathf.Deg2Rad) + GlobalSystem.transform.position.y;

        return nPosition;
    }

    // If flight just entered zone of interest, add it to the list of active flights
    public void AddFlight(FlightsEmbeddedField flight, float flightAltitudeAjustment)
    {

        Vector3 newPosition;
        Quaternion newRotation;

        //var result2 = string.Join("; ", flightNames.Select(s => s));

        flightNames.Add(flight.reg_number);

        newPosition = GetXYZPositions(flight, flightAltitudeAjustment);

        newRotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f);
        newPosition = GlobalSystem.transform.position + newRotation * (newPosition - GlobalSystem.transform.position);
        newRotation = newRotation * Quaternion.AngleAxis(-flight.dir, Vector3.up);


        planes.Add(Instantiate(planePrefab, newPosition, newRotation, GlobalSystem.transform));

        if (flight.arr_iata == "DCA")
        {
            planes[flightNames.Count - 1].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }

        planes[flightNames.Count - 1].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (flight.alt <= 1500f)
        {
            planes[flightNames.Count - 1].transform.rotation = Quaternion.identity;
        }
        else
        {
            planes[flightNames.Count - 1].transform.Rotate(0f, 0f, -flight.dir, Space.Self);
        }

        planes[flightNames.Count - 1].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));
        planes[flightNames.Count - 1].name = flight.reg_number;
        planes[flightNames.Count - 1].tag = "Untagged";

        //planes[flightNames.Count - 1].GetComponentInChildren<TextMeshPro>().text = flight.reg_number; // too busy when every plane has a number displayed

        GameObject menuPanel = GameObject.Find("MenuPanel");
        Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;
        menuPanel.transform.LookAt(cameraPosition);

        flightButtons.Add(Instantiate(flightButtonsTemplate,
            new Vector3(flightButtonsTemplate.transform.position.x,
            flightButtonsTemplate.transform.position.y, flightButtonsTemplate.transform.position.z),
            Quaternion.Euler(0f, menuPanel.transform.eulerAngles.y / 2, 0f), flightButtonsTemplate.transform.parent.transform));

        flightButtons[flightNames.Count - 1].transform.Rotate(transform.up, -(180f - menuPanel.transform.eulerAngles.y / 2));
        flightButtons[flightNames.Count - 1].name = flight.reg_number;
        flightButtons[flightNames.Count - 1].tag = "Untagged";

        string temp = flight.reg_number + "    " + flight.lat.ToString("F4") + "   " + flight.lng.ToString("F4") +
            "    " + flight.alt.ToString("F0") + "    " + flight.dir.ToString("F0");
        flightButtons[flightNames.Count - 1].GetComponentInChildren<TextMeshProUGUI>().text = temp;

    }

    private void UpdatePlanePosition(int currentIndex, FlightsEmbeddedField flightCurrent, float flightAltitudeAjustment)
    {

        Vector3 endPosition;
        Quaternion rotation;

        endPosition = GetXYZPositions(flightCurrent, flightAltitudeAjustment);
        rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f); // adjust plane location to GlobalSystem rotation
        endPosition = GlobalSystem.transform.position + rotation * (endPosition - GlobalSystem.transform.position);

        planes[currentIndex].transform.position = Vector3.MoveTowards(planes[currentIndex].transform.position, endPosition, move);

        planes[currentIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (moveForward)
        {
            planes[currentIndex].transform.Rotate(Vector3.forward * (-flightCurrent.dir));
        }
        else
        {
            planes[currentIndex].transform.Rotate(Vector3.forward * (180f - flightCurrent.dir));
        }

        if (flightCurrent.alt < 100f)
        {
            planes[currentIndex].transform.eulerAngles = new Vector3(planes[currentIndex].transform.rotation.x, planes[currentIndex].transform.rotation.y, 0f);
            planes[currentIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));
            planes[currentIndex].transform.localScale = Vector3.Lerp(planes[currentIndex].transform.localScale, planes[currentIndex].transform.localScale/2f, move);
        }

        if (flightCurrent.arr_iata == "IAD")
        {
            planes[currentIndex].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }

        // update button information
        string temp = flightCurrent.reg_number + "     " + flightCurrent.lat.ToString("F3") + "      " + flightCurrent.lng.ToString("F3") + "      " + flightCurrent.alt.ToString("F0") + "     " + flightCurrent.dir.ToString("F0");
        if (flightNames[currentIndex] == flightCurrent.reg_number)
            flightButtons[currentIndex].GetComponentInChildren<TextMeshProUGUI>().text = temp;

    }

    public void PlanePosition()
    {

        float altAdjustment = 0;
        int currentIndex, planeIndex;
        List<string> flightNamesCurrent = new List<string>();
        List<string> planeNames = new List<string>();

        FlightsEmbeddedField flightCurrent;
        int[] indeces;
        bool current = false;

        if (responseIndex < requestFlightResponsesMax-1 && moveForward)
        {
            responseIndex++;
        }
        else 
        {
            moveForward = false;
            responseIndex--;
            if (responseIndex == 0) moveForward = true;
        }

        float flightAltitudeAdjustment;
        List<float> altitudeAjustments = AdjustAltitude(responseIndex);

        foreach (var reg_number in registrationNumbers)
        {
            indeces = listDictionary[reg_number];
            current = false;

            flightNamesCurrent = flightResponses[responseIndex].response.Select(a => a.reg_number).ToList(); // get the names of all flights in this response
            currentIndex = flightNamesCurrent.IndexOf(reg_number); // get index of the current flight. -1 if not in this response

            if (indeces[0] == responseIndex && !current && currentIndex != -1 && moveForward) // first response that contains this flight
            {
                // instantiate a plane corresponding to this flight
                if (!flightNames.Contains(reg_number))
                    AddFlight(flightResponses[responseIndex].response[currentIndex], altAdjustment); // double check it was not previously added

                flightCurrent = flightResponses[responseIndex].response[currentIndex];
                current = true;
            }
            else if (indeces[1] == responseIndex && responseIndex < requestFlightResponsesMax - 1 && moveForward) // last response that contains this flight
            {
                planeNames = planes.Select(a => a.name).ToList(); // check if this plane was previously instantiated
                planeIndex = planeNames.IndexOf(reg_number);

                if (planeIndex != -1)
                    RemoveFlight(planeIndex); // remove from the list of the active planes
            }
            else //if (indeces[0] <= responsIndex && indeces[1] > responsIndex && current)
            {
                if (currentIndex != -1)
                {
                    flightCurrent = flightResponses[responseIndex].response[currentIndex];

                    if (flightCurrent.alt > flightResponses[responseIndex].response.Average(a => a.alt))
                    {
                        flightAltitudeAdjustment = flightCurrent.alt * altitudeAdjustment + altitudeAjustments.IndexOf(flightCurrent.alt) * 20;
                    }
                    else
                    {
                        flightAltitudeAdjustment = flightCurrent.alt * altitudeAdjustment - altitudeAjustments.IndexOf(flightCurrent.alt) * 20;
                    }

                    planeNames = planes.Select(a => a.name).ToList(); // get the names of all current planes
                    planeIndex = planeNames.IndexOf(reg_number); // get the index of this plane 
                    
                    if (planeIndex != -1)
                    UpdatePlanePosition(planeIndex, flightCurrent, flightAltitudeAdjustment); // update position of the plane
                }

                // the name of the plane is facing the player
                foreach (var plane in planes)
                {
                    cameraPosition = cameraRig.centerEyeAnchor.transform.position;

                    plane.transform.GetChild(2).transform.LookAt(cameraPosition);
                    //plane.transform.GetChild(2).GetChild(0).transform.LookAt(GlobalSystem.transform.position);
                }
            }
        }
    }



    private List<float> AdjustAltitude(int responseIndex)
    {

        List<float> localList = new List<float>();

        float splitvalue = flightResponses[responseIndex].response.Average(item => item.alt);

        localList = flightResponses[responseIndex].response.Select(flt => flt.alt).ToList();

        localList.Insert(0, splitvalue);
        localList = localList.OrderBy(x => x).ToList();

        return localList;
    }

        public void SaveFlightInfo(List<string> flightInfo, int indx)
    {
        StreamWriter writer = new StreamWriter(Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_writingData.json", true);
        for (int i = 0; i < indx; i++)
        {
            writer.WriteLine(flightInfo[i]); //writer.Write(" ");
                                             //writer.WriteLine(planes[i].transform.position);
        }
        writer.WriteLine("-------------------");
        writer.Close();
    }

    public void ReadJson()
    {
        List<int> requestNumbers = new List<int>();

        string jsonString;
        int start, end, indx;
        int[] indices = new int[3];

        // During the first build and run, the app won't load planes and buttons. After the first build an run, place the data file here:
        // This PC\Quest Pro\Internal shared storage\Android\data\com.DefaultCompany.ATC_test_v2\files and it should work properly
        // 

        StreamReader streamReader = new StreamReader(Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_data_next.json");
        //StreamReader streamReader = new StreamReader("C:/Users/raisa/Documents/ATC_test_v2_Windows/Assets/Resources/AirLab_data_next.json");
        //C: \Users\raisa\Documents\ATC_test_v2_Windows
        //var path = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_data_next.json";

        streamReader.BaseStream.Position = 0;
        jsonString = streamReader.ReadToEnd();
        streamReader.Close();


        Int32 next = 0;

        // break json line (the entire request) into individual responses
        for (int i = 1; i < jsonString.Length; i++)
        {
            indx = jsonString.IndexOf("res", i);
            if (indx > next)
            {
                requestNumbers.Add(indx);
                next = indx;
            }

        }

        // break each response into individual flights. 
        start = 0;
        end = 0;
        flights flightResponse = new flights();

        for (int i = 0; i < requestNumbers.Count - 1; i++) // the last request could be corrupted
        {
            start = requestNumbers[i] - 2;
            if (i == requestNumbers.Count - 1)
            {
                end = jsonString.Length;
            }
            else
            {
                end = requestNumbers[i + 1] - 2;
            }

            flightResponse = JsonConvert.DeserializeObject<flights>(jsonString[start..end]);

            flightResponses.Add(flightResponse);

        }


        for (int i = 0; i < flightResponses.Count - 1; i++)
        {
            for (int j = 0; j < flightResponses[i].response.Count; j++)
            {
                if (!registrationNumbers.Contains(flightResponses[i].response[j].reg_number))
                    registrationNumbers.Add(flightResponses[i].response[j].reg_number);
            }
        }

        bool first = true;
        List<string> flightsInRequest = new List<string>();
        float positionResamplingLat, positionResamplingLng;

        foreach (string name in registrationNumbers)
        {
            first = true;
            indices = new int[] { 0, 0, 0 };
            for (int i = 0; i < requestFlightResponsesMax; i++)
            {
                flightsInRequest = flightResponses[i].response.Select(flt => flt.reg_number).ToList();
                indx = flightsInRequest.FindIndex(a => a.Contains(name));
                if (indx != -1)
                {
                    if (indices[0] == 0 && first)
                    {
                        indices[0] = i;
                        first = false;
                    }
                    else
                    {
                        indices[1] = i;
                    }
                }
            }
            indices[2] = (indices[1] - indices[0]) / 2;
            listDictionary.Add(name, indices);
        }

        for (int i = 0; i < requestFlightResponsesMax; i++)
        {
            indices = new int[] { 0, 0 };

            foreach (string name in registrationNumbers)
            {

                flightsInRequest = flightResponses[i].response.Select(flt => flt.reg_number).ToList();
                indx = flightsInRequest.FindIndex(a => a.Contains(name));

                if (indx != -1)
                {
                    positionResamplingLat = (-listDictionary[name][2] + i) * travelDistanceAdjustment;
                    positionResamplingLng = (-listDictionary[name][2] + i) * travelDistanceAdjustment;

                    if (indx == (int)listDictionary[name][2])
                    {
                        positionResamplingLat = 0;
                        positionResamplingLng = 0;
                    }

                    if (flightResponses[i].response[indx].dir > 0 && flightResponses[i].response[indx].dir < -90)
                    {
                        flightResponses[i].response[indx].lat = flightResponses[i].response[indx].lat - positionResamplingLat;
                        flightResponses[i].response[indx].lng = flightResponses[i].response[indx].lng - positionResamplingLng;
                    }
                    else if (flightResponses[i].response[indx].dir > 90 && flightResponses[i].response[indx].dir <= 180)
                    {
                        flightResponses[i].response[indx].lat = flightResponses[i].response[indx].lat + positionResamplingLat;
                        flightResponses[i].response[indx].lng = flightResponses[i].response[indx].lng - positionResamplingLng;
                    }
                    else if (flightResponses[i].response[indx].dir > 180 && flightResponses[i].response[indx].dir <= 270)
                    {
                        flightResponses[i].response[indx].lat = flightResponses[i].response[indx].lat + positionResamplingLat;
                        flightResponses[i].response[indx].lng = flightResponses[i].response[indx].lng + positionResamplingLng;
                    }
                    else if (flightResponses[i].response[indx].dir > 270 && flightResponses[i].response[indx].dir <= 360)
                    {
                        flightResponses[i].response[indx].lat = flightResponses[i].response[indx].lat - positionResamplingLat;
                        flightResponses[i].response[indx].lng = flightResponses[i].response[indx].lng + positionResamplingLng;
                    }
                }
            }
        }
    }


    public List<flights> DeepCopy(List<flights> requestFlightResponsesCurrent)
    {
        List<flights> requestFlightResponsesTemp = new List<flights>(); // Create a new list to store copied elements

        for (int i = 1; i < requestFlightResponsesCurrent.Count - 1; i++)
        {
            requestFlightResponsesTemp.Add(requestFlightResponsesCurrent.ElementAt(i)); // Add each element to the new list
        }
        return requestFlightResponsesTemp; // Return the new list
    }

}
