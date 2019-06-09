using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuiInfo : MonoBehaviour
{
    // local number of tracks
    int numTracks = -1;
    Text text;
    string newestTrackString = "Newest Track: ";
    // Use this for initialization
    void Start()
    {
        text = GameObject.Find("NumberOfTracksText").GetComponent<Text>();
        text.text = newestTrackString + "No Track yet";
        numTracks = LabelToolManager.TrackID;
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            if (LabelToolManager.TrackID != numTracks)
            {
                text.text = newestTrackString + LabelToolManager.TrackID.ToString();
            }
        }
        catch
        {
            text.text = newestTrackString + "Unknown";
        }
    }
}

