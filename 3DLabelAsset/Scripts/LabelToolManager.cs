using System.Collections.Generic;
using UnityEngine;

public class LabelToolManager : MonoBehaviour
{
    // path to pcd data
    //static string path: it needs to lead to the folder of the unity project (i.e. the folder "Assets" is within this folder)

    static string path = "C:/Users/wirth/Documents/VR_devel/VR_test_aktuell/"; 
    //static string path = @"U:\VR_dev\VR_test_aktuell_BUGFIX/";

    // path to raw data
    public static string PathToData = path + "Assets/RecordedData_KITTI";
    // path to labels
    public static string PathToLabels = path + "Assets/Labels";
    // path to pcd meshes
    public static string PathToPCLMeshs = path + "Assets/Resources/PointCloudMeshes";
    // array of pcd files in pcd folder
    public static string[] fileNamesPCD;
    // array of images in image folders
    public static string[] fileNamesImg;
    // list of subfolders
    List<string> subfolders = new List<string> { "pcd", "front", "rear", "left", "right" };

    //integer value ratio of pcd recording frequancy (i.e. 10Hz) to image recording frequency (i.e. 10Hz), i.e ratio = 1:
    private int RecordingFrequencyRatio;
    private int numOfImages;

    // thumbstick push intensity to trigger events:
    public static float threshold = 0.2f;
    // small jump in sequence (left thumbstick left or right)
    public static int small_jump = 1;
    // large jump in sequence (left thumbsitck up or down)
    public static int large_jump = 3;
    // scale factors
    public static List<float> scaleFactors = new List<float> { 0.1f, 0.01f };
    // current scale
    public static int current_scale_idx = 0;

    // how often is the trigger pulled?
    public static int trigger_count = 0;
    // is right index trigger pressed?
    bool right_index_trigger_pressed = false;
    // is left thumbstick pressed?
    bool left_thumbstick_pressed = false;
    // is the track choice done?
    public static bool trackChoiceDone = false;
    // Was X just pressed?
    public static bool X_pressed = false;

    // Dialogs
    static GameObject NewTrackDialogs;
    static GameObject BoxCollection;
    static GameObject QualityDialog;

    // Total number of tracks
    public static int TrackID = -1;
    // number of track currently chosen
    public static int currentTrackID = 0;
    // number of scene currently loaded
    public static int SequenceIdx = 0;

    // Is new track dialog currently open?
    public static bool NewTrackDialogOpen = false;
    // Is quality dialog currently open?
    public static bool QualityDialogOpen = false;
    // Any dialog open?
    public static bool DialogOpen = false;

    // each menu is a gameobject in this array, order equals order of appearence
    static public GameObject[] menu = new GameObject[5];
    // final choices of annotators
    static public int[] choice = new int[6];
    // this variable states which menu is currently active
    static public int activeDialog = 0;

    // Which dialog is currently active? 
    bool newTracksDialogActive = false;
    bool QualityDialogActive = false;
    // list with track information
    public static List<TrackInformation> trackInformationList = new List<TrackInformation>();

    // Use this for initialization
    void Start()
    {
        //Assign non public game objects
        NewTrackDialogs = GameObject.Find("NewTrackDialogs");
        BoxCollection = GameObject.Find("BoxCollection");
        QualityDialog = GameObject.Find("QualityDialog");

        menu[0] = GameObject.Find("VehicleTypeDialog");
        menu[1] = GameObject.Find("PriorityDialog");
        menu[2] = GameObject.Find("DirectionDialog");
        menu[3] = GameObject.Find("IsParkingDialog");
        menu[4] = GameObject.Find("LaneDialog");

        for (int i = 0; i < menu.Length; i++)
        {
            menu[i].SetActive(false);
        }

        foreach (string subfolder in subfolders)
        {
            if (System.IO.Directory.Exists(LabelToolManager.PathToData + "/" + subfolder))
            {
                if (subfolder == "pcd")
                {
                    fileNamesPCD = System.IO.Directory.GetFiles(LabelToolManager.PathToData + "/" + subfolder + "/", "*.pcd");
                    if (fileNamesPCD.Length > 0)
                        fileNamesPCD = ReadNames(fileNamesPCD);
                    else
                        Debug.LogError("No .pcd files found in directory: " + LabelToolManager.PathToData + "/" + subfolder + "/");
                }
                else
                {
                    fileNamesImg = System.IO.Directory.GetFiles(LabelToolManager.PathToData + "/" + subfolder + "/", "*.png");
                    if (fileNamesImg.Length > 0)
                        fileNamesImg = ReadNames(fileNamesImg);
                    else
                        Debug.LogError("No .png files found in directory: " + LabelToolManager.PathToData + "/" + subfolder + "/");
                }
            }
            else
            {
                Debug.Log("Directory: " + LabelToolManager.PathToData + "/" + subfolder + "/" + " does not exist");
            }
        }
        numOfImages = fileNamesImg.Length;
        if ( numOfImages != 0 && PointCloudManager.numOfPointClouds != 0)
        {
            RecordingFrequencyRatio = numOfImages / PointCloudManager.numOfPointClouds;
            Debug.Log("Ratio of image to PointCloud frequencies : " + RecordingFrequencyRatio);
        }
        else
        {
            Debug.Log("Either Images or PointCloud folder is empty");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        DialogOpen = NewTrackDialogs.activeSelf | QualityDialog.activeSelf;
        NewTrackDialogOpen = NewTrackDialogs.activeSelf & !QualityDialog.activeSelf;
        QualityDialogOpen = !NewTrackDialogs.activeSelf & QualityDialog.activeSelf;

        if (OVRInput.Get(OVRInput.RawButton.B) && !DialogOpen)
        {
            Debug.Log("Activate New Track Menu");
            NewTrackDialogs.SetActive(true);
            activateMenu(activeDialog);
            trackChoiceDone = false;
            //NewTrackDialogOpen = true;
        }
        else if (!OVRInput.Get(OVRInput.RawButton.B) && NewTrackDialogOpen && trigger_count >= 5)
        {
            Debug.Log("Deactivate New Track Menu");
            NewTrackDialogs.SetActive(false);
            //NewTrackDialogOpen = false;
            trigger_count = 0;
            trackChoiceDone = true;
        }

        if (OVRInput.Get(OVRInput.RawButton.A) && !DialogOpen && TrackID >= 0)
        {
            QualityDialog.SetActive(true);
            //QualityDialogOpen = true;
        }
        else if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > LabelToolManager.threshold && QualityDialogOpen)
        {
            QualityDialog.SetActive(false);
            //QualityDialogOpen = false;
        }

        if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > LabelToolManager.threshold && NewTrackDialogOpen && !QualityDialogOpen && !right_index_trigger_pressed)
        {
            right_index_trigger_pressed = true;
            trigger_count++;
        }
        else if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) < LabelToolManager.threshold && right_index_trigger_pressed)
        {
            right_index_trigger_pressed = false;
        }
        if ((OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0] > LabelToolManager.threshold) && !left_thumbstick_pressed && !LabelToolManager.DialogOpen)
        {
            if (SequenceIdx < fileNamesPCD.Length - RecordingFrequencyRatio)
            {
                SequenceIdx++;
                // Adjust camera rate to fit the pcd data
                ImageManager.loadImages_(fileNamesImg[SequenceIdx * RecordingFrequencyRatio]);
                
            }
            left_thumbstick_pressed = true;
        }       
        else if ((OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0] < -LabelToolManager.threshold) && !left_thumbstick_pressed && !LabelToolManager.DialogOpen) 
        {
            if (SequenceIdx > 0)
            {
                SequenceIdx--;
                // Adjust camera rate to fit the pcd data
                ImageManager.loadImages_(fileNamesImg[SequenceIdx * RecordingFrequencyRatio]);
            }
            left_thumbstick_pressed = true;
        }
        else if (OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick)[0] < LabelToolManager.threshold && left_thumbstick_pressed && !LabelToolManager.DialogOpen)
        {
            left_thumbstick_pressed = false;
        }

        if (OVRInput.Get(OVRInput.RawButton.X) && !X_pressed)
        {
            if (current_scale_idx >= scaleFactors.Count - 1)
            {
                current_scale_idx = 0;
            }
            else
            {
                current_scale_idx += 1;
            }
            X_pressed = true;
        }
        else if (!OVRInput.Get(OVRInput.RawButton.X) && X_pressed)
        {
            X_pressed = false;
        }
    }

    public static void activateMenu(int numMenu)
    {
        activeDialog = numMenu;
        if (numMenu != 0)
        {
            //Debug.Log("Close last manu");
            menu[numMenu - 1].SetActive(false);
        }
        if (numMenu <= menu.Length - 1)
        {
            //Debug.Log("Open next menu");
            menu[numMenu].SetActive(true);
        }
        else if (numMenu == menu.Length)
        {
            activeDialog = 0;
            //NewTrackDialogs.SetActive(false);
        }
        if (numMenu < 0 && numMenu > menu.Length - 1)
            Debug.LogError("invalid menu number");
    }

    string[] ReadNames(string[] Names)
    {
        int i = 0;
        string[] fileNamesFinal = new string[Names.Length];
        foreach (string _ in Names)
        {
            string tempString = System.IO.Path.GetFileName(Names[i]);
            string tempStringSplit = tempString.Split('.')[0];
            fileNamesFinal[i] = tempStringSplit;
            i++;
        }
        System.Array.Sort(fileNamesFinal);
        return fileNamesFinal;
    }
}

// Class to assign Track information
public class TrackInformation
{
    // track ID
    private int ID;
    // choice of parameters
    private int[] choiceParameters = new int[6];

    public TrackInformation(int id, int[] choiceParam)
    {
        ID = id;

        for (int i = 0; i <= 5; i++)
        {
            choiceParameters[i] = choiceParam[i];
        }
    }

    public int getID()
    {
        return ID;
    }

    public int[] getChoice()
    {
        return choiceParameters;
    }
}




