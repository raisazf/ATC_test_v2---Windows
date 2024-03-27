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
    [SerializeField] private float positionAdjustment = 1f;

    
    public static List<GameObject> flightButtons;
    public static List<GameObject> planes;

    
    private List<string> flightNames;
    private List<string> flightNamesPrevious;
    private List<GameObject> planesPreviousLocation;
    private List<flights> requestFlightResponses;
    private flights flightResponse;

    private List<GameObject> planesPreviousPositionList;

    //private int count = 0;
    public float interval = 600f;
    private float time = 0.0f;
    public float transitionDuration = 1f;
    public float speed = 0.1f;

    private OVRCameraRig cameraRig;
    void Start()
    {

        planes = new List<GameObject>();
        flightButtons = new List<GameObject>();
        flightNamesPrevious = new List<string>();
        flightNames = new List<string>();
        flightResponse = new flights();
        requestFlightResponses = new List<flights>();
        planesPreviousPositionList = new List<GameObject>();
        cameraRig = FindObjectOfType<OVRCameraRig>();

        ReadJson();

    }

    public void Update()
    {
        float radius = radiusAdjustment * GlobalSystem.transform.localScale.x;

        // This loop is executing PlaneLocation every interval. Right now it is set to 60sec
        for (int responseIndex = 0; responseIndex < requestFlightResponses.Count; responseIndex++)
        {

            time += Time.deltaTime;
            while (time >= interval)
            {
                GameObject menuPanel = GameObject.Find("MenuPanel");
                //GameObject lookAtGlobal = GameObject.Find("LookAt");
                Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;
                menuPanel.transform.LookAt(cameraPosition);
                //lookAtGlobal.transform.LookAt(cameraPosition);

                flightResponse = requestFlightResponses[responseIndex];
                PlanePosition();
                time -= interval;
            }
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
    private Vector3 GetXYZPositions(FlightsEmbeddedField flight, float latAdjustment, float lngAdjustment, float altAdjustment)
    {
        Vector3 nPosition = new Vector3();
        float newRadius;
        float radius = radiusAdjustment * GlobalSystem.transform.localScale.x;

        newRadius = (float)((float)(6.3781e6 + flight.lat * Mathf.Deg2Rad / 0.3048) * radius / 6.3781e6);
        nPosition[0] = (newRadius) * Mathf.Cos((flight.lat + latAdjustment) * Mathf.Deg2Rad) * Mathf.Cos((flight.lng + lngAdjustment) * Mathf.Deg2Rad) + GlobalSystem.transform.position.x;
        nPosition[2] = (newRadius) * Mathf.Cos((flight.lat + latAdjustment) * Mathf.Deg2Rad) * Mathf.Sin((flight.lng + lngAdjustment) * Mathf.Deg2Rad) + GlobalSystem.transform.position.z;
        nPosition[1] = (newRadius + altAdjustment) * Mathf.Sin((flight.lat + latAdjustment) * Mathf.Deg2Rad) + GlobalSystem.transform.position.y;

        return nPosition;
    }

    // If flight just entered zone of interest, add it to the list of active flights
    public void AddFlight(FlightsEmbeddedField flight, float latAdjustment, float lngAdjustment, float altAdjustment)
    {

        Vector3 newPosition;

        flightNames.Add(flight.reg_number);

        newPosition = GetXYZPositions(flight, latAdjustment, lngAdjustment, altAdjustment);

        Quaternion rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f); // adjust for Earth GameObject rotation
        newPosition = GlobalSystem.transform.position + rotation * (newPosition - GlobalSystem.transform.position);
        planePrefab.transform.position = newPosition;

        planes.Add(Instantiate(planePrefab, newPosition, Quaternion.identity, GlobalSystem.transform));

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

    public void PlanePosition()
    {
        Vector3[] planesPreviousPositionArray = new Vector3[20];
        
        int indexUpdate = 0;
        var tempResponseNames = new List<string>();
        float latAdjustment, lngAdjustment, altAdjustment;

        foreach (var flight in flightResponse.response)
        {
            tempResponseNames.Add(flight.reg_number);
        }

        foreach (var plane in planes)
        {
            Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;

            plane.transform.GetChild(2).transform.LookAt(cameraPosition);
            //plane.transform.GetChild(2).GetChild(0).transform.LookAt(GlobalSystem.transform.position);
        }

        foreach (var flight in flightResponse.response)
        {
            (latAdjustment, lngAdjustment, altAdjustment) = ResampleLocations(flight);

            // if flight is in the active list, update its position
            if (flightNames.Contains(flight.reg_number))
            {
                indexUpdate = flightNames.FindIndex(a => a.Contains(flight.reg_number));
                if (indexUpdate >= 0)
                    UpdatePlanePosition(indexUpdate, flight, planesPreviousPositionList, latAdjustment, lngAdjustment, altAdjustment);
            }
            else // add a new flight to the list
            {
                AddFlight(flight, latAdjustment, lngAdjustment, altAdjustment);
            }
        }

        for (int i = 0; i < flightNamesPrevious.Count - 1; i++)
        {
            var flight = flightNamesPrevious[i];

            if (!tempResponseNames.Contains(flight))
            {
                // if a flight left the area if interest, remove it from the list of active flights
                indexUpdate = flightNames.FindIndex(a => a.Contains(flight));
                RemoveFlight(indexUpdate);
            }
        }

        flightNamesPrevious = flightNames;
        planesPreviousPositionList = new List<GameObject>(planes.Select(x => x));

        //var result2 = string.Join(", ", planesPreviousPositionList.Select(s => s.transform.position));
        //Debug.Log(" Previous position in PlanePosition" + result1 + " after " + result2);
        //planesPreviousPositionArray = planes.Select(s => s.transform.position).ToArray();

        //SaveFlightInfo(flightNames, flightNames.Count);

    }

    private (float, float, float) ResampleLocations(FlightsEmbeddedField flight)
    {
        float latAdjustment = 0f;
        float lngAdjustment = 0f;
        float altAdjustment = 0f;
        float latInx, lngInx, altInx;

        List<float> latReorderedList = reorderList("lat", airportLatitude);
        List<float> lngReorderedList = reorderList("lng", airpotLongitude);
        List<float> altReorderedList = reorderList("alt", flightResponse.response.Average(item => item.alt));

        latInx = (float)latReorderedList.IndexOf(flight.lat)+0.1f;
        lngInx = (float)lngReorderedList.IndexOf(flight.lng)+0.1f;
        altInx = (float)altReorderedList.IndexOf(flight.alt)+0.1f;

        float latAirportInx = (float)latReorderedList.IndexOf(airportLatitude)+0.1f;
        float lngAirportInx = (float)lngReorderedList.IndexOf(airpotLongitude)+0.1f;

        if (latInx == 0.1) latInx = latInx + 0.2f;
        //if (lngInx == 0.1) lngInx = lngInx + 0.2f;
        //if (altInx == 0.1) altInx = altInx + 0.2f;

        if (flight.lat < airportLatitude)
        {
            latAdjustment = -positionAdjustment / latInx;
        }
        else
        {
            latAdjustment = positionAdjustment / (latInx - latAirportInx);
        }

        if (flight.lng < airpotLongitude)
        {
            lngAdjustment = -positionAdjustment / (lngAirportInx - lngInx);
        }
        else
        {
            lngAdjustment = positionAdjustment / lngInx; 
        }

        altAdjustment = altitudeAdjustment / altInx;

        if (flight.alt < 700f) 
        {
            latAdjustment = 0f;
            lngAdjustment = 0f;
            altAdjustment = 0f;
        } 

        return (latAdjustment, lngAdjustment, altAdjustment);
    }

    private IEnumerator TransitionCoroutine(Vector3 startPosition, Vector3 endPosition, int currentIndex)
    {

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            if (currentIndex < 0 || currentIndex > planes.Count - 1) yield break;

            planes[currentIndex].transform.position = Vector3.Lerp(startPosition, endPosition, (elapsedTime / transitionDuration));
            elapsedTime += Time.deltaTime;

            yield return null;
        }

    }

    private void UpdatePlanePosition(int currentIndex, FlightsEmbeddedField flight, List<GameObject> planesPreviousPositionList, float latAdjustment, float lngAdjustment, float altAdjustment)
    {
        Vector3 newPosition;
        Vector3 startPosition;
        Vector3 endPosition;

        newPosition = GetXYZPositions(flight, latAdjustment, lngAdjustment, altAdjustment);

        planes[currentIndex].transform.position = newPosition;
        planes[currentIndex].transform.rotation = Quaternion.identity;

        // adjust plane location to GlobalSystem rotation
        Quaternion rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f);

        // Attempting to smooth transition
        int previousIndex = planesPreviousPositionList.FindIndex(a => a.transform.name.Contains(planes[currentIndex].name));
        endPosition = GlobalSystem.transform.position + rotation * (planes[currentIndex].transform.position - GlobalSystem.transform.position);
        Debug.Log("Update previous index " + previousIndex);
        if (previousIndex >= 0)
        {
            startPosition = planesPreviousPositionList[previousIndex].transform.position;
        }
        else {
            startPosition = endPosition;
        }
            
        //Vector3 startPosition = planesPreviousPositionArray[previousIndex];
        //StartCoroutine(TransitionCoroutine(startPosition, endPosition, currentIndex));

        planes[currentIndex].transform.position = GlobalSystem.transform.position + rotation * (planes[currentIndex].transform.position - GlobalSystem.transform.position);
        planes[currentIndex].transform.rotation = rotation * planes[currentIndex].transform.rotation;

        // plane is perpendicular to surface normal
        planes[currentIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (flight.arr_iata == "IAD")
        {
            planes[currentIndex].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }

        // adjust route direction. Set to zero if grounded
        if (altAdjustment == 0f)
        {
            planes[currentIndex].transform.Rotate(0f, 0f, 0f, Space.Self);
        }
        else
        {
            planes[currentIndex].transform.Rotate(0f, 0f, -flight.dir, Space.Self);
        }

        // update button information
        string temp = flight.reg_number + "     " + flight.lat.ToString("F3") + "      " + flight.lng.ToString("F3") + "      " + flight.alt.ToString("F0") + "     " + flight.dir.ToString("F0");
        if (flightNames[currentIndex] == flight.reg_number)
            flightButtons[currentIndex].GetComponentInChildren<TextMeshProUGUI>().text = temp;

    }
   
    private List<float> reorderList(string infoType, float splitvalue)
    {
        // split value is a location of the Airport
        // This re-arranging allows separation of the planes for better visibility around the airport.
        // Separation coefficient is based on the distance away from othe airport, i.e., the difference between the indecies of the airport and a plane

        List<float> localList = new List<float>();
        
        // ganerate local list for all flights based on lat, lng, or alt
        if (infoType == "lat")
        {
            localList = flightResponse.response.Select(flt => flt.lat).ToList();
        }
        else if (infoType == "lng")
        {
            localList = flightResponse.response.Select(flt => flt.lng).ToList();
        }
        else if (infoType == "alt")
        {
            localList = flightResponse.response.Select(flt => flt.alt).ToList();
        }

        //splitvalue = localList.Average();
        localList.Insert(0, splitvalue);

        localList = localList.OrderBy(x => x).ToList();



        if (infoType == "alt" || infoType == "lng") return localList;

        int splitIndex = localList.IndexOf(splitvalue);

        List<float> firstHalf = localList.GetRange(0, splitIndex);
        List<float> secondHalf = localList.GetRange(splitIndex, localList.Count - splitIndex);

        firstHalf.Reverse();

        List<float> reorderedList = firstHalf.Concat(secondHalf).ToList();

        return reorderedList;
    }

    public void SaveFlightInfo(List<string> flightInfo, int indx)
    {

        StreamWriter writer = new StreamWriter("C:/Users/raisa/Documents/GitHub/ATC_test_v2/Assets/Resources/TryingToWrite.txt", true);
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

    // During the first build and run, the app won't load planes and buttons. After the first build an run, place the data file here:
    // This PC\Quest Pro\Internal shared storage\Android\data\com.DefaultCompany.ATC_test_v2\files and it should work properly
    // 

    StreamReader streamReader = new StreamReader(Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_data_new.json");
        var path = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_data_new.json";

        streamReader.BaseStream.Position = 0;
        jsonString = streamReader.ReadToEnd();
        streamReader.Close();


        Int32 next = 0;

        // break json line (the entire request) into individual responses
        for (int i = 1; i < jsonString.Length; i++)
        {
            Int32 indx = jsonString.IndexOf("res", i);
            if (indx > next)
            {
                requestNumbers.Add(indx);
                next = indx;
            }

        }

        // break each response into individual flights. 
        int start = 0;
        int end = 0;

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
    }

}
