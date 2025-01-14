using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// This class is responsible for identifying an airplane based on the button clicked.
// Below is the comment about the issue in CAPS


public class ButtonPlaneInteraction : MonoBehaviour
{
    public Material selectedMat;


    private Material originalMat;
    private string previousButtonName;

    private OVRCameraRig cameraRig;

    //private List<Button> localButtons;
    private Button clickedButton;
    //private List<GameObject> localPlanes;

    private ColorBlock colors;
    private void Awake()
    {
        previousButtonName = "";
    }
    public void Start()
    {
        foreach (GameObject button in ListJsonPlaneLocation_zero.flightButtons)
        {
            // Add a listener to the button's onClick event
            button.GetComponent<Button>().onClick.AddListener(() => ButtonClicked(button.GetComponent<Button>()));
        }
        cameraRig = FindObjectOfType<OVRCameraRig>();

        //var planesMat = new List<Renderer>();
        //originalMat = gameObject.GetComponentsInChildren<Renderer>()[2].material;
    }

    public void ButtonClicked(Button button)
    {
        // Output the name of the clicked button
        Debug.Log("Aircraft Clicked button: " + clickedButton.name + " Clicked button tag: " + clickedButton.tag);
        //SelectPlane();
    }
    public void SelectPlane()
    {

        
        int indxPlane = ListJsonPlaneLocation_zero.planes.FindIndex(plane => plane.tag == "Selected");
        int indxButton = ListJsonPlaneLocation_zero.flightButtons.FindIndex(button => button.tag == "Activated");

        Debug.Log("Aircraft SelectPlane() indxPlane: " + indxPlane + " indxButton: " + indxButton + " Previous button name " + previousButtonName);

        if (indxPlane == -1 && indxButton == -1) // select the corresponding plane
        {
            Debug.Log("Aircraft original selection 1");
            gameObject.tag = "Activated";
            indxButton = ListJsonPlaneLocation_zero.flightButtons.FindIndex(button => button.tag == "Activated");

            originalMat = ListJsonPlaneLocation_zero.planes[indxButton].gameObject.GetComponentsInChildren<Renderer>()[2].material;

            Debug.Log("Aircraft original selection 2 ");

            ListJsonPlaneLocation_zero.planes[indxButton].gameObject.GetComponentsInChildren<Renderer>()[2].material = selectedMat; // highlight with a different material
            ListJsonPlaneLocation_zero.planes[indxButton].gameObject.GetComponentInChildren<TextMeshPro>().text = gameObject.name; // display registration name
            ListJsonPlaneLocation_zero.planes[indxButton].gameObject.tag = "Selected";

            Debug.Log("Aircraft original selection 3 ");

            colors = ListJsonPlaneLocation_zero.flightButtons[indxButton].GetComponent<Button>().colors;
            colors.pressedColor = new Color(0f, 0f, 1f, 0.34f);
            colors.selectedColor = new Color(0f, 0f, 1f, 0.34f);
            ListJsonPlaneLocation_zero.flightButtons[indxButton].GetComponent<Button>().colors = colors;

            Debug.Log("Aircraft original selection 4 ");

            previousButtonName = gameObject.name;

            Debug.Log("Keep Aircraft clicked button tag: " + gameObject.tag + "Activated" + ListJsonPlaneLocation_zero.flightButtons[indxButton].name + " button tag " + ListJsonPlaneLocation_zero.flightButtons[indxPlane].tag);

        }
        else if (indxPlane != -1 && indxButton != -1 && gameObject.tag != "Activated")  // highlight the correct button if the wrong one is clicked
        {

            Debug.Log("Aircraft keep selection 1");

            indxButton = ListJsonPlaneLocation_zero.flightButtons.FindIndex(button => button.tag == "Activated");

            Debug.Log("Aircraft keep selection 2");
            colors = ListJsonPlaneLocation_zero.flightButtons[indxButton].GetComponent<Button>().colors;
            colors.pressedColor = Color.red;
            colors.selectedColor = Color.red;
            ListJsonPlaneLocation_zero.flightButtons[indxButton].GetComponent<Button>().colors = colors;
            Debug.Log("Aircraft keep selection 3");

            colors = gameObject.GetComponent<Button>().colors;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            gameObject.GetComponent<Button>().colors = colors;
            Debug.Log("Aircraft keep selection 4");

            ListJsonPlaneLocation_zero.flightButtons[indxButton].GetComponent<Button>().Select();

            Debug.Log("Keep Aircraft button name " + ListJsonPlaneLocation_zero.flightButtons[indxButton].name + " button tag " + ListJsonPlaneLocation_zero.flightButtons[indxPlane].tag);

        }
        else if (indxPlane != -1 && indxButton != -1 && gameObject.tag == "Activated") // Deselect the plane and return button to its default color
        {
            ListJsonPlaneLocation_zero.planes[indxButton].gameObject.GetComponentsInChildren<Renderer>()[2].material = originalMat;
            ListJsonPlaneLocation_zero.planes[indxButton].gameObject.GetComponentInChildren<TextMeshPro>().text = "";
            ListJsonPlaneLocation_zero.planes[indxButton].gameObject.tag = "Untagged";

            gameObject.tag = "Untagged";

            //localButtons[indxPlane].gameObject.GetComponent<Button>().
            var colors = ListJsonPlaneLocation_zero.flightButtons[indxPlane].GetComponent<Button>().colors;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            ListJsonPlaneLocation_zero.flightButtons[indxPlane].GetComponent<Button>().colors = colors;

            previousButtonName = "";
            Debug.Log("Desactivate Aircraft  plane name " + ListJsonPlaneLocation_zero.planes[indxButton].name + " plane tag " + ListJsonPlaneLocation_zero.planes[indxPlane].gameObject.tag);
        }
        //else if ((indxPlane != -1 && indxButton == -1 && gameObject.tag == "Activated") || (indxPlane == -1 && indxButton != -1 && gameObject.tag == "Activated")) // reset everything
        //{

        //    // reset all planes
        //    foreach (var plane in ListJsonPlaneLocation_zero.planes)
        //    {
        //        plane.gameObject.GetComponentsInChildren<Renderer>()[2].material = originalMat;
        //        plane.gameObject.GetComponentInChildren<TextMeshPro>().text = "";
        //        plane.gameObject.tag = "Untagged";

        //        indxPlane = -1;
        //    }
        //    //reset all buttons
        //    foreach (var button in ListJsonPlaneLocation_zero.flightButtons)
        //    {

        //        button.tag = "Untagged";

        //        var colors = button.GetComponent<Button>().colors;
        //        colors.pressedColor = Color.white;
        //        colors.selectedColor = Color.white;
        //        button.GetComponent<Button>().colors = colors;

        //        indxButton = -1;

        //    }
        //}

    }
}