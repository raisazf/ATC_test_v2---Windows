using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TMPro;

public class PlaneLocation : MonoBehaviour
{
    // Start is called before the first frame update
    //public GameObject marker;
    public GameObject GlobalSystem;
    public GameObject marker;
    public float radius = 1.0095f; // globe ball radius (unity units)
    public float latitude = 0f; // lat
    public float longitude = 0f; // long
    public float altitude = 0f;
    public float direction = 0f;
    private OVRVirtualKeyboard overlayKeyboard;
    [SerializeField] public GameObject flightButtonsTemplate;
    [SerializeField] public GameObject flightButtonsParent;
    [SerializeField] public GameObject[] flightButtons;
    [SerializeField] public GameObject flightsInfo;
    //[SerializeField] public GameObject flightPanel;
    private int numplanes = 3;

    void Start()
    {
        var planes = new GameObject[numplanes];
        var flightButtons = new GameObject[numplanes];

        //GameObject sphereTry = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        for (int i = 0; i < numplanes; i++)
        {
            //Instantiate(sphereTry, Vector3.zero, Quaternion.identity);
            //Instantiate(marker, Vector3.zero, Quaternion.identity, parent.transform);
            //Instantiate(marker, Vector3.zero, Quaternion.identity);

            latitude = 38.94846f + Random.Range(-1.0f, 1.0f);
            longitude = -77.44057f + Random.Range(-1.0f, 1.0f);

            latitude = Mathf.PI * latitude / 180;
            longitude = Mathf.PI * longitude / 180;

            altitude = altitude + Random.Range(0f, 50000f);
            direction = -direction + Random.Range(-360f, 0f);
            altitude = 0;
            float newRadius = (float)((float)(2.093e7 + altitude) * radius / 2.093e7);
            float xPos = (newRadius) * Mathf.Cos(latitude) * Mathf.Cos(longitude) + GlobalSystem.transform.position.x;
            float zPos = (newRadius) * Mathf.Cos(latitude) * Mathf.Sin(longitude) + GlobalSystem.transform.position.z;
            float yPos = (newRadius) * Mathf.Sin(latitude) + GlobalSystem.transform.position.y ;
            

            //GameObject plane = Instantiate(marker, new Vector3(xPos, yPos, zPos), Quaternion.Euler(0, i*direction, 0 ), parent.transform);
            planes[i] = Instantiate(marker, new Vector3(xPos, yPos, zPos), Quaternion.identity, GlobalSystem.transform);
            planes[i].tag = "Untagged";

            //planes[i].transform.LookAt(Vector3.zero);
            planes[i].transform.LookAt(new Vector3(GlobalSystem.transform.position.x, GlobalSystem.transform.position.y, GlobalSystem.transform.position.z));

            if (altitude <= 0f)
            {
                planes[i].transform.Rotate(0f, 0f, 0f, Space.Self);
            }
            else
            {
                planes[i].transform.Rotate(0f, 0f, direction, Space.Self);
            }
            Debug.Log(message: $"Plane altitude {latitude*180/Mathf.PI}, {longitude * 180 / Mathf.PI}, {altitude}, {direction}");

            flightButtons[i] = Instantiate(flightButtonsTemplate, new Vector3(flightButtonsTemplate.transform.position.x, flightButtonsTemplate.transform.position.y, flightButtonsTemplate.transform.position.z), Quaternion.Euler(0f,0f,0f), flightButtonsParent.transform);
            //if (i > 0) flightButtons[i].transform.position = new Vector3(flightButtons[i].transform.position.x, flightButtons[i-1].transform.position.y-0.05f, flightButtons[i].transform.position.z);
            

            latitude = latitude * 180 / Mathf.PI;
            longitude = longitude * 180 / Mathf.PI;
            string temp = "N68821" + "    " + latitude.ToString("F4") + "   " + longitude.ToString("F4") + "    " + altitude.ToString("F0") + "    " + newRadius.ToString("F0");
            flightsInfo.GetComponent<TextMeshProUGUI>().text = temp;

            Debug.Log("keyboard is activated outside");
            //overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            //overlayKeyboard.gameObject.SetActive(false);
        }
    }


    // Update is called once per frame
    public void Keyboard()
    {
        Debug.Log("keyboard is activated outside");
        //overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        overlayKeyboard.TextCommitField.keyboardType = (TouchScreenKeyboardType)(-1);
        if (overlayKeyboard != null)
            //inputText = overlayKeyboard.text;
            Debug.Log("keyboard is activated");

    }
}
