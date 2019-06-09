using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointCloudInfo : MonoBehaviour
{

    Text text;
    // Use this for initialization
    void Start()
    {
        text = GameObject.Find("CurrentPointCloudText").GetComponent<Text>();
        text.text = "Current PC name: " + PointCloudManager.currentPCName;
    }

    // Update is called once per frame
    void Update()
    {
        text.text = "Current PC name: " + PointCloudManager.currentPCName;
    }
}
