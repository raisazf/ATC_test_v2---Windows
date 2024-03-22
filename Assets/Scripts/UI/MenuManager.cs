using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction.Input;
using APIData;

namespace UI
{
    public class MenuManager : MonoBehaviour
    {
        //[SerializeField] private List<Button> flightButtons;
        private TouchScreenKeyboard overlayKeyboard;
        

        //[SerializeField] public Text inputText;

        public void OpenVirtualKeyboard()
        {
            //flightButtons = new List<Button>();
            Debug.Log("Ready to open keyboard");
            overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            Debug.Log("Where is keyboard?");

            if (overlayKeyboard != null) 
            {
                //Debug.Log("keyboard text?"+ overlayKeyboard.text.ToString());
                //inputText.text = overlayKeyboard.text; 
            }


        }

    }
}
