using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TypeInfo : MonoBehaviour
{
    // vehicle type as an integer
    int type;
    // text field
    Text text;

    string typeString = "Type: ";

    // Use this for initialization
    void Start()
    {
        text = GameObject.Find("CurrentVehicleText").GetComponent<Text>();
        text.text = typeString + "No Type Info";
    }

    // Update is called once per frame
    void Update()
    {
        if (text.text != typeString + "No Type Info" || LabelToolManager.currentTrackID >= 0)
        {
            if (LabelToolManager.TrackID != -1 && !LabelToolManager.DialogOpen)
            {
                try
                {
                    if (LabelToolManager.trackInformationList.ElementAtOrDefault(LabelToolManager.currentTrackID) != null & type != LabelToolManager.trackInformationList[LabelToolManager.currentTrackID].getChoice()[0])
                    {
                        type = LabelToolManager.trackInformationList[LabelToolManager.currentTrackID].getChoice()[0];
                    }
                }
                catch
                {
                    type = LabelToolManager.choice[0];
                }

                switch (type)
                {
                    case 0:
                        text.text = typeString + "Pedestrian";
                        break;
                    case 1:
                        text.text = typeString + "Two Wheeler";
                        break;
                    case 2:
                        text.text = typeString + "Car";
                        break;
                    case 3:
                        text.text = typeString + "Truck";
                        break;
                    default:
                        text.text = typeString + "Error";
                        break;
                }
            }
        }
    }
}
