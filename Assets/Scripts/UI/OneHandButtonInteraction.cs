using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// This class is responsible for identifying an airplane touched be the player.
// 
// The idea is once the collider of the touched airplane is triggered the following has to happen:
// 1) the airplane material should change its "halo" to selectedMaterial
// 2) the registration name should appear over that plane

// The issues are:
// 1) More than one airplane gets triggered, although isSelected flag is set to true for the first airplane touched. 
// Another airplane should not be selected until isSelected is set to false.
// Debuggin log shows two or three airplanes with isSelected flags set to true, which I can't figure out why. 
// 2) TextMeshProUGUI text is now showing over the plane, although I'm successfully displaying text within other game objects.

public class OneHandButtonInteraction : MonoBehaviour
{
    public Material selectedMat;

    //private ListJsonPlaneLocation_zero tryPlanes;
    //private ListJsonPlaneLocation_zero currentButton;

    private Material originalMat;
    private string previousPlaneName;

    private List<GameObject> localPlanes;
    private List<string> localPlaneIndex;

    private int index;

    //private List<Renderer> planesMat;

    public void Start()
    {
        //var planesMat = new List<Renderer>();
        localPlanes = ListJsonPlaneLocation_zero.planes;
        localPlaneIndex = localPlanes.Select(p => p.name).ToList();
        index = localPlaneIndex.IndexOf(gameObject.name);

        Debug.Log("AircraftIndex " + index + " button name " + gameObject.name + " plane name " + localPlanes[index].gameObject.name);
        if (index >= 0)
        {
            originalMat = localPlanes[index].gameObject.GetComponentsInChildren<Renderer>()[2].material;
        }
    }
    public void OnTriggerEnter(Collider other)
    {


        localPlanes = ListJsonPlaneLocation_zero.planes;
        localPlaneIndex = localPlanes.Select(p => p.name).ToList();
        index = localPlaneIndex.IndexOf(gameObject.name);

        if (other.CompareTag("IndexFinger") && index>=0)
        {
            localPlanes[index].gameObject.GetComponentsInChildren<Renderer>()[2].material = selectedMat; // highlight with a different material
            localPlanes[index].gameObject.GetComponentInChildren<TextMeshPro>().text = gameObject.name; // display registration name
            localPlanes[index].gameObject.tag = "Selected";
            previousPlaneName = localPlanes[index].gameObject.name;

            Debug.Log("Sel Aircraft" + other.tag + " name: " + localPlanes[index].gameObject.name + " " + localPlanes[index].gameObject.tag);
        }
        else if (other.CompareTag("IndexFinger") && index != 0 && localPlanes[index].gameObject.name == previousPlaneName)
        {
            localPlanes[index].gameObject.GetComponentsInChildren<Renderer>()[2].material = originalMat;
            localPlanes[index].gameObject.GetComponentInChildren<TextMeshPro>().text = "";
            localPlanes[index].gameObject.tag = "Untagged";

            var colors = gameObject.GetComponent<Button>().colors;
            colors.normalColor = Color.white;
            gameObject.GetComponent<Button>().colors = colors;

            Debug.Log("Des Aircraft " + other.tag + " name: " + localPlanes[index].gameObject.name + " " + localPlanes[index].gameObject.tag);
        }
    }
}