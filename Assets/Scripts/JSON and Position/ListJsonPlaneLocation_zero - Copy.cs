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
public class ListJsonPlaneLocation_zero_Copy : MonoBehaviour
{

    [SerializeField] public GameObject GlobalSystem;
    [SerializeField] public GameObject marker;
    [SerializeField] public GameObject flightButtonsTemplate;
    [SerializeField] public GameObject flightButtonsParent;

    [SerializeField] public float radiusAdjustment = 1; // globe ball radius (unity units 1m)
    [SerializeField] public float airportLatitude = 38.94846f;
    [SerializeField] public float airpotLongitude = -77.44057f;

    [SerializeField] public Material haloMat;

    [SerializeField] public float altitudeScale = 1f;
    [SerializeField] public float altIndexAjustment = 1f;
    [SerializeField] public float positionScale = 0.1f;
    [SerializeField] public float indexAdjustment = 10f;

    public static List<GameObject> flightButtons;
    public static List<GameObject> planes;
    public  List<GameObject> planesPreviousLocation;
    public Vector3[] planesPreviousPosition;

    private List<string> flightNames;
    private List<string> flightNamesPrevious;

    private List<flights> requestFlightResponses;
    private flights flightResponse;

    private float latitude = 0f; // lat
    private float longitude = 0f; // lng
    private float altitude = 0f; // alt

    private float radius;
    private float newRadius;
    //private float xPos, yPos, zPos;
    
    private Vector3 newPosition;

    private List<float> latList;
    private List<float> lngList;
    private List<float> altList;

    private float latIndex;
    private float lngIndex;
    private float altIndex;

    private int donotIncludRequest = 1;
    private List<int> requestNumbers; // how many requests
    private string jsonString;

    //private int count = 0;
    public float interval = 600f;
    private float time = 0.0f;
    public float transitionDuration = 1f;
    public float speed = 0.1f;

    private int responseIndex;

    private OVRCameraRig cameraRig;
    void Start()
    {
        planes = new List<GameObject>();
        flightButtons = new List<GameObject>();
        flightNamesPrevious = new List<string>();
        flightNames = new List<string>();
        flightResponse = new flights();
        requestNumbers = new List<int>();
        requestFlightResponses = new List<flights>();
        planesPreviousLocation = new List<GameObject>();
        planesPreviousPosition = new Vector3[20];
        radius = radiusAdjustment * GlobalSystem.transform.localScale.x;

        cameraRig = FindObjectOfType<OVRCameraRig>();

        ReadJson();

    }

    public void Update()
    {
        radius = radiusAdjustment * GlobalSystem.transform.localScale.x;

        // This loop is executing PlaneLocation every interval. Right now it is set to 60sec
        for (responseIndex = 0; responseIndex < requestFlightResponses.Count; responseIndex++)
        {
            Debug.Log("Index in Update " + responseIndex);



            time += Time.deltaTime;
            while (time >= interval)
            {
                GameObject menuPanel = GameObject.Find("MenuPanel");
                //GameObject loockAtGlobal = GameObject.Find("LookAt");
                Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;
                menuPanel.transform.LookAt(cameraPosition);
                //loockAtGlobal.transform.LookAt(cameraPosition);

                flightResponse = requestFlightResponses[responseIndex];
                PlaneLocation();
                time -= interval;
            }
        }
    }

    // Remove flights that left the region from the list of active flights.
    private void RemoveFromList(int indexRemove)
    {

        if (indexRemove < 0) return;
        Destroy(planes[indexRemove]);
        planes.RemoveAt(indexRemove);

        Destroy(flightButtons[indexRemove]);
        flightButtons.RemoveAt(indexRemove);
        flightNames.RemoveAt(indexRemove);
        

        //if (planes[indexRemove] != null) Debug.Log("Plane not removed " + planes[indexRemove].name);
        //if (flightButtons[indexRemove] != null) Debug.Log("Button not removed " + flightButtons[indexRemove].name);

    }

    private void GetXYZPositions()
    {

        newRadius = (float)((float)(6.3781e6 + altitude / 0.3048) * radius / 6.3781e6);
        newPosition[0] = (newRadius) * Mathf.Cos(latitude + latIndex) * Mathf.Cos(longitude + lngIndex) + GlobalSystem.transform.position.x;
        newPosition[2] = (newRadius) * Mathf.Cos(latitude + latIndex) * Mathf.Sin(longitude + lngIndex) + GlobalSystem.transform.position.z;
        newPosition[1] = (newRadius) * Mathf.Sin(latitude + latIndex) + GlobalSystem.transform.position.y;

    }

    // If a flight doesn't exist, add it to the list of active flights
    public void AddToList(FlightsEmbeddedField flight)
    {

        flightNames.Add(flight.reg_number);

        //altitude = flight.alt * altitudeScale;
        altitude = altIndex;

        latitude = flight.lat * Mathf.Deg2Rad;
        longitude = flight.lng * Mathf.Deg2Rad;

        GetXYZPositions();

        Quaternion rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f);
        newPosition = GlobalSystem.transform.position + rotation * (newPosition - GlobalSystem.transform.position);

        planes.Add(Instantiate(marker, newPosition, Quaternion.identity, GlobalSystem.transform));


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
            Quaternion.Euler(0f,  menuPanel.transform.eulerAngles.y/2, 0f), flightButtonsParent.transform));
        flightButtons[flightNames.Count - 1].transform.Rotate(transform.up, -(180f - menuPanel.transform.eulerAngles.y / 2));
        flightButtons[flightNames.Count - 1].name = flight.reg_number;
        flightButtons[flightNames.Count - 1].tag = "Untagged";

        //Debug.Log("Add button " + flightButtons[flightNames.Count - 1].name + " number of buttons: " + flightButtons.Count);
        //Debug.Log("Add plane " + planes[flightNames.Count - 1].name + " number of planes: " + planes.Count);


        // remove scale for altitude separation in VR
        if (altitudeScale <= 0)
        {
            altitude = 0;
        }

        string temp = flight.reg_number + "    " + flight.lat.ToString("F4") + "   " + flight.lng.ToString("F4") +
            "    " + flight.alt.ToString("F0") + "    " + flight.dir.ToString("F0");

        flightButtons[flightNames.Count - 1].GetComponentInChildren<TextMeshProUGUI>().text = temp;

    }

    public void PlaneLocation()
    {

        int indexUpdate = 0;
        var tempResponseNames = new List<string>();

        var tempLatList = new List<float>();

        latList = reorderParameterList("lat", airportLatitude);
        lngList = reorderParameterList("lng", airpotLongitude);
        altList = reorderParameterList("alt", flightResponse.response.Average(item => item.alt));

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

            if (flight.lat < airportLatitude)
            {
                latIndex = -(latList.IndexOf(flight.lat) - latList.IndexOf(flight.lat) / indexAdjustment) * positionScale;
            }
            else if (flight.lat > airportLatitude)
            {
                latIndex = (latList.IndexOf(flight.lat) - latList.IndexOf(flight.lat) / indexAdjustment) * positionScale;
            }
            else
            {
                latIndex = 0;
            }

            if (flight.lng < airpotLongitude)
            {
                lngIndex = -(lngList.IndexOf(flight.lng) - lngList.IndexOf(flight.lng) / indexAdjustment) * positionScale;
            }
            else if (flight.lat > airportLatitude)
            {
                lngIndex = lngList.IndexOf(flight.lng - lngList.IndexOf(flight.lng) / indexAdjustment) * positionScale;
            }
            else
            {
                lngIndex = 0;
            }

            if (flight.alt > flightResponse.response.Average(item => item.alt))
            {
                altIndex = (altList.IndexOf(flight.alt) + altList.IndexOf(flight.alt) / altIndexAjustment) * altitudeScale;
            }
            else
            {
                altIndex = (altList.IndexOf(flight.alt) + altList.IndexOf(flight.alt) / altIndexAjustment) * altitudeScale;
            }


            // if flight is in the active list, update its position
            if (flightNames.Contains(flight.reg_number))
            {
                indexUpdate = flightNames.FindIndex(a => a.Contains(flight.reg_number));
                if (indexUpdate >= 0)
                    UpdatePlanePosition(indexUpdate, flight);
            }
            else // add a new flight to the list
            {
                AddToList(flight);
            }
        }

        for (int i = 0; i < flightNamesPrevious.Count - 1; i++)
        {
            var flight = flightNamesPrevious[i];

            if (!tempResponseNames.Contains(flight))
            {
                // if a flight left the area if interest, remove it from the list of active flights
                indexUpdate = flightNames.FindIndex(a => a.Contains(flight));
                RemoveFromList(indexUpdate);
            }
        }

        flightNamesPrevious = flightNames;

        //SaveFlightInfo(flightNames, flightNames.Count);

    }

    private IEnumerator TransitionCoroutine(Vector3 startPosition, Vector3 endPosition, int currentIndex)
    {

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            if (currentIndex < 0 || currentIndex > planes.Count-1) yield break;

            planes[currentIndex].transform.position = Vector3.Lerp(startPosition, endPosition, (elapsedTime / transitionDuration));
            elapsedTime += Time.deltaTime;
            Debug.Log(" START " + startPosition + " CURRENT " + planes[currentIndex].transform.position + " END " + endPosition);
            yield return null;
        }

    }

    private void UpdatePlanePosition(int currentIndex, FlightsEmbeddedField flight)
    {


        altitude = altIndex;
        //altitude = flight.alt + altIndex; // scale altitude to get a better separation in Oculus

        latitude = flight.lat * Mathf.Deg2Rad;
        longitude = flight.lng * Mathf.Deg2Rad;

        GetXYZPositions();

        planes[currentIndex].transform.position = newPosition;
        planes[currentIndex].transform.rotation = Quaternion.identity;

        // adjust plane location to GlobalSystem rotation
        Quaternion rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f);

        // %%%%%%%%%%%%%%
        // Attempting to smooth transition
        int previousIndex = planesPreviousLocation.FindIndex(a => a.transform.name.Contains(planes[currentIndex].name));
        Vector3 endPosition = GlobalSystem.transform.position + rotation * (planes[currentIndex].transform.position - GlobalSystem.transform.position);

        //if (previousIndex >= 0 && responseIndex > 0)
        //{
            // startPosition doesn't seem correct
            //Vector3 startPosition = planesPreviousLocation[previousIndex].transform.position;
            Vector3 startPosition = planesPreviousPosition[previousIndex];
            Vector3 diff = endPosition - startPosition;
            //string result = string.Join(", ", planesPreviousLocation.Select(s => $"{s.transform.position}"));
            //Debug.Log(" UPDATE position " + result);

            //Debug.Log(" STARTING (previous) position " + startPosition + " previous index " + previousIndex + " END (current) " + endPosition + " current index " + currentIndex + " difference " + diff);
            //StartCoroutine(TransitionCoroutine(startPosition, endPosition, currentIndex));
        //}
        //%%%%%%%%%%%%%%%%

        planes[currentIndex].transform.position = GlobalSystem.transform.position + rotation * (planes[currentIndex].transform.position - GlobalSystem.transform.position);
        planes[currentIndex].transform.rotation = rotation * planes[currentIndex].transform.rotation;

        // plane is perpendicular to surface normal
        planes[currentIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (flight.arr_iata == "DCA")
        {
            planes[currentIndex].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }

        // adjust route direction. Set to zero if grounded
        if (altitude <= 90f)
        {
            planes[currentIndex].transform.Rotate(0f, 0f, 0f, Space.Self);
        }
        else
        {
            planes[currentIndex].transform.Rotate(0f, 0f, -flight.dir, Space.Self);
        }


        //flightButtons[currentIndex].GetComponent<RectTransform>().transform.Rotate(0f, 90f, 0f, Space.Self);

        // update button information
        string temp = flight.reg_number + "     " + flight.lat.ToString("F3") + "      " + flight.lng.ToString("F3") + "      " + flight.alt.ToString("F0") + "     " + flight.dir.ToString("F0");
        if (flightNames[currentIndex] == flight.reg_number)
            flightButtons[currentIndex].GetComponentInChildren<TextMeshProUGUI>().text = temp;

    }

    private List<float> reorderParameterList(string infoType, float splitvalue)
    {

        List<float> localList;

        localList = new List<float>();

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

        //Airport location
        localList.Insert(0, splitvalue);

        //splitvalue = localList.Average();
        localList.Insert(0, splitvalue);

        localList = localList.OrderBy(x => x).ToList();



        if (infoType == "alt") return localList;

        int splitIndex = localList.IndexOf(splitvalue);

        List<float> firstHalf = localList.GetRange(0, splitIndex);
        List<float> secondHalf = localList.GetRange(splitIndex, localList.Count - splitIndex);

        firstHalf.Reverse();

        List<float> reorderedList = firstHalf.Concat(secondHalf).ToList();

        string values = String.Join(", ", reorderedList);

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

        for (int i = 0; i < requestNumbers.Count - donotIncludRequest; i++) // the last request could be corrupted
        {
            start = requestNumbers[i] - 2;
            if (i == requestNumbers.Count - donotIncludRequest)
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
