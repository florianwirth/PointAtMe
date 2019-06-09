using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrackInfo : MonoBehaviour
{
    // text field
    Text text;
    // local current track id
    int currentTrackID;
    string trackString = "Track: ";
    // Use this for initialization
    void Start()
    {
        text = GameObject.Find("CurrentTrackText").GetComponent<Text>();
        currentTrackID = -1;
        text.text = trackString + "No current track yet";
    }

    // Update is called once per frame
    void Update()
    {
        if (text.text != trackString + "No current track yet" || LabelToolManager.currentTrackID >= 0)
        {
            if (LabelToolManager.currentTrackID != currentTrackID && LabelToolManager.TrackID != -1)
            {
                currentTrackID = LabelToolManager.currentTrackID;
                text.text = trackString + currentTrackID.ToString();
            }
        }
    }
}


