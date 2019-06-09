using UnityEngine;
using UnityEngine.UI;
using System;



public class ChooseOption : MonoBehaviour
{
    // How strong does an annotator have to push the stick to get another option choosen? 
    float push_intensity = LabelToolManager.threshold;
    // contain states of button B and right stick
    bool trigger_pressed = true;
    bool pushed_down = false;
    bool pushed_up = false;
    // this menu
    public GameObject Menu;
    // this menu's options
    public GameObject[] Option;
    // the related GUI elements
    public Button[] ButtonOption;
    // number of options; gets initialized in Start()
    int numOptions;
    // current choice in this menu
    public int choice = 0;

    void Start()
    {
        numOptions = Option.Length - 1;
        for (int i = 0; i <= numOptions; i++)
        {
            ButtonOption[i] = Option[i].GetComponent<Button>();
        }
    }


    // Update is called once per frame
    void Update()
    {
        // Choose state
        if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] < -push_intensity | OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] < -push_intensity) & !pushed_down)
        {
            //Debug.Log("Switched to lower option");
            choice = getLowerOption();
            ButtonOption[choice].onClick.Invoke();
            ButtonOption[choice].Select();
            pushed_down = true;
        }
        else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1] > push_intensity | OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[1] > push_intensity) & !pushed_up)
        {
            //Debug.Log("Switched to upper option");
            choice = getUpperOption();
            ButtonOption[choice].onClick.Invoke();
            ButtonOption[choice].Select();
            pushed_up = true;
        }
        else if ((Math.Abs(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1]) <= push_intensity | Math.Abs(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1]) <= push_intensity) & pushed_down)
        {
            //Debug.Log("Right stick released");
            pushed_down = false;
        }
        else if ((Math.Abs(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1]) <= push_intensity | Math.Abs(OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[1]) <= push_intensity) && pushed_up)
        {
            //Debug.Log("Right stick released");
            pushed_up = false;
        }
        if ((OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > push_intensity) && !trigger_pressed)
        {
            //Debug.Log("Next Menu");
            trigger_pressed = true;
            Debug.Log("Choice: " + choice);
            LabelToolManager.choice[LabelToolManager.activeDialog] = choice;

            LabelToolManager.activateMenu(LabelToolManager.activeDialog + 1);
        }
        else if ((OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) < push_intensity) && trigger_pressed)
        {
            Debug.Log("New Menu");
            choice = 0;
            ButtonOption[choice].onClick.Invoke();
            ButtonOption[choice].Select();
            trigger_pressed = false;
        }
    }

    int getLowerOption()
    {
        choice++;
        if (choice > numOptions)
            choice = 0;
        return choice;
    }

    int getUpperOption()
    {
        choice--;
        if (choice < 0)
            choice = numOptions;
        return choice;
    }
}
