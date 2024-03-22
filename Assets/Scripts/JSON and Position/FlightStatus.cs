using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;

// This class is responsible for identifying an airplane touched be the player.
// 

public class FlightStatus : MonoBehaviour
{
    public Material selectedMat;

    private Material originalMat;
    private string previousPlaneName;


    public void Start()
    {
        //var planesMat = new List<Renderer>();
        originalMat = gameObject.GetComponentsInChildren<Renderer>()[2].material;

    }

    public void OnTriggerEnter(Collider other)
    {

        int indxPlane = ListJsonPlaneLocation_zero.planes.FindIndex(plane => plane.tag == "Selected");
        int indxButton = ListJsonPlaneLocation_zero.flightButtons.FindIndex(button => button.tag == "Activated");

        // select plane and button
        if (other.CompareTag("IndexFinger") && indxPlane == -1 && indxButton == -1 && gameObject.name != "PlaneHolderInside")
        {
            originalMat = gameObject.GetComponentsInChildren<Renderer>()[2].material;

            gameObject.GetComponentsInChildren<Renderer>()[2].material = selectedMat; // highlight with a different material
            gameObject.GetComponentInChildren<TextMeshPro>().text = gameObject.name; // display registration name
            gameObject.tag = "Selected";

            indxPlane = ListJsonPlaneLocation_zero.planes.FindIndex(plane => plane.tag == "Selected");

            ListJsonPlaneLocation_zero.flightButtons[indxPlane].tag = "Activated";
            //ListJsonPlaneLocation_zero.flightButtons[indxPlane].Select();

            var colors = ListJsonPlaneLocation_zero.flightButtons[indxPlane].GetComponent<Button>().colors;
            colors.pressedColor = new Color(0f, 0f, 1f, 0.34f);
            colors.selectedColor = new Color(0f, 0f, 1f, 0.34f);
            ListJsonPlaneLocation_zero.flightButtons[indxPlane].GetComponent<Button>().colors = colors;

            previousPlaneName = gameObject.name;

        }
        //Deselect plane and button
        else if (other.CompareTag("IndexFinger") && indxPlane != -1 && indxButton != -1 && gameObject.name == previousPlaneName)
        {
            ListJsonPlaneLocation_zero.planes[indxPlane].GetComponentsInChildren<Renderer>()[2].material = originalMat;
            ListJsonPlaneLocation_zero.planes[indxPlane].GetComponentInChildren<TextMeshPro>().text = "";
            ListJsonPlaneLocation_zero.planes[indxPlane].tag = "Untagged";

            ListJsonPlaneLocation_zero.flightButtons[indxButton].tag = "Untagged";
            var colors = ListJsonPlaneLocation_zero.flightButtons[indxButton].GetComponent<Button>().colors;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            ListJsonPlaneLocation_zero.flightButtons[indxButton].GetComponent<Button>().colors = colors;

        }
        else if ((indxPlane != -1 && indxButton == -1 && gameObject.name != previousPlaneName) || (indxPlane == -1 && indxButton != -1 && gameObject.name != previousPlaneName))
        {
            // reset all planes
            foreach (var plane in ListJsonPlaneLocation_zero.planes)
            {
                plane.gameObject.GetComponentsInChildren<Renderer>()[2].material = originalMat;
                plane.gameObject.GetComponentInChildren<TextMeshPro>().text = "";
                plane.gameObject.tag = "Untagged";

                indxPlane = -1;
            }
            //reset all buttons
            foreach (var button in ListJsonPlaneLocation_zero.flightButtons)
            {
  
                button.tag = "Untagged";

                var colors = button.GetComponent<Button>().colors;
                colors.pressedColor = Color.white;
                colors.selectedColor = Color.white;
                button.GetComponent<Button>().colors = colors;

                indxButton = -1;

            }
        }
    }
}