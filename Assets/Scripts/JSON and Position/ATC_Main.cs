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
public class ATC_Main : MonoBehaviour
{

    //[SerializeField] 

    [SerializeField] private GameObject GlobalSystem;
    [SerializeField] private GameObject planePrefab;
    [SerializeField] private GameObject flightButtonsTemplate;

    [SerializeField] private float radiusAdjustment = 21; // globe ball radius (unity units 1m)
    [SerializeField] private float airportLatitude = 38.94846f;
    [SerializeField] private float airpotLongitude = -77.44057f;

    [SerializeField] private Material haloMat;

    [SerializeField] private float altitudeAdjustment = 1f;
    [SerializeField] private float positionAdjustment = 1f;


    public static List<GameObject> flightButtons;
    public static List<GameObject> planes;

    private List<string> allFlightNames;
    private FlightsEmbeddedField emptySpot;

    //private int count = 0;
    public float interval = 600f;
    private float time = 0.0f;
    public float transitionDuration = 1f;

    private Dictionary<string, List<FlightsEmbeddedField>> listDictionary;

    private OVRCameraRig cameraRig;
    void Start()
    {

        Debug.Log(" Starting");
        planes = new List<GameObject>();
        flightButtons = new List<GameObject>();
        allFlightNames = new List<string>();
        emptySpot = new FlightsEmbeddedField();
        listDictionary = new Dictionary<string, List<FlightsEmbeddedField>>();

        cameraRig = FindObjectOfType<OVRCameraRig>();

        Debug.Log(" Ready for ReadJson");
        (listDictionary, allFlightNames) = ReadJson();
        Debug.Log(" Left ReadJson");
        Updating(listDictionary, allFlightNames);
    }

    public void Update()
    {
        //time = 0;

        time += Time.deltaTime;
        while (time >= interval)
        {
            Updating(listDictionary, allFlightNames);

            GameObject menuPanel = GameObject.Find("MenuPanel");
            //GameObject lookAtGlobal = GameObject.Find("LookAt");
            Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;
            menuPanel.transform.LookAt(cameraPosition);
            //lookAtGlobal.transform.LookAt(cameraPosition);

            time -= interval;
            Debug.Log("KEEP GOING!");
        }

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


    private IEnumerator TransitionCoroutine(Vector3 startPosition, Vector3 endPosition, Quaternion startRotation, Quaternion endRotation, float directon, int currentIndex)
    {

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            if (currentIndex < 0 || currentIndex > planes.Count - 1) yield break;

            planes[currentIndex].transform.position = Vector3.Lerp(startPosition, endPosition, (elapsedTime / transitionDuration));
            planes[currentIndex].transform.rotation = Quaternion.Lerp(startRotation, endRotation, elapsedTime / transitionDuration);
            //planes[currentIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));


            //Debug.Log("Start rotation " + startRotation.eulerAngles + " End rotation " + endRotation.eulerAngles);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

    }

    public void UpdatePlaneButton(FlightsEmbeddedField flightPrevious, FlightsEmbeddedField flightCurrent, int planeIndex, float latAdjustment, float lngAdjustment, float altAdjustment)
    {

        Vector3 newPosition;
        Vector3 startPosition;
        Vector3 endPosition;
        Quaternion startRotation;
        Quaternion endRotation;
        Quaternion rotation;
        GameObject planePrevious = new GameObject();

        //Debug.Log(" Updating Previous name " + flightPrevious.reg_number + " Current name " + flightCurrent.reg_number + " index " + planeIndex);
        //Debug.Log(" Updating Previous lat " + flightPrevious.lat + " lng " + flightPrevious.lng + " alt " + flightPrevious.alt);
        //Debug.Log(" Updating Current lat " + flightCurrent.lat + " lng " + flightCurrent.lng + " alt " + flightCurrent.alt);
        rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f); // adjust for Earth GameObject rotation

        newPosition = GetXYZPositions(flightPrevious, latAdjustment, lngAdjustment, altAdjustment);
        
        planePrevious.transform.position = GlobalSystem.transform.position + rotation * (newPosition - GlobalSystem.transform.position);

        planePrevious.transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (altAdjustment == 0f)
        {
            planePrevious.transform.Rotate(0f, 0f, 0f, Space.Self);
        }
        else
        {
            planePrevious.transform.Rotate(0f, 0f, -flightPrevious.dir, Space.Self);
        }

        startPosition = planePrevious.transform.position;
        startRotation = planePrevious.transform.rotation;
        Destroy(planePrevious);

        // Current Response

        newPosition = GetXYZPositions(flightCurrent, latAdjustment, lngAdjustment, altAdjustment);

        planes[planeIndex].transform.position = GlobalSystem.transform.position + rotation * (newPosition - GlobalSystem.transform.position);

        planes[planeIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

        if (altAdjustment == 0f)
        {
            planes[planeIndex].transform.Rotate(0f, 0f, 0f, Space.Self);
        }
        else
        {
            planes[planeIndex].transform.Rotate(0f, 0f, -flightPrevious.dir, Space.Self);
        }

        endPosition = planes[planeIndex].transform.position;
        endRotation = planes[planeIndex].transform.rotation;

        StartCoroutine(TransitionCoroutine(startPosition, endPosition, startRotation, endRotation, -flightCurrent.dir, planeIndex));

        planes[planeIndex].transform.position = endPosition;
        planes[planeIndex].transform.rotation = endRotation;

        if (flightCurrent.arr_iata == "IAD")
        {
            planes[planeIndex].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }

        if (altAdjustment == 0f)
        {
            planes[planeIndex].transform.Rotate(0f, 0f, 0f, Space.Self);
        }
        else
        {
            planes[planeIndex].transform.Rotate(0f, 0f, -flightCurrent.dir, Space.Self);
        }

        //Debug.Log(" Updating Previous position " + startPosition + " curr position " + endPosition + " prev rot " + startRotation + " curr rot " + endRotation);
       // Debug.Log(" Updating Current lat " + flightCurrent.lat + " lng " + flightCurrent.lng + " alt " + flightCurrent.alt);

        if (flightCurrent.reg_number == "empty")
        {
            planes[planeIndex].SetActive(false);
        }
        else
        {
            planes[planeIndex].SetActive(true);
        }

        
        //planes[flightNames.Count - 1].GetComponentInChildren<TextMeshPro>().text = flight.reg_number; // too busy when every plane has a number displayed

        GameObject menuPanel = GameObject.Find("MenuPanel");
        Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;
        menuPanel.transform.LookAt(cameraPosition);

        if (flightCurrent.reg_number == "empty")
        {
            flightButtons[planeIndex].SetActive(false);
        }
        else
        {
            flightButtons[planeIndex].SetActive(true);
        }

        string temp = flightCurrent.reg_number + "    " + flightCurrent.lat.ToString("F4") + "   " + flightCurrent.lng.ToString("F4") +
            "    " + flightCurrent.alt.ToString("F0") + "    " + flightCurrent.dir.ToString("F0");
        flightButtons[planeIndex].GetComponentInChildren<TextMeshProUGUI>().text = temp;


    }

    public void Updating(Dictionary<string, List<FlightsEmbeddedField>> listDictionary, List<string> allFlightNames)
    {

        float latAdjustment, lngAdjustment, altAdjustment;
        int currentResponse;

        for (currentResponse = 1; currentResponse < listDictionary[allFlightNames[0]].Count - 300; currentResponse++)
        {
            //foreach (string flName in allFlightNames)
            //for (int planeIndex = 1; planeIndex < allFlightNames.Count - 1; planeIndex++)
            {
                int planeIndex = 5;
                string flName = allFlightNames[planeIndex];
                (latAdjustment, lngAdjustment, altAdjustment) = ResampleLocations(listDictionary[flName], currentResponse);


                //if (listDictionary[flName][currentResponse].reg_number == "empty") continue;

                UpdatePlaneButton(listDictionary[flName][currentResponse - 1], listDictionary[flName][currentResponse], planeIndex, latAdjustment, lngAdjustment, altAdjustment);

                //if (listDictionary[flName][currentResponse - 1].reg_number != "empty" && listDictionary[flName][currentResponse - 1].reg_number != "empty")
                //{
                //    //Debug.Log(" NOT EMPTY Updating name " + flName + " dictionary " + listDictionary[flName][currentResponse] + " index " + currentResponse);
                //    UpdatePlaneButton(listDictionary[flName][currentResponse - 1], listDictionary[flName][currentResponse], planeIndex, latAdjustment, lngAdjustment, altAdjustment);
                //}
                //else if (listDictionary[flName][currentResponse - 1].reg_number == "empty" && listDictionary[flName][currentResponse].reg_number != "empty")
                //{
                //    //Debug.Log(" Updating name " + flName + " dictionary " + listDictionary[flName][currentResponse] + " index " + currentResponse);
                //    //UpdatePlaneButton(listDictionary[flName][currentResponse], listDictionary[flName][currentResponse], planeIndex, latAdjustment, lngAdjustment, altAdjustment);
                //}
                //else if (listDictionary[flName][currentResponse - 1].reg_number != "empty" && listDictionary[flName][currentResponse].reg_number == "empty")
                //{
                //    Debug.Log(" Ever get here?");
                //    UpdatePlaneButton(listDictionary[flName][currentResponse - 1], listDictionary[flName][currentResponse - 1], planeIndex, latAdjustment, lngAdjustment, altAdjustment);
                //}
            }

            Debug.Log("currentResponse " + currentResponse);
        }

        planes[5].SetActive(true);

        //foreach (string flName in allFlightNames)
        //for (int planeIndex = 1; planeIndex < allFlightNames.Count - 1; planeIndex++)
        //{

        //    string flName = allFlightNames[planeIndex];

        //    if (listDictionary[flName][listDictionary[allFlightNames[0]].Count - 301].reg_number == "empty")
        //    {
        //        planes[planeIndex].SetActive(false);
        //    }
        //    else
        //    {
        //        planes[planeIndex].SetActive(true);
        //    }

        //}

        //var result2 = string.Join(", ", listDictionary[allFlightNames[5]].Select(s => s.lat));
        //Debug.Log(" Latitude " + allFlightNames[5] + "  " + result2);
        //result2 = string.Join(", ", listDictionary[allFlightNames[5]].Select(s => s.lng));
        //Debug.Log(" Longitude " + result2);

    }


    public void InstantiatePlaneButton(FlightsEmbeddedField flight, int planeIndex, float latAdjustment, float lngAdjustment, float altAdjustment)
    {

        Vector3 newPosition;

        newPosition = GetXYZPositions(flight, latAdjustment, lngAdjustment, altAdjustment);

        Quaternion rotation = Quaternion.Euler(0f, -(90f - GlobalSystem.transform.rotation.eulerAngles.y), 0f); // adjust for Earth GameObject rotation
        newPosition = GlobalSystem.transform.position + rotation * (newPosition - GlobalSystem.transform.position);
        planePrefab.transform.position = newPosition;

        planes.Add(Instantiate(planePrefab, newPosition, Quaternion.identity, GlobalSystem.transform));


        if (altAdjustment == 0f)
        {
            planes[planeIndex].transform.Rotate(0f, 0f, 0f, Space.Self);
        }
        else
        {
            planes[planeIndex].transform.Rotate(0f, 0f, -flight.dir, Space.Self);
        }

        planes[planeIndex].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));
        planes[planeIndex].name = flight.reg_number;
        planes[planeIndex].tag = "Untagged";

        if (flight.arr_iata == "IAD")
        {
            planes[planeIndex].gameObject.GetComponentsInChildren<Renderer>()[2].material = haloMat; // highlight with a different material
        }

        if (altAdjustment == 0f)
        {
            planes[planeIndex].transform.Rotate(0f, 0f, 0f, Space.Self);
        }
        else
        {
            planes[planeIndex].transform.Rotate(0f, 0f, -flight.dir, Space.Self);
        }

        if (flight.reg_number == "empty")
        {
            planes[planeIndex].SetActive(false);
        }
        else
        {
            planes[planeIndex].SetActive(true);
        }

        //Debug.Log(" Instantiate " + flight.reg_number + " plane name " + planes[planeIndex].name + " direction " + (-flight.dir));

        //planes[flightNames.Count - 1].GetComponentInChildren<TextMeshPro>().text = flight.reg_number; // too busy when every plane has a number displayed

        GameObject menuPanel = GameObject.Find("MenuPanel");
        Vector3 cameraPosition = cameraRig.centerEyeAnchor.transform.position;
        menuPanel.transform.LookAt(cameraPosition);

        flightButtons.Add(Instantiate(flightButtonsTemplate,
            new Vector3(flightButtonsTemplate.transform.position.x,
            flightButtonsTemplate.transform.position.y, flightButtonsTemplate.transform.position.z),
            Quaternion.Euler(0f, menuPanel.transform.eulerAngles.y / 2, 0f), flightButtonsTemplate.transform.parent.transform));

        if (flight.reg_number == "empty") flightButtons[planeIndex].SetActive(false);
        flightButtons[planeIndex].transform.Rotate(transform.up, -(180f - menuPanel.transform.eulerAngles.y / 2));
        flightButtons[planeIndex].name = flight.reg_number;
        flightButtons[planeIndex].tag = "Untagged";

        string temp = flight.reg_number + "    " + flight.lat.ToString("F4") + "   " + flight.lng.ToString("F4") +
            "    " + flight.alt.ToString("F0") + "    " + flight.dir.ToString("F0");
        flightButtons[planeIndex].GetComponentInChildren<TextMeshProUGUI>().text = temp;

    }

    public (Dictionary<string, List<FlightsEmbeddedField>> listDictionary, List<string>) Initiation(Dictionary<string, List<FlightsEmbeddedField>> listDictionary, List<string> allFlightNames)
    {

        float latAdjustment, lngAdjustment, altAdjustment;

        //foreach (string flName in allFlightNames)
        for (int i = 0; i < allFlightNames.Count - 1; i++)
        {
            //var tt = listDictionary[flName];

            //Debug.Log(" initiate " + listDictionary[flName][0].reg_number);
            //(latAdjustment, lngAdjustment, altAdjustment) = ResampleLocations(listDictionary[flName], 0);
            //InstantiatePlaneButton(listDictionary[flName][0], latAdjustment, lngAdjustment, altAdjustment);

            //Debug.Log(" initiate " + listDictionary[allFlightNames[i]][0].reg_number);
            (latAdjustment, lngAdjustment, altAdjustment) = ResampleLocations(listDictionary[allFlightNames[i]], 0);
            InstantiatePlaneButton(listDictionary[allFlightNames[i]][0], i, latAdjustment, lngAdjustment, altAdjustment);

        }

        GameObject plnPrefab = GameObject.Find("PlanePrefab");
        plnPrefab.SetActive(false);

        return (listDictionary, allFlightNames);
    }


    private (float, float, float) ResampleLocations(List<FlightsEmbeddedField> flightResponse, int Index)
    {
        float latAdjustment = 0f;
        float lngAdjustment = 0f;
        float altAdjustment = 0f;
        float latInx, lngInx, altInx;

        List<float> latReorderedList = reorderList(flightResponse, "lat", airportLatitude);
        //var result2 = string.Join(", ", latReorderedList.Select(s => s), );
        //Debug.Log(" Latitude " +  result2);
        List<float> lngReorderedList = reorderList(flightResponse, "lng", airpotLongitude);
        //result2 = string.Join(", ", lngReorderedList.Select(s => s));
        //Debug.Log(" Longitude " + result2);
        List<float> altReorderedList = reorderList(flightResponse, "alt", flightResponse.Average(item => item.alt));
        //result2 = string.Join(", ", altReorderedList.Select(s => s));
        //Debug.Log(" Altitude " + result2);

        latInx = (float)latReorderedList.IndexOf(flightResponse[Index].lat) + 0.1f;
        lngInx = (float)lngReorderedList.IndexOf(flightResponse[Index].lng) + 0.1f;
        altInx = (float)altReorderedList.IndexOf(flightResponse[Index].alt) + 0.1f;

        float latAirportInx = (float)latReorderedList.IndexOf(airportLatitude) + 0.1f;
        float lngAirportInx = (float)lngReorderedList.IndexOf(airpotLongitude) + 0.1f;

        if (latInx == 0.1) latInx = latInx + 0.2f;
        //if (lngInx == 0.1) lngInx = lngInx + 0.2f;
        //if (altInx == 0.1) altInx = altInx + 0.2f;

        if (flightResponse[Index].lat < airportLatitude)
        {
            latAdjustment = -positionAdjustment / (latAirportInx - latInx);
            var result2 = string.Join(", ", latReorderedList.Select(s => s));
            //Debug.Log(" Latitude " + result2 + " Airport lat " + latAirportInx + " Current altitude " + flightResponse[Index].lat + "Current index " + latInx + " Adjustment " + latAdjustment);
        }
        else
        {
            latAdjustment = positionAdjustment / (latInx - latAirportInx);
        }

        if (flightResponse[Index].lng < airpotLongitude)
        {
            lngAdjustment = -positionAdjustment / (lngAirportInx - lngInx);
        }
        else
        {
            lngAdjustment = positionAdjustment / lngInx;
        }

        altAdjustment = altitudeAdjustment / altInx;

        if (flightResponse[Index].alt == 0f)
        {
            latAdjustment = 0f;
            lngAdjustment = 0f;
            altAdjustment = 0f;
        }

        return (latAdjustment, lngAdjustment, altAdjustment);
    }



    private List<float> reorderList(List<FlightsEmbeddedField> flightResponse, string infoType, float splitvalue)
    {
        // split value is a location of the Airport
        // This re-arranging allows separation of the planes for better visibility around the airport.
        // Separation coefficient is based on the distance away from othe airport, i.e., the difference between the indecies of the airport and a plane

        List<float> localList = new List<float>();

        // ganerate local list for all flights based on lat, lng, or alt
        if (infoType == "lat")
        {
            localList = flightResponse.Select(flt => flt.lat).ToList();
        }
        else if (infoType == "lng")
        {
            localList = flightResponse.Select(flt => flt.lng).ToList();
        }
        else if (infoType == "alt")
        {
            localList = flightResponse.Select(flt => flt.alt).ToList();
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

    public (Dictionary<string, List<FlightsEmbeddedField>> listDictionary, List<string> allFlightNames) ReadJson()
    {
        List<int> requestNumbers = new List<int>();
        flights flightResponse = new flights();
        List<flights> individualFlightResponses = new List<flights>();
        Dictionary<string, List<FlightsEmbeddedField>> listDictionary = new Dictionary<string, List<FlightsEmbeddedField>>();

        string jsonString;

        // During the first build and run, the app won't load planes and buttons. After the first build an run, place the data file here:
        // This PC\Quest Pro\Internal shared storage\Android\data\com.DefaultCompany.ATC_test_v2\files and it should work properly
        // 

        StreamReader streamReader = new StreamReader(Application.persistentDataPath + Path.AltDirectorySeparatorChar + "AirLab_data_next.json");

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
            individualFlightResponses.Add(flightResponse);
        }

        int count = 0;
        foreach (flights fltResponse in individualFlightResponses)
        {
            if (fltResponse.response.Count == 0) continue;

            foreach (FlightsEmbeddedField flight in fltResponse.response)
            {
                if (!allFlightNames.Contains(flight.reg_number))
                {
                    allFlightNames.Add(flight.reg_number);
                }
            }
            count++;
        }

        foreach (string nm in allFlightNames)
        {
            //var result2 = string.Join(", ", listDictionary[nm].Select(s => s));
            //Debug.Log(" flihgts for name " + nm + ": " + result2);
            List<FlightsEmbeddedField> flight = new List<FlightsEmbeddedField>();
            flight.Clear();

            for (int i = 0; i < individualFlightResponses.Count - 500; i++)
            {
                var namesList = individualFlightResponses[i].response.Select(flt => flt.reg_number).ToList();
                var flightIndex = namesList.FindIndex(a => a.Contains(nm));
                if (flightIndex == -1)
                {
                    flight.Add(emptySpot); //
                }
                else
                {
                    flight.Add(individualFlightResponses[i].response[flightIndex]); //
                }

            }
            listDictionary.Add(nm, flight);
        }

        foreach (string nm in listDictionary.Keys)
        {
            // Debug.Log(" Dictionary name " + nm);
        }
        (listDictionary, allFlightNames) = Initiation(listDictionary, allFlightNames);

        return (listDictionary, allFlightNames);
    }
}
