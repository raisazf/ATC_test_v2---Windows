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
public class ListJsonPlaneLocation_zero_changingPrevious : MonoBehaviour
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
    [SerializeField] private float positionSpreadAdjustment = 1f;
    [SerializeField] private float travelDistanceAdjustment = 1f;
    [SerializeField] private int requestFlightResponsesMax = 2028;

    public static List<GameObject> flightButtons;
    public static List<GameObject> planes;


    private List<string> flightNames;
    private List<string> flightNamesPrevious;
    private List<flights> requestFlightResponses;
    private List<flights> requestFlightResponsesPrevious;
    List<string> names = new List<string>();

    //private int count = 0;
    public float interval = 600f;
    private float time = 0.0f;
    public float transitionDuration = 1f;

    private Dictionary<string, int[]> listDictionary = new Dictionary<string, int[]>();

    private Vector3 cameraPosition;
    private OVRCameraRig cameraRig;
    void Start()
    {

        planes = new List<GameObject>();
        flightButtons = new List<GameObject>();
        flightNamesPrevious = new List<string>();
        flightNames = new List<string>();
        requestFlightResponses = new List<flights>();
        requestFlightResponsesPrevious = new List<flights>();

        cameraRig = FindObjectOfType<OVRCameraRig>();

        ReadJson();

    }

    public void Update()
    {
        float radius = radiusAdjustment * GlobalSystem.transform.localScale.x;
        GameObject menuPanel = GameObject.Find("MenuPanel");
        //GameObject lookAtGlobal = GameObject.Find("LookAt");
        Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;
        menuPanel.transform.LookAt(cameraPosition);
        //lookAtGlobal.transform.LookAt(cameraPosition);

        time += Time.deltaTime;

        // Check if the timer exceeds the desired interval
        if (time >= interval)
        {
            // Reset the timer
            time = 0f;

            CallPlanePosition();

        }

    }

    //private flights CopyFlights(flights flightsCurrent)
    //{

    //    flights flightsPrevioius = new flights();
    //    flightsPrevioius.response = new List<FlightsEmbeddedField>();

    //    int count = 0;
    //    foreach (var flight in flightsCurrent.response)
    //    {
    //        flightsPrevioius.response.Add(flight);
    //        count++;
    //    }

    //    return flightsPrevioius;

    //}

    private void CallPlanePosition()
    {
        flights flightResponsePrevious = new flights();
        flights flightResponse = new flights();


        //flightResponsePrevious = CopyFlights(requestFlightResponses[0]);

        for (int responseIndex = 0; responseIndex < requestFlightResponsesMax; responseIndex++)
        {
            //Debug.Log("updating " + responseIndex);
            flightResponse = requestFlightResponses[responseIndex];
            flightResponsePrevious = requestFlightResponsesPrevious[responseIndex];
            PlanePosition(flightResponse, flightResponsePrevious, responseIndex);
            //flightResponsePrevious = CopyFlights(requestFlightResponses[responseIndex]);

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
    private Vector3 GetXYZPositions(FlightsEmbeddedField flight, float altAdjustment)
    {
        Vector3 nPosition = new Vector3();
        float newRadius;
        float radius = radiusAdjustment * GlobalSystem.transform.localScale.x;

        // Debug.Log(" Getting XYZ  " + name + " = " + flight.reg_number + " lat " + flight.lat + " lng " + flight.lng);
        newRadius = (float)((float)(6.3781e6 + flight.lat * Mathf.Deg2Rad) * radius / 6.3781e6);
        nPosition[0] = (newRadius) * Mathf.Cos(flight.lat * Mathf.Deg2Rad) * Mathf.Cos(flight.lng * Mathf.Deg2Rad) + GlobalSystem.transform.position.x;
        nPosition[2] = (newRadius) * Mathf.Cos(flight.lat * Mathf.Deg2Rad) * Mathf.Sin(flight.lng * Mathf.Deg2Rad) + GlobalSystem.transform.position.z;
        nPosition[1] = (newRadius + altAdjustment) * Mathf.Sin((flight.lat) * Mathf.Deg2Rad) + GlobalSystem.transform.position.y;
        //Debug.Log(" XYZ position " + name + " = " + flight.reg_number + " position " + nPosition);


        return nPosition;
    }

    // If flight just entered zone of interest, add it to the list of active flights
    public void AddFlight(FlightsEmbeddedField flight, float altAdjustment)
    {

        Vector3 newPosition;
        Quaternion newRotation;

        var result2 = string.Join("; ", flightNames.Select(s => s));

        flightNames.Add(flight.reg_number);

        newPosition = GetXYZPositions(flight, altAdjustment);

        newRotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f);
        newPosition = GlobalSystem.transform.position + newRotation * (newPosition - GlobalSystem.transform.position);
        newRotation = newRotation * Quaternion.AngleAxis(-flight.dir, Vector3.up);


        planes.Add(Instantiate(planePrefab, newPosition, newRotation, GlobalSystem.transform));

        if (flight.arr_iata == "DCA")
        {
            planes[flightNames.Count - 1].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }

        planes[flightNames.Count - 1].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (flight.alt <= 90f)
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
        //Debug.Log(" AddFlight adding name " + flight.reg_number + " to the list list " + result2 + " instantiate plane to " + planes[flightNames.Count - 1].name);

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

    private (List<Vector3>, List<Quaternion>) CopyList(List<GameObject> currentPlanes)
    {

        List<Vector3> planesPreviousPosition = new List<Vector3>();
        List<Quaternion> planesPreviousRotation = new List<Quaternion>();
        planesPreviousPosition.Clear();
        planesPreviousRotation.Clear();

        foreach (var plane in currentPlanes)
        {
            var tempPosition = plane.transform.position;
            planesPreviousPosition.Add(tempPosition);
            var tempRotation = plane.transform.rotation;
            planesPreviousRotation.Add(tempRotation);
        }

        return (planesPreviousPosition, planesPreviousRotation);

    }

    public void PlanePosition(flights flightResponse, flights flightResponsePreviousTT, int responsIndex)
    {

        float altAdjustment = 0;
        int currentIndex, previousIndex, planeIndex;
        List<string> flightNamesCurrent = new List<string>();
        List<string> flightNamesPrevious = new List<string>();
        List<string> planeNames = new List<string>();

        FlightsEmbeddedField flightCurrent, flightPrevious;
        int[] indeces;
        bool current = false;

        foreach (var name in names)
        {
            indeces = listDictionary[name];
            current = false;



            flightNamesCurrent = requestFlightResponses[responsIndex].response.Select(a => a.reg_number).ToList(); // get the names of all flights in this response
            currentIndex = flightNamesCurrent.IndexOf(name); // get index of the current flight. -1 if not in this response
            flightNamesPrevious = requestFlightResponsesPrevious[responsIndex].response.Select(a => a.reg_number).ToList(); // get the names of all flights in previous response
            previousIndex = flightNamesPrevious.IndexOf(name); // get index of this flight in the previous response. -1 if not in previous response

            var result2 = string.Join("; ", requestFlightResponses[responsIndex].response.Select(a => a.lat).ToList());
            Debug.Log(" Current name lat " + name + " for index "+ responsIndex +" lat " + result2);
            result2 = string.Join("; ", requestFlightResponsesPrevious[responsIndex].response.Select(a => a.lat).ToList());
            Debug.Log(" Previous name lat " + name + "  " + result2);

            if (indeces[0] == responsIndex && !current) // first response that contains this flight
            {
                //Debug.Log(" PlanePositioning adding  " + name + " indeces (" + indeces[0] + ", " + indeces[1] + ") responsIndex " + responsIndex + " current Index " + currentIndex + " contains " + flightNames.Contains(name));
                //var result2 = string.Join("; ", flightNames.Select(s => s));
                //Debug.Log(" PlanePositioning adding name " + name + " to the list list " + result2);
                // instantiate a plane corresponding to this flight
                if (!flightNames.Contains(name))
                    AddFlight(flightResponse.response[currentIndex], altAdjustment); // double check it was not previously added

                flightCurrent = flightResponse.response[currentIndex];
                current = true;
            }
            else if (indeces[1] == responsIndex && responsIndex < requestFlightResponsesMax - 1) // last response that contains this flight
            {


                planeNames = planes.Select(a => a.name).ToList(); // check if this plane was previously instantiated
                planeIndex = planeNames.IndexOf(name);

                //Debug.Log(" Removing plane name " + name + " at " + planeIndex);

                if (planeIndex != -1)
                    RemoveFlight(planeIndex); // remove from the list of the active planes
            }
            else //if (indeces[0] <= responsIndex && indeces[1] > responsIndex && current)
            {
                //var result2 = string.Join("; ", requestFlightResponses[responsIndex].response.Select(a => a.lat).ToList());
                //Debug.Log(" Current name lat " + name + " for index "+ responsIndex +" lat " + result2);
                //result2 = string.Join("; ", requestFlightResponsesPrevious[responsIndex].response.Select(a => a.lng).ToList());
                //Debug.Log(" Previous name lng " + name + " to the list list " + result2);

                //Debug.Log(" Updating plane position " + name + " indeces (" + indeces[0] + ", " + indeces[1] + ") responsIndex " + responsIndex);
                if (currentIndex != -1)
                {
                    flightCurrent = requestFlightResponses[responsIndex].response[currentIndex];

                    if (previousIndex == -1)
                    {
                        //Debug.Log(" Updating if prev does not exist " + name + " currentIndex " + currentIndex + " flightCurrent " + flightCurrent.reg_number);
                        previousIndex = currentIndex;
                        flightPrevious = flightCurrent;
                    }
                    else
                    {
                        flightPrevious = requestFlightResponsesPrevious[responsIndex].response[previousIndex];
                        //Debug.Log(" Updating if prev does xists " + name + " previous " + previousIndex + " flightPrevious (" + requestFlightResponsesPrevious[responsIndex].response[previousIndex].lat + ", " + requestFlightResponsesPrevious[responsIndex].response[previousIndex].lng);
                    }

                    planeNames = planes.Select(a => a.name).ToList(); // get the names of all current planes
                    planeIndex = planeNames.IndexOf(name); // get the index of this plane 

                    UpdatePlanePosition(planeIndex, flightCurrent, flightPrevious, altAdjustment); // update position of the plane

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

    private float ResampleLocations(flights flightResponse, FlightsEmbeddedField flight)
    {

        float altAdjustment = 0f;
        float altInx;

        List<float> altReorderedList = reorderList(flightResponse, flightResponse.response.Average(item => item.alt));

        altInx = (float)altReorderedList.IndexOf(flight.alt) + 0.1f;

        altAdjustment = altitudeAdjustment / altInx;

        if (flight.alt == 0f)
        {
            altAdjustment = 0f;
        }

        return altAdjustment;
    }

    private IEnumerator TransitionCoroutine(Vector3 startPosition, Vector3 endPosition, Quaternion startRotation, Quaternion endRotation, float directon, int currentIndex)
    {

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            if (currentIndex < 0 || currentIndex > planes.Count - 1) yield break;

            planes[currentIndex].transform.position = Vector3.Lerp(planes[currentIndex].transform.position, endPosition, (elapsedTime / transitionDuration));
            planes[currentIndex].transform.rotation = Quaternion.Lerp(planes[currentIndex].transform.rotation, endRotation, elapsedTime / transitionDuration);
            planes[currentIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));
            planes[currentIndex].transform.Rotate(0f, 0f, directon, Space.Self);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

    }

    private void UpdatePlanePosition(int currentIndex, FlightsEmbeddedField flightCurrent, FlightsEmbeddedField flightPrevious, float altAdjustment)
    {
        Vector3 startPosition;
        Quaternion startRotation;
        Vector3 endPosition;
        Quaternion endRotation;
        Quaternion rotation;



        //current flight
        endPosition = GetXYZPositions(flightCurrent, altAdjustment);
        rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f); // adjust plane location to GlobalSystem rotation
        endPosition = GlobalSystem.transform.position + rotation * (endPosition - GlobalSystem.transform.position);
        endRotation = rotation * Quaternion.Euler(0f, -flightCurrent.dir, 0f);


        startPosition = GetXYZPositions(flightPrevious, altAdjustment);
        rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f); // adjust plane location to GlobalSystem rotation
        startPosition = GlobalSystem.transform.position + rotation * (startPosition - GlobalSystem.transform.position);
        startRotation = rotation * Quaternion.Euler(0f, -flightPrevious.dir, 0f);

        //var result2 = string.Join("; ", flightNames.Select(s => s));
        //Debug.Log(" PlanePositioning adding name " + name + " to the list list " + result2);

        //Debug.Log(" PlanePositioningUpdating name " + planes[currentIndex].name + " current " + startPosition + " end " + endPosition);
        planes[currentIndex].transform.position = startPosition;
        planes[currentIndex].transform.rotation = startRotation;

        StartCoroutine(TransitionCoroutine(startPosition, endPosition, startRotation, endRotation, -flightCurrent.dir, currentIndex));


        // plane is perpendicular to surface normal
        planes[currentIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (flightCurrent.arr_iata == "IAD")
        {
            planes[currentIndex].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }


        // update button information
        string temp = flightCurrent.reg_number + "     " + flightCurrent.lat.ToString("F3") + "      " + flightCurrent.lng.ToString("F3") + "      " + flightCurrent.alt.ToString("F0") + "     " + flightCurrent.dir.ToString("F0");
        if (flightNames[currentIndex] == flightCurrent.reg_number)
            flightButtons[currentIndex].GetComponentInChildren<TextMeshProUGUI>().text = temp;

    }

    private List<float> reorderList(flights flightResponse, float splitvalue)
    {
        // split value is a location of the Airport
        // This re-arranging allows separation of the planes for better visibility around the airport.
        // Separation coefficient is based on the distance away from othe airport, i.e., the difference between the indecies of the airport and a plane

        List<float> localList = new List<float>();

        localList = flightResponse.response.Select(flt => flt.alt).ToList();


        //splitvalue = localList.Average();
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

    public void SaveFlightCoordinates(List<string> flightInfo, flights positions, int responesIndex)
    {

        StreamWriter writer = new StreamWriter(Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_writingData_lat.json", true);
        for (int i = 0; i < positions.response.Count; i++)
        {
            if (positions.response[i].reg_number != "N34141") continue;
            string flt = positions.response[i].lat.ToString();
            //string combinedStr = flightInfo[i] + " " + flt + " " + responesIndex.ToString();
            //writer.WriteLine(combinedStr);
            writer.WriteLine(flt);

            //writer.WriteLine(flt);//writer.Write(" ");
            //writer.WriteLine(planes[i].transform.position);
        }

        //writer.WriteLine("-------------------");
        writer.Close();
    }

    public void ReadJson()
    {
        List<int> requestNumbers = new List<int>();
        flights flightResponsePrevious = new flights();
        string jsonString;
        int indxPrevious, start, end, indx;
        float dx, dy;
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

            if (i == 0)
            {
                foreach (FlightsEmbeddedField flight in flightResponse.response)
                {
                    flightNamesPrevious.Add(flight.reg_number);

                }
            }
            requestFlightResponses.Add(flightResponse);


        }



        for (int i = 0; i < requestFlightResponses.Count - 1; i++)
        {
            for (int j = 0; j < requestFlightResponses[i].response.Count; j++)
            {
                if (!names.Contains(requestFlightResponses[i].response[j].reg_number))
                    names.Add(requestFlightResponses[i].response[j].reg_number);
            }
        }

        bool first = true;
        List<string> flightsInRequest = new List<string>();
        float positionResamplingLat, positionResamplingLng;

        foreach (string name in names)
        {
            first = true;
            indices = new int[] { 0, 0, 0 };
            for (int i = 0; i < requestFlightResponsesMax; i++)
            {
                flightsInRequest = requestFlightResponses[i].response.Select(flt => flt.reg_number).ToList();
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

        ////////////////////////////
        int responseIndexPrevious = 0;
        for (int i = 0; i < requestFlightResponsesMax; i++)
        {
            first = true;
            indices = new int[] { 0, 0 };
            

            foreach (string name in names)
            {
                if (i == 0)
                {
                    requestFlightResponsesPrevious.Add(requestFlightResponses[0]);
                    //Debug.Log(" request count " + requestFlightResponses.Count + " prev " + requestFlightResponsesPrevious.Count);
                }

                if (i > 0)
                {
                    responseIndexPrevious = i - 1;
                    flightsInRequest = requestFlightResponsesPrevious[responseIndexPrevious].response.Select(flt => flt.reg_number).ToList();
                    indxPrevious = flightsInRequest.FindIndex(a => a.Contains(name));
                }
                else 
                {
                    responseIndexPrevious = 0;
                    indxPrevious = -1;
                }

                flightsInRequest = requestFlightResponses[i].response.Select(flt => flt.reg_number).ToList();
                indx = flightsInRequest.FindIndex(a => a.Contains(name));
                dx = 0f;
                dy = 0f;

                //Debug.Log(" All indeces: indx " + indx + " prev " + indxPrevious + " response prev " + responseIndexPrevious + " i " + i);
                //if (indx != -1 && indxPrevious != -1 && requestFlightResponsesPrevious.Count >= 0)
                //{
                //    Debug.Log(" JSON before name " + requestFlightResponses[i].response[indx].reg_number + " previous (" + requestFlightResponsesPrevious[responseIndexPrevious].response[indxPrevious].lat +
                //                        ", " + requestFlightResponsesPrevious[responseIndexPrevious].response[indxPrevious].lng + ") current (" + requestFlightResponses[i].response[indx].lat + ", " + requestFlightResponses[i].response[indx].lng + ")");
                //}
                
                if (indx != -1)
                {
                    //flightsInRequest = requestFlightResponsesPrevious[responseIndexPrevious].response.Select(flt => flt.reg_number).ToList();
                    //indxPrevious = flightsInRequest.FindIndex(a => a.Contains(name));

                    //if (i < flightDuration[1] + 1)
                    //{
                    if (indxPrevious != -1)
                    {
                        dx = requestFlightResponses[i].response[indx].lat - requestFlightResponsesPrevious[responseIndexPrevious].response[indxPrevious].lat;
                        dy = requestFlightResponses[i].response[indx].lng - requestFlightResponsesPrevious[responseIndexPrevious].response[indxPrevious].lng;

                    }

                    positionResamplingLat = (-listDictionary[name][2] + i) * positionSpreadAdjustment;
                    positionResamplingLng = (-listDictionary[name][2] + i) *positionSpreadAdjustment;

                    //Debug.Log("for name " + requestFlightResponses[i].response[indx].reg_number + " adjustment " + travelDistanceAdjustment * positionResamplingLat);
                    if (dx <= 0)
                    {
                        requestFlightResponses[i].response[indx].lat = requestFlightResponses[i].response[indx].lat + travelDistanceAdjustment * positionResamplingLat;
                    }
                    else
                    {
                        requestFlightResponses[i].response[indx].lat = requestFlightResponses[i].response[indx].lat - travelDistanceAdjustment * positionResamplingLat;
                    }

                    if (dy <= 0)
                    {
                        requestFlightResponses[i].response[indx].lng = requestFlightResponses[i].response[indx].lng + travelDistanceAdjustment * positionResamplingLng;
                    }
                    else
                    {
                        requestFlightResponses[i].response[indx].lng = requestFlightResponses[i].response[indx].lng - travelDistanceAdjustment * positionResamplingLng;
                    }
                    //}
                }
                if (i == 0)
                {
                    requestFlightResponsesPrevious.Clear();
                }
                else
                {
                    requestFlightResponsesPrevious.Add(requestFlightResponses[i]);
                }
            }
            //Debug.Log(" JSON after name " + requestFlightResponses[i].response[0].reg_number + " prev length " + requestFlightResponsesPrevious.Count + " " + " previous (" + requestFlightResponsesPrevious[responseIndexPrevious].response[0].lat +
            //", " + requestFlightResponsesPrevious[responseIndexPrevious].response[0].lng + ") current (" + requestFlightResponses[i].response[0].lat + ", " + requestFlightResponses[i].response[0].lng + ")");
        }
    }

    private void GeneratePreviousResponse(flights responseCurrent)
    {

        List<flights> responsePrevioius = new List<flights>();
        //flightsPrevioius.response = new List<FlightsEmbeddedField>();
        requestFlightResponsesPrevious.Add(responseCurrent);
    }
}
