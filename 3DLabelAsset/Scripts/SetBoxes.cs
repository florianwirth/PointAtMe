using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class SetBoxes : MonoBehaviour
{
    // cuboid dummy
    public GameObject CubeReshapeable;
    // scene object
    static public GameObject SceneObject;
    // state of buttons
    bool A_pressed = false;
    bool B_pressed = false;
    //Quality dialog
    GameObject QualityDialog;
    //new tracks dialog
    GameObject NewTrackDialogs;
    // mesh renderer dummy
    public static MeshRenderer meshrenderer;

    //List<GameObject> dynamicBoxList = new List<GameObject>();
    public static List<LabeledObject> labeledObjectList = new List<LabeledObject>();
    //List for colors of tracks
    public static List<Color> trackColorList = new List<Color>();

    // creation of a new track initiated
    bool newTrackCreationInitiated = false;

    // Use this for initialization
    void Start()
    {
        NewTrackDialogs = GameObject.Find("NewTrackDialogs");
        QualityDialog = GameObject.Find("QualityDialog");
        QualityDialog.SetActive(false);
        NewTrackDialogs.SetActive(false);
        SceneObject = GameObject.Find("SceneObject");
        if (trackColorList.Count() < LabelToolManager.currentTrackID + 1)
            trackColorList.Add(generateColor());
    }

    // Update is called once per frame
    void Update()
    {
        OVRInput.Update();

        if (OVRInput.Get(OVRInput.RawButton.B) && !B_pressed && !LabelToolManager.DialogOpen)
        {
            B_pressed = true;
            //Add a new color to the list of colors for bounding boxes
            trackColorList.Add(generateColor());
            //assigns a unique color to each track
            if (labeledObjectList.Count > 0)
            {
                int i = 0;
                foreach (LabeledObject obj in labeledObjectList)
                {
                    meshrenderer = obj.objectBoundingBox.GetComponent<MeshRenderer>();
                    meshrenderer.material.color = trackColorList[i];
                    //make material transparent
                    
                    i += 1;
                }
            }
            // Increment TrackID and assign currentTrackID
            LabelToolManager.TrackID++;
            LabelToolManager.currentTrackID = LabelToolManager.TrackID;
            newTrackCreationInitiated = true;
        }
        else if (!OVRInput.Get(OVRInput.RawButton.B) && B_pressed)
        {
            B_pressed = false;
        }

        if (LabelToolManager.trackChoiceDone && newTrackCreationInitiated)
        {
            Debug.Log("New Track, new box");
            // Create a placeholder object with the current track ID when beginning a new track
            GameObject newBoundingBox = GameObject.CreatePrimitive(PrimitiveType.Cube);

            // Set object's position at the origin of the coordinate system to distinguish from labeled objects
            newBoundingBox.transform.position = new Vector3(0f, 0f, 0f);
            newBoundingBox.transform.localScale = new Vector3(0.000001f, 0.000001f, 0.000001f);
            newBoundingBox.transform.rotation = new Quaternion(0, 0, 0, 0);

            newBoundingBox.transform.SetParent(SceneObject.transform, true);
            meshrenderer = newBoundingBox.GetComponent<MeshRenderer>();
            meshrenderer.material.color = Color.red;
            meshrenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            meshrenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            meshrenderer.material.SetInt("_ZWrite", 0);
            meshrenderer.material.DisableKeyword("_ALPHATEST_ON");
            meshrenderer.material.DisableKeyword("_ALPHABLEND_ON");
            meshrenderer.material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            meshrenderer.material.renderQueue = 3000;

            Debug.Log("Choices: Class: " +  LabelToolManager.choice[0] + 
                ", Priority: " +            LabelToolManager.choice[1] + 
                ", Direction: " +           LabelToolManager.choice[2]  + 
                ", Moves: " +               LabelToolManager.choice[3] + 
                ", Lane: " +                LabelToolManager.choice[4]);

            LabeledObject labeledObject = new LabeledObject(newBoundingBox,
                LabelToolManager.TrackID,
                LabelToolManager.choice[0],
                LabelToolManager.choice[5],
                LabelToolManager.choice[1],
                LabelToolManager.choice[2],
                LabelToolManager.choice[3],
                LabelToolManager.choice[4],
                0,
                0,
                0);
            labeledObjectList.Add(labeledObject);

            LabelToolManager.trackInformationList.Add(new TrackInformation(LabelToolManager.TrackID, LabelToolManager.choice));

            Debug.Log("Object created");
            newTrackCreationInitiated = false;
        }

        if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[0] > LabelToolManager.threshold) && !A_pressed && (labeledObjectList.Count >= 1) && (LabelToolManager.currentTrackID <= LabelToolManager.TrackID) && !LabelToolManager.DialogOpen)
        {
            if (LabelToolManager.currentTrackID < LabelToolManager.TrackID)
            {
                int i = 0;
                foreach (LabeledObject obj in labeledObjectList)
                {
                    meshrenderer = obj.objectBoundingBox.GetComponent<MeshRenderer>();
                    meshrenderer.material.color = trackColorList[i];
                    i += 1;
                }
                LabelToolManager.currentTrackID++;
                meshrenderer = labeledObjectList[LabelToolManager.currentTrackID].objectBoundingBox.GetComponent<MeshRenderer>();
                meshrenderer.material.color = Color.red;
            }
            else if (LabelToolManager.currentTrackID == LabelToolManager.TrackID)
            {
                //TODO: UI info
                
            }
            A_pressed = true;

            Debug.Log("Right controller stick pressed right");
            Debug.Log("currentTrackID: " + LabelToolManager.currentTrackID);
            Debug.Log("TrackID: " + LabelToolManager.TrackID);
        }
        else if ((OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick)[0] < -LabelToolManager.threshold) && !A_pressed && !LabelToolManager.DialogOpen)
        {
            if (LabelToolManager.currentTrackID > 0)
            {
                int i = 0;
                foreach (LabeledObject obj in labeledObjectList)
                {
                    meshrenderer = obj.objectBoundingBox.GetComponent<MeshRenderer>();
                    meshrenderer.material.color = trackColorList[i];
                    i += 1;
                }
                LabelToolManager.currentTrackID--;
                meshrenderer = labeledObjectList[LabelToolManager.currentTrackID].objectBoundingBox.GetComponent<MeshRenderer>();
                meshrenderer.material.color = Color.red;
            }
            else if (LabelToolManager.currentTrackID == 0)
            {
                //TODO: UI info 
            }
            A_pressed = true;

            Debug.Log("Switched Track: currentTrackID= " + LabelToolManager.currentTrackID + ", Tracks in total: " + LabelToolManager.TrackID);
        }
        else if (OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick).magnitude < LabelToolManager.threshold && A_pressed)
        {
            A_pressed = false;
        }

        if (OVRInput.Get(OVRInput.RawButton.A) && !A_pressed && !LabelToolManager.DialogOpen && LabelToolManager.TrackID >= 0)
        {
            Debug.Log("labeled objects in list: " + labeledObjectList.Count);
            if (labeledObjectList[LabelToolManager.currentTrackID].objectBoundingBox.GetComponent<MeshRenderer>().material.color == Color.red)
            {
                labeledObjectList[LabelToolManager.currentTrackID].objectBoundingBox = ResizeBox(labeledObjectList[LabelToolManager.currentTrackID].objectBoundingBox);
                meshrenderer = labeledObjectList[LabelToolManager.currentTrackID].objectBoundingBox.GetComponent<MeshRenderer>();
                meshrenderer.material.color = Color.red;
                QualityDialog.SetActive(true);
            }
            A_pressed = true;
        }
        else if (!OVRInput.Get(OVRInput.RawButton.A) && A_pressed && (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > LabelToolManager.threshold))
        {
            A_pressed = false;
            QualityDialog.SetActive(false);
        }
    }

    IEnumerator Wait()
    {
        yield return new WaitForSecondsRealtime(1);
    }

    GameObject ResizeBox(GameObject tempObject)
    {
        tempObject.transform.position = CubeReshapeable.transform.position;
        tempObject.transform.rotation = CubeReshapeable.transform.rotation;
        tempObject.transform.localScale = CubeReshapeable.transform.localScale / PointCloudManager.scale;
        return tempObject;
    }

    //Generates a random color for bounding box
    public static Color generateColor()
    {
        Color c = new Color((float)Random.Range(0.2f, 0.9f), (float)Random.Range(0.2f, 0.9f), (float)Random.Range(0.2f, 0.9f), 0.6f);
        Debug.Log("Color: " + c.a);
        return c;
        
    }

    public static void loadObject(
        int objID,
        int objClass,
        int objPriority,
        int objDirection,
        int objIsParking,
        int objLane,
        int objQuality,
        int objFirstClearAppearance,
        int objLastClearAppearance,
        int objClearestShot,
        float objPosX,
        float objPosY,
        float objPosZ,
        float objRotX,
        float objRotY,
        float objRotZ,
        float objRotW,
        float objLocalScaleX,
        float objLocalScaleY,
        float objLocalScaleZ,
        bool objColorRed)
    {
        GameObject newBoundingBox = GameObject.CreatePrimitive(PrimitiveType.Cube);

        SceneObject = GameObject.Find("SceneObject");
        newBoundingBox.transform.parent = SceneObject.transform;
        // TODO: compensate for the position and the rotation of the pointcloud in previous scene when loading the objects 

        newBoundingBox.transform.position = SceneObject.transform.position + SceneObject.transform.rotation * new Vector3(objPosX, objPosY, objPosZ);
        newBoundingBox.transform.rotation = SceneObject.transform.rotation * new Quaternion(objRotX, objRotY, objRotZ, objRotW);
        newBoundingBox.transform.localScale = new Vector3(objLocalScaleX, objLocalScaleY, objLocalScaleZ);

        meshrenderer = newBoundingBox.GetComponent<MeshRenderer>();
        meshrenderer.material.color = Color.red;
        meshrenderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        meshrenderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        meshrenderer.material.SetInt("_ZWrite", 0);
        meshrenderer.material.DisableKeyword("_ALPHATEST_ON");
        meshrenderer.material.DisableKeyword("_ALPHABLEND_ON");
        meshrenderer.material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        meshrenderer.material.renderQueue = 3000;

        if (objColorRed == true)
        {
            meshrenderer.material.color = Color.red;
        }
        else if (objColorRed == false)
        {
            //assigns a unique color to each bounding box and stores in trackColorList
            if (trackColorList.Count() != 0 && objID < trackColorList.Count() )
            {
                Debug.LogError("generate color from track color list " + trackColorList[objID].a);
                meshrenderer.material.color = trackColorList[objID];   
            }
            else
            {
                Debug.LogError("generate color");
                meshrenderer.material.color = generateColor();
                trackColorList.Add(meshrenderer.material.color);
            }
            Debug.Log("Track " + objID + " with color " + meshrenderer.material.color);
        }

        LabeledObject labeledObj = new LabeledObject(
            newBoundingBox,
            objID,
            objClass,
            objQuality,
            objPriority,
            objDirection,
            objIsParking,
            objLane,
            objFirstClearAppearance,
            objLastClearAppearance,
            objClearestShot);

        labeledObjectList.Add(labeledObj);
        Debug.Log("Created Placeholder with ID: " + labeledObj.objectID + "    Created red:" + objColorRed);
    }

    static public Vector3 PositionFromMatrix(Matrix4x4 m)
    {
        return new Vector3(m[12], m[13], m[14]);
    }

    static public GameObject compensatePCD(GameObject Box)
    {
        GameObject newBox = new GameObject();
        newBox.transform.parent = SceneObject.transform;
        Matrix4x4 bbRelToScObj =
            Matrix4x4.TRS(SceneObject.transform.position,
            SceneObject.transform.rotation,
            new Vector3(1.0f, 1.0f, 1.0f));

        Matrix4x4 newMatrix = bbRelToScObj * Matrix4x4.TRS(
            Box.transform.position,
            Box.transform.rotation,
            new Vector3(1.0f, 1.0f, 1.0f)
            );
        newBox.transform.rotation = SceneObject.transform.rotation * Box.transform.rotation;
        newBox.transform.position = PositionFromMatrix(newMatrix);
        newBox.transform.localScale = Box.transform.localScale;
        return newBox;
    }
}

// labeled object class
public class LabeledObject
{

    public GameObject objectBoundingBox;
    public int objectID;
    public int objectClass;
    public int objectPriority;
    public int objectDirection;
    public int objectIsParking;
    public int objectLane;
    public int objectQuality;
    public int objectFirstClearAppearance;
    public int objectLastClearAppearance;
    public int objectClearestShot;

    public LabeledObject(
        GameObject gameObj,
        int objID,
        int objClass,
        int objQuality,
        int objPriority,
        int objDirection,
        int objIsParking,
        int objLane,
        int objFirstAppearance,
        int objLastAppearance,
        int objBestShot)
    {
        objectBoundingBox = gameObj;
        objectID = objID;
        objectClass = objClass;
        objectQuality = objQuality;
        objectPriority = objPriority;
        objectDirection = objDirection;
        objectIsParking = objIsParking;
        objectLane = objLane;
        objectFirstClearAppearance = objFirstAppearance;
        objectLastClearAppearance = objLastAppearance;
        objectClearestShot = objBestShot;
    }

}