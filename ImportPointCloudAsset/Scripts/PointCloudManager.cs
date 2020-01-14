using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

public class PointCloudManager : MonoBehaviour
{

    // File
    public string dataPath;
    //private string filename;
    public Material matVertex;
    // All .pcd files in directory
    List<string> fileNames;

    // GUI
    private float progress = 0;
    // gui text dummy object
    private string GUIText;
    // pcd loaded
    private bool loaded = false;

    // scene object containing pcd
    public GameObject PointCloud;
    // scene object containing pcd and boxes
    public GameObject SceneObject;

    // list of point groups (for pcd larger thatn 65000 points)
    public GameObject[] pointGroup;
    // number of groups
    private int groupId = 0;
    // scale
    public static float scale;
    public bool forceReload = false;

    public int numPoints;
    private float heightvec;
    public int numPointGroups;
    private int limitPoints = 65000;
    public static int numOfPointClouds;

    private Vector3[] points;
    private Color[] colors;
    private Vector3 minValue;

    public static int ShowingIdx = 0;

    private bool Thumbstick_released = true;

    // Current PointCloud name
    public static string currentPCName = "UNKNOWN";

    // height of the sensor above the ground
    public float sensorHeight = 2.4f;
    // height of point cloud which is of our interest measured above ground
    public float upperHeight = 2.6f;
    // representation of points: "lines" or "points" (default)
    string design = "points";
    // connect points up to a distance of ...
    float connect_dist = 0.1f;
    // ... for each ... of distance to sensor
    float sensor_dist = 5.0f;
    // is intensity available? Sometimes there are only zeros in the pcd files...
    bool intensity_exists = false;
    void Start()
    {
        // Create Resources folder
        createFolders();

        // Get all .pcd files that are part of a sequence
        if (System.IO.Directory.Exists(LabelToolManager.PathToData + "/pcd"))
        {
            fileNames = System.IO.Directory.GetFiles(LabelToolManager.PathToData + "/pcd", "*.pcd").ToList();
            if (fileNames.Count > 0)
            {
                int i = 0;
                string[] fileNamesFinal = new string[fileNames.Count];
                foreach (string nameOfFile in fileNames)
                {
                    string tempString = Path.GetFileName(fileNames[i]);
                    string tempStringSplit = tempString.Split('.')[0];
                    fileNamesFinal[i] = tempStringSplit;
                    i++;
                }
                Array.Sort(fileNamesFinal);
                numOfPointClouds = fileNamesFinal.Length;
                fileNames = fileNamesFinal.ToList();
                loadScene(fileNames[LabelToolManager.SequenceIdx]);
                currentPCName = fileNames[LabelToolManager.SequenceIdx];
            }
            else
            {
                Debug.LogError("No .pcd files found in directory: " + LabelToolManager.PathToData + "/pcd");
            }
        }
        else
        {
            Debug.LogError("Directory: " + LabelToolManager.PathToData + "/pcd" + " does not exist");
        }
        scale = LabelToolManager.scaleFactors[LabelToolManager.current_scale_idx];

        //Import labels on reloading
        string pathToFile = LabelToolManager.PathToLabels + "/label_" + fileNames[LabelToolManager.SequenceIdx] + ".txt"; // path to first file in label folder
        if (File.Exists(pathToFile))
        {
            CountLabelsOnStart();
            readObjectsFromText(pathToFile);
            if (LabelToolManager.currentTrackID != -1)
                Debug.Log("number of Tracks on reloading: " + (LabelToolManager.currentTrackID + 1));
        }
    }

    void loadScene(string fileName)
    {
        // assign SceneObject and PointCloud  varaibles in Hierarchy
        SceneObject = GameObject.Find("SceneObject");
        PointCloud = GameObject.Find("PointCloud");

        foreach (GameObject pg in pointGroup)
        {
            Destroy(pg);
        }
        pointGroup = new GameObject[3];

        // Check if the PointCloud was loaded previously
        if (!Directory.Exists(LabelToolManager.PathToPCLMeshes + "/" + fileName))
        {
            UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/PointCloudMeshes", fileName);
            loadPointCloud(fileName);
        }
        else if (forceReload)
        {
            Debug.Log("ForceReload");
            UnityEditor.FileUtil.DeleteFileOrDirectory(LabelToolManager.PathToPCLMeshes + "/" + fileName);
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.AssetDatabase.CreateFolder("Assets/Resources/PointCloudMeshes", fileName);
            loadPointCloud(fileName);
        }
        else
        { // Load stored PointCloud
            loadStoredMeshes(fileName);
        }
    }

    void loadPointCloud(string fileName)
    {
        // Check what file exists
        if (File.Exists(LabelToolManager.PathToData + "/pcd/" + fileName + ".pcd"))
        {
            StartCoroutine(loadPointCloudFile(LabelToolManager.PathToData + "/pcd/" + fileName + ".pcd", fileName));

            currentPCName = fileName;
        }
        else
            Debug.Log("File '" + LabelToolManager.PathToData + "/pcd/" + fileName + ".pcd" + "' could not be found");
    }

    // Load stored PointCloud
    void loadStoredMeshes(string fileName)
    {
        currentPCName = fileName;
        // instantiates as loacl variable PCL, dont change this line!!
        GameObject PCL = Instantiate(Resources.Load("PointCloudMeshes/" + fileName + "/" + fileName)) as GameObject;

        PCL.transform.parent = PointCloud.transform;
        PCL.transform.localScale = PointCloud.transform.localScale;

        // assign pointGroup variable in Hierarchy 
        for (int i = 0; i < 3; i++)
        {
            try
            {
                pointGroup[i] = GameObject.Find("PointCloud" + "/" + fileName + "(Clone)" + "/" + fileName + "_PG" + i.ToString());
                pointGroup[i].transform.position = SceneObject.transform.position;
                pointGroup[i].transform.rotation = SceneObject.transform.rotation;
            }
            catch (NullReferenceException e)
            {
                Debug.Log("Maybe not all PointGroups were loaded correctly.");
            }

        }

        //pointGroup[1] = GameObject.Find("PointCloud" + "/" + fileName + "(clone)" + "/" + filename + "_pg1");
        //pointgroup[2] = gameobject.find("pointcloud" + "/" + filename + "(clone)" + "/" + filename + "_pg2");



        //pointgroup[1].transform.position = sceneobject.transform.position;
        //pointgroup[1].transform.rotation = sceneobject.transform.rotation;

        //pointgroup[2].transform.position = sceneobject.transform.position;
        //pointgroup[2].transform.rotation = sceneobject.transform.rotation;

        loaded = true;
    }

    // Start Coroutine of reading the points from the PCL file and creating the meshes
    System.Collections.IEnumerator loadPointCloudFile(string dPath, string fileName)
    {
        // Read file
        System.IO.StreamReader sr = new System.IO.StreamReader(dPath);
        bool header_end = false;
        while (!header_end)
        {
            string[] discard = sr.ReadLine().Split();
            switch (discard[0])
            {
                case ("POINTS"):
                    numPoints = int.Parse(discard[1]);
                    break;
                case ("FIELDS"):
                    if (discard[1] != "x" || discard[2] != "y" || discard[3] != "z")
                        Debug.LogError("Wrong data format");
                    break;
                case ("WIDTH"):
                    //Debug.Log("ScannerLines: " + discard[1]);
                    break;
                case ("HEIGHT"):
                    //Debug.Log("ScannerRows: " + discard[1]);
                    break;
                case ("DATA"):
                    header_end = true;
                    break;
            }
        }

        points = new Vector3[numPoints];
        colors = new Color[numPoints];
        minValue = new Vector3();


        int i = 0;
        while (i < numPoints)
        {
            string[] buffer = sr.ReadLine().Split();

            points[i] = new Vector3(float.Parse(buffer[0]) * scale, float.Parse(buffer[2]) * scale, float.Parse(buffer[1]) * scale);
            if (intensity_exists)
            {
                colors[i] = intensityToColor(float.Parse(buffer[3]), 128.0f, 255.0f);
            }
            else
            {
                colors[i] = intensityToColor(float.Parse(buffer[2]), sensorHeight, upperHeight);
            }
            
            // Relocate Points near the origin

            // GUI
            progress = i * 1.0f / (numPoints - 1) * 1.0f;
            if (i % Mathf.FloorToInt(numPoints / 20) == 0)
            {
                GUIText = i.ToString() + " out of " + numPoints.ToString() + " loaded";
                yield return null;
            }
            i += 1;
        }
        // Instantiate Point Groups
        numPointGroups = Mathf.CeilToInt(numPoints * 1.0f / limitPoints * 1.0f);

        for (int j = 0; j < numPointGroups - 1; j++)
        {
            InstantiateMesh(j, limitPoints, fileName);
            if (j % 10 == 0)
            {
                GUIText = j.ToString() + " out of " + numPointGroups.ToString() + " PointGroups loaded";
                yield return null;
            }
        }
        InstantiateMesh(numPointGroups - 1, numPoints - (numPointGroups - 1) * limitPoints, fileName);

        //Store PointCloud as prefab
        UnityEditor.PrefabUtility.CreatePrefab("Assets/Resources/PointCloudMeshes" + "/" + fileName + "/" + fileName + ".prefab", PointCloud);

        loaded = true;

    }

    // Instantiate Mesh and assiagn to Point Groups
    void InstantiateMesh(int meshInd, int nPoints, string fileName)
    {
        pointGroup[groupId] = new GameObject(fileName + "_PG" + meshInd);
        pointGroup[groupId].AddComponent<MeshFilter>();
        pointGroup[groupId].AddComponent<MeshRenderer>();
        pointGroup[groupId].GetComponent<Renderer>().material = matVertex;

        pointGroup[groupId].GetComponent<MeshFilter>().mesh = CreateMesh(meshInd, nPoints, limitPoints);
        pointGroup[groupId].transform.parent = PointCloud.transform;
        Destroy(GetComponent<BoxCollider>());

        // Store Mesh
        UnityEditor.AssetDatabase.CreateAsset(pointGroup[groupId].GetComponent<MeshFilter>().mesh, "Assets/Resources/PointCloudMeshes" + "/" + fileName + "/" + fileName + meshInd + ".asset");
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

        groupId++;
    }

    // Create Meshes from PointCloud
    Mesh CreateMesh(int id, int nPoints, int limitPoints)
    {
        Mesh mesh = new Mesh();



        if(design == "lines")
        {
            // Lines
            Vector3[] myPoints = new Vector3[nPoints];
            List<Vector3> linePoints = new List<Vector3>(0);
            int[] indeciesLine = new int[2 * (nPoints - 1)];
            Color[] myLineColors = new Color[nPoints];
            int num = 0;
            for (int i = 0; i < nPoints; ++i)
            {
                myPoints[i] = points[id * limitPoints + i] - minValue;

            }
            bool flag = true;
            for (int i = 0; i < nPoints - 1; i++)
            {
                float threshold = Vector3.Magnitude(myPoints[i]) / (sensor_dist * LabelToolManager.scaleFactors[LabelToolManager.current_scale_idx]) * connect_dist * LabelToolManager.scaleFactors[LabelToolManager.current_scale_idx];

                if (Vector3.Distance(myPoints[i], myPoints[i + 1]) < threshold)
                {

                    indeciesLine[2 * i] = i;
                    indeciesLine[2 * i + 1] = i + 1;
                    myLineColors[i] = colors[id * limitPoints + i];
                    num++;
                }
            }
            Debug.Log("LINESNUM" + indeciesLine.Length);
            mesh.vertices = myPoints;
            mesh.colors = myLineColors;
            mesh.SetIndices(indeciesLine, MeshTopology.Lines, 0);


            mesh.uv = new Vector2[nPoints];
            mesh.normals = new Vector3[nPoints];
        }
        else
        {
            // Points
            Vector3[] myPoints = new Vector3[nPoints];
            int[] indecies = new int[nPoints];
            Color[] myColors = new Color[nPoints];

            for (int i = 0; i < nPoints; ++i)
            {
                myPoints[i] = points[id * limitPoints + i] - minValue;
                indecies[i] = i;
                myColors[i] = colors[id * limitPoints + i];
            }

            mesh.vertices = myPoints;
            mesh.colors = myColors;
            mesh.SetIndices(indecies, MeshTopology.Points, 0);
            mesh.uv = new Vector2[nPoints];
            mesh.normals = new Vector3[nPoints];
        } 

        return mesh;
    }

    private void Update()
    {
        SceneObject = GameObject.Find("SceneObject");
        PointCloud = GameObject.Find("PointCloud");
        if (LabelToolManager.X_pressed)
        {
            scale = LabelToolManager.scaleFactors[LabelToolManager.current_scale_idx];
        }

        for (int i = 0; i < groupId; i++)
        {
            // this is needed to load pcd with the same pose as the previously loaded ones
            pointGroup[i].transform.position = SceneObject.transform.position;
            pointGroup[i].transform.rotation = SceneObject.transform.rotation;
        }

        if (!(ShowingIdx == LabelToolManager.SequenceIdx))
        {
            changePointCloud(LabelToolManager.SequenceIdx, ShowingIdx);
            ShowingIdx = LabelToolManager.SequenceIdx;
        }
    }

    void changePointCloud(int nextIdx, int lastIdx)
    {
        saveSceneToText(fileNames[lastIdx]);
        Destroy(GameObject.Find("PointCloud" + "/" + fileNames[lastIdx] + "(Clone)"));
        groupId = 0;
        loadScene(fileNames[nextIdx]);
        string pathToFile = LabelToolManager.PathToLabels + "/" + "label_" + fileNames[nextIdx] + ".txt";
        readObjectsFromText(pathToFile);
    }

    void createFolders()
    {
        if (!System.IO.Directory.Exists(Application.dataPath + "/Resources/"))
            UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");

        if (!System.IO.Directory.Exists(LabelToolManager.PathToPCLMeshes))
            Directory.CreateDirectory(LabelToolManager.PathToPCLMeshes);
        //UnityEditor.AssetDatabase.CreateFolder("Assets/Resources", "PointCloudMeshes");
    }

    void OnGUI()
    {
        if (!loaded)
        {
            GUI.BeginGroup(new Rect(Screen.width / 2 - 100, Screen.height / 2, 400.0f, 20));
            GUI.Box(new Rect(0, 0, 200.0f, 20.0f), GUIText);
            GUI.Box(new Rect(0, 0, progress * 200.0f, 20), "");
            GUI.EndGroup();
        }
    }

    public Color intensityToColor(float pheight, float sheight, float uheight)
    {
        //sheight is sensor height
        //pheight is point height
        //uheight is the height of points which are not of our interest 
        // Solid green. RGBA is (0, 1, 0, 1).
        // Solid red. RGBA is   (1, 0, 0, 1).
        // Yellow.RGBA is (1, 0.92, 0.016, 1)
        // Solid blue. RGBA is (0, 0, 1, 1).

        float color_norm = 6.0f * (pheight + sheight) / uheight;

        float c;

        switch ((int)Math.Floor(color_norm))
        {
            case 0:
                c = color_norm % 1.0f;
                return new Color(1.0f, c, 0.0f, 1.0f);
            case 1:
                c = color_norm % 1.0f;
                return new Color(1.0f - c, 1.0f, 0.0f, 1.0f);
            case 2:
                c = color_norm % 1.0f;
                return new Color(0.0f, 1.0f, c, 1.0f);
            case 3:
                c = color_norm % 1.0f;
                return new Color(0.0f, 1.0f - c, 1.0f, 1.0f);
            case 4:
                c = color_norm % 1.0f;
                return new Color(c, 0.0f, 1.0f, 1.0f);
            case 5:
                c = color_norm % 1.0f;
                return new Color(1.0f, 0.0f, 1.0f - c, 1.0f);
            default:
                return new Color(1.0f, 0.0f, 0.0f, 1.0f);
        }
    }



    // Save labeled objects from current scene to a text file
    public void saveSceneToText(string name)
    {
        string path = LabelToolManager.PathToLabels + "/label_" + name + ".txt";
        if (!System.IO.Directory.Exists(LabelToolManager.PathToLabels))
        {
            System.IO.Directory.CreateDirectory(LabelToolManager.PathToLabels);
        }

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
        // Create a file to write to.
        using (System.IO.StreamWriter sw = System.IO.File.CreateText(path))
        {
            if (LabelToolManager.TrackID >= 0)
            {
                for (int i = 0; i < SetBoxes.labeledObjectList.Count; i++)
                {
                    // File format:
                    // objectID objectClass objectPriority objectFirstClearAppearance objectLastClearAppearance objectClearestShot position.x position.y position.z rotation.x rotation.y rotation.z rotation.w localScale.x localScale.y localScale.z
                    int tempObjID = SetBoxes.labeledObjectList[i].objectID;
                    int tempObjClass = SetBoxes.labeledObjectList[i].objectClass;
                    int tempObjPriority = SetBoxes.labeledObjectList[i].objectPriority;

                    int tempObjDirection = SetBoxes.labeledObjectList[i].objectDirection;
                    int tempObjIsParking = SetBoxes.labeledObjectList[i].objectIsParking;
                    int tempObjLane = SetBoxes.labeledObjectList[i].objectLane;
                    int tempObjQuality = SetBoxes.labeledObjectList[i].objectQuality;

                    int tempObjFirstClearAppearance = SetBoxes.labeledObjectList[i].objectFirstClearAppearance;
                    int tempObjLastClearAppearance = SetBoxes.labeledObjectList[i].objectLastClearAppearance;
                    int tempObjClearestShot = SetBoxes.labeledObjectList[i].objectClearestShot;

                    GameObject tempLabeledObj = compensatePCD(SetBoxes.labeledObjectList[i].objectBoundingBox);

                    //TODO: Check if Quaternion is correctly tranfered to right hand COS
                    Quaternion tempRot = convertToUnity(new Quaternion(
                        tempLabeledObj.transform.rotation.x,
                        tempLabeledObj.transform.rotation.y,
                        tempLabeledObj.transform.rotation.z,
                        tempLabeledObj.transform.rotation.w));
                    sw.WriteLine(
                        tempObjID + " " +
                        tempObjClass + " " +
                        tempObjQuality + " " +
                        tempObjPriority + " " +
                        tempObjDirection + " " +
                        tempObjIsParking + " " +
                        tempObjLane + " " +
                        tempObjFirstClearAppearance + " " +
                        tempObjLastClearAppearance + " " +
                        tempObjClearestShot + " " +
                        tempLabeledObj.transform.position.x / scale + " " +
                        tempLabeledObj.transform.position.y / scale + " " +
                        -tempLabeledObj.transform.position.z / scale + " " +
                        tempRot.x + " " +
                        tempRot.y + " " +
                        tempRot.z + " " +
                        tempLabeledObj.transform.rotation.w + " " +
                        Math.Abs(tempLabeledObj.transform.localScale.x) + " " +
                        Math.Abs(tempLabeledObj.transform.localScale.y) + " " +
                        Math.Abs(tempLabeledObj.transform.localScale.z));

                    Debug.Log("RESULTS: " +
                        tempObjID + " " +
                        tempObjClass + " " +
                        tempObjQuality + " " +
                        tempObjPriority + " " +
                        tempObjDirection + " " +
                        tempObjIsParking + " " +
                        tempObjLane + " " +
                        tempObjFirstClearAppearance + " " +
                        tempObjLastClearAppearance + " " +
                        tempObjClearestShot + " " +
                        tempLabeledObj.transform.position.x / scale + " " +
                        tempLabeledObj.transform.position.y / scale + " " +
                        tempLabeledObj.transform.position.z / scale + " " +
                        tempLabeledObj.transform.rotation.x + " " +
                        tempLabeledObj.transform.rotation.x + " " +
                        tempLabeledObj.transform.rotation.y + " " +
                        tempLabeledObj.transform.rotation.z + " " +
                        tempLabeledObj.transform.rotation.w + " " +
                        Math.Abs(tempLabeledObj.transform.localScale.x) + " " +
                        Math.Abs(tempLabeledObj.transform.localScale.y) + " " +
                        Math.Abs(tempLabeledObj.transform.localScale.z) + " true");
                    Destroy(tempLabeledObj);
                }
            }
        }
        // Remove labeled objects from scene after they are saved to text 
        foreach (LabeledObject lo in SetBoxes.labeledObjectList)
        {
            Destroy(lo.objectBoundingBox);
        }
        SetBoxes.labeledObjectList.Clear();
    }

    public void readObjectsFromText(string path)
    {
        foreach (TrackInformation tInfo in LabelToolManager.trackInformationList)
        {
            Debug.Log("SAVED TRACK INFO: ID: = " + tInfo.getID() +
                ", Class: " + tInfo.getChoice()[0] +
                ", Quality: " + tInfo.getChoice()[5] +
                ", Priority: " + tInfo.getChoice()[1] +
                ", Direction: " + tInfo.getChoice()[2] +
                ", Moves: " + tInfo.getChoice()[3] +
                ", Lane: " + tInfo.getChoice()[4]);
        }

        // Open the file to read from.
        if (!File.Exists(path))
        {
            // If the file does not exist this means that the scene has not been labeled yet, placeholder objects are created for each ID
            for (int k = 0; k <= LabelToolManager.TrackID; k++)
            {
                foreach (TrackInformation tInfo in LabelToolManager.trackInformationList)
                {
                    if (tInfo.getID() == k)
                    {
                        bool colorBoxInRed = false;
                        if (tInfo.getID() == LabelToolManager.TrackID)
                        {
                            colorBoxInRed = true;
                        }
                        SetBoxes.loadObject(
                        tInfo.getID(),
                        tInfo.getChoice()[0],
                        tInfo.getChoice()[5],
                        tInfo.getChoice()[1],
                        tInfo.getChoice()[2],
                        tInfo.getChoice()[3],
                        tInfo.getChoice()[4],
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            0,
                            colorBoxInRed);
                        Debug.Log("Track Choice Loaded: ");
                        Debug.Log("ID: " + tInfo.getID());
                        Debug.Log("Choice: " + tInfo.getChoice()[0] +
                                                tInfo.getChoice()[5] +
                                                tInfo.getChoice()[1] +
                                                tInfo.getChoice()[2] +
                                                tInfo.getChoice()[3] +
                                                tInfo.getChoice()[4]);
                    }
                }
            }
        }
        else if (File.Exists(path))
        {
            if (LabelToolManager.TrackID >= 0)
            {
                // create a bool array (by default on creation all elements have the value "false")
                bool[] tempLabelInfo = new bool[LabelToolManager.TrackID + 1];
                List<LoadedLabeledObject> labeledObjectsLoadedList = new List<LoadedLabeledObject>();
                using (StreamReader sr = File.OpenText(path))
                {
                    string s = "";
                    while ((s = sr.ReadLine()) != null)
                    {
                        string[] parameters = s.Split(' ');
                        if (parameters.Length != 20)
                        {
                            Debug.Log("Wrong format found while reading from file: " + path);
                        }
                        else
                        {
                            // Parse read values from string to int/double
                            int readObjID;
                            int.TryParse(parameters[0], out readObjID);
                            int readObjClass;
                            int.TryParse(parameters[1], out readObjClass);
                            int readObjQuality;
                            int.TryParse(parameters[2], out readObjQuality);
                            int readObjPriority;
                            int.TryParse(parameters[3], out readObjPriority);
                            int readObjDirection;
                            int.TryParse(parameters[4], out readObjDirection);
                            int readObjIsParking;
                            int.TryParse(parameters[5], out readObjIsParking);
                            int readObjLane;
                            int.TryParse(parameters[6], out readObjLane);
                            int readObjFirstClearAppearance;
                            int.TryParse(parameters[7], out readObjFirstClearAppearance);
                            int readObjLastClearAppearance;
                            int.TryParse(parameters[8], out readObjLastClearAppearance);
                            int readObjClearestShot;
                            int.TryParse(parameters[9], out readObjClearestShot);
                            float objPosX;
                            float.TryParse(parameters[10], out objPosX);
                            float objPosY;
                            float.TryParse(parameters[11], out objPosY);
                            float objPosZ;
                            float.TryParse(parameters[12], out objPosZ);
                            float objRotX;
                            float.TryParse(parameters[13], out objRotX);
                            float objRotY;
                            float.TryParse(parameters[14], out objRotY);
                            float objRotZ;
                            float.TryParse(parameters[15], out objRotZ);
                            float objRotW;
                            float.TryParse(parameters[16], out objRotW);
                            float objLocalScaleX;
                            float.TryParse(parameters[17], out objLocalScaleX);
                            float objLocalScaleY;
                            float.TryParse(parameters[18], out objLocalScaleY);
                            float objLocalScaleZ;
                            float.TryParse(parameters[19], out objLocalScaleZ);

                            Quaternion tempRot = convertToUnity(new Quaternion(objRotX, objRotY, objRotZ, objRotW));

                            LoadedLabeledObject tempLoadedObj = new LoadedLabeledObject(
                                readObjID,
                                readObjClass,
                                readObjQuality,
                                readObjPriority,
                                readObjDirection,
                                readObjIsParking,
                                readObjLane,
                                readObjFirstClearAppearance,
                                readObjLastClearAppearance,
                                readObjClearestShot,
                                objPosX * scale,
                                objPosY * scale,
                                objPosZ * scale,
                                tempRot.x,
                                tempRot.y,
                                tempRot.z,
                                tempRot.w,
                                objLocalScaleX,
                                objLocalScaleY,
                                objLocalScaleZ);

                            labeledObjectsLoadedList.Add(tempLoadedObj);
                            // set value to true when object with this ID was found in the text file
                            tempLabelInfo[readObjID] = true;
                            Debug.Log(s);
                        }
                    }
                }
                for (int k = 0; k <= LabelToolManager.TrackID; k++)
                {
                    if (tempLabelInfo[k] == true)
                    {
                        foreach (LoadedLabeledObject llo in labeledObjectsLoadedList)
                        {
                            if (llo.loadedReadObjID == k)
                            {
                                // Create a new Bounding Box with the parameters from the text file
                                bool colorBoxInRed = false;
                                if (llo.loadedReadObjID == LabelToolManager.TrackID)
                                {
                                    colorBoxInRed = true;
                                }
                                SetBoxes.loadObject(
                                    llo.loadedReadObjID,
                                    llo.loadedReadObjClass,
                                    llo.loadedReadObjQuality,
                                    llo.loadedReadObjPriority,
                                    llo.loadedReadObjDirection,
                                    llo.loadedReadObjIsParking,
                                    llo.loadedReadObjLane,
                                    llo.loadedReadObjFirstClearAppearance,
                                    llo.loadedReadObjLastClearAppearance,
                                    llo.loadedReadObjClearestShot,
                                    llo.loadedObjPosX,
                                    llo.loadedObjPosY,
                                    -llo.loadedObjPosZ,
                                    llo.loadedObjRotX,
                                    llo.loadedObjRotY,
                                    llo.loadedObjRotZ,
                                    llo.loadedObjRotW,
                                    llo.loadedObjLocalScaleX,
                                    llo.loadedObjLocalScaleY,
                                    llo.loadedObjLocalScaleZ,
                                    colorBoxInRed);
                            }
                        }
                    }
                    else if (tempLabelInfo[k] == false)
                    {
                        // Create a new Bounding Box as a placeholder for Tracks that weren't labeled
                        foreach (TrackInformation tInfo in LabelToolManager.trackInformationList)
                        {
                            if (tInfo.getID() == k)
                            {
                                bool colorBoxInRed = false;
                                if (k == LabelToolManager.TrackID)
                                {
                                    colorBoxInRed = true;
                                }
                                SetBoxes.loadObject(
                                tInfo.getID(),
                                tInfo.getChoice()[0],
                                tInfo.getChoice()[5],
                                tInfo.getChoice()[1],
                                tInfo.getChoice()[2],
                                tInfo.getChoice()[3],
                                tInfo.getChoice()[4],
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    0,
                                    colorBoxInRed);
                            }
                        }

                    }

                }
                labeledObjectsLoadedList.Clear();
            }
        }
    }

    public Vector3 PositionFromMatrix(Matrix4x4 m)
    {
        return new Vector3(m[12], m[13], m[14]);
    }

    public GameObject compensatePCD(GameObject Box)
    {
        GameObject newBox = new GameObject();
        newBox.transform.parent = SceneObject.transform;
        Matrix4x4 bbRelToScObj =
            Matrix4x4.TRS(SceneObject.transform.position,
            SceneObject.transform.rotation,
            new Vector3(1.0f, 1.0f, 1.0f));

        Matrix4x4 newMatrix = bbRelToScObj.inverse * Matrix4x4.TRS(
            Box.transform.position,
            Box.transform.rotation,
            new Vector3(1.0f, 1.0f, 1.0f)
            );
        newBox.transform.rotation = Quaternion.Inverse(SceneObject.transform.rotation) * Box.transform.rotation;
        newBox.transform.position = PositionFromMatrix(newMatrix);
        newBox.transform.localScale = Box.transform.localScale;
        return newBox;
    }

    public Quaternion convertToUnity(Quaternion input)
    {
        // TODO: check if orientation conversion works as intended
        // Source: https://gamedev.stackexchange.com/questions/157946/converting-a-quaternion-in-a-right-to-left-handed-coordinate-system
        return new Quaternion(-input.x, -input.y, input.z, input.w);
    }

    //Counts the labels from last written text file for importing the labels on reloading
    public void CountLabelsOnStart()
    {
        string tpath = LabelToolManager.PathToLabels;
        var directory = new DirectoryInfo(tpath);
        var myFile = directory.GetFiles("*.txt").OrderByDescending(f => f.LastWriteTime).First().ToString();

        int numOfLines = File.ReadAllLines(myFile).Length;
        LabelToolManager.TrackID = numOfLines - 1;
        LabelToolManager.currentTrackID = numOfLines - 1;

        using (StreamReader sr = File.OpenText(myFile))
        {
            string s = "";
            while ((s = sr.ReadLine()) != null)
            {
                string[] parameters = s.Split(' ');
                if (parameters.Length != 20)
                {
                    Debug.Log("Wrong format found while reading from file: " + myFile);
                    System.IO.File.Delete(myFile);
                }
                else
                {
                    // Parse read values from string to int/double
                    int readObjID;
                    int.TryParse(parameters[0], out readObjID);
                    int readObjClass;
                    int.TryParse(parameters[1], out readObjClass);
                    int readObjQuality;
                    int.TryParse(parameters[2], out readObjQuality);
                    int readObjPriority;
                    int.TryParse(parameters[3], out readObjPriority);
                    int readObjDirection;
                    int.TryParse(parameters[4], out readObjDirection);
                    int readObjIsParking;
                    int.TryParse(parameters[5], out readObjIsParking);
                    int readObjLane;
                    int.TryParse(parameters[6], out readObjLane);
                    int readObjFirstClearAppearance;
                    int.TryParse(parameters[7], out readObjFirstClearAppearance);
                    int readObjLastClearAppearance;
                    int.TryParse(parameters[8], out readObjLastClearAppearance);
                    int readObjClearestShot;
                    int.TryParse(parameters[9], out readObjClearestShot);
                    float objPosX;
                    float.TryParse(parameters[10], out objPosX);
                    float objPosY;
                    float.TryParse(parameters[11], out objPosY);
                    float objPosZ;
                    float.TryParse(parameters[12], out objPosZ);
                    float objRotX;
                    float.TryParse(parameters[13], out objRotX);
                    float objRotY;
                    float.TryParse(parameters[14], out objRotY);
                    float objRotZ;
                    float.TryParse(parameters[15], out objRotZ);
                    float objRotW;
                    float.TryParse(parameters[16], out objRotW);
                    float objLocalScaleX;
                    float.TryParse(parameters[17], out objLocalScaleX);
                    float objLocalScaleY;
                    float.TryParse(parameters[18], out objLocalScaleY);
                    float objLocalScaleZ;
                    float.TryParse(parameters[19], out objLocalScaleZ);

                    // reload the trackInformationList 
                    int[] tempInfo = new int[6];
                    tempInfo[0] = readObjClass;
                    tempInfo[5] = readObjQuality;
                    tempInfo[1] = readObjPriority;
                    tempInfo[2] = readObjDirection;
                    tempInfo[3] = readObjIsParking;
                    tempInfo[4] = readObjLane;
                    LabelToolManager.choice = tempInfo;
                    LabelToolManager.trackInformationList.Add(new TrackInformation(readObjID, LabelToolManager.choice));

                }
            }
        }
    }
}

public class LoadedLabeledObject
{
    public int loadedReadObjID;
    public int loadedReadObjClass;
    public int loadedReadObjPriority;
    public int loadedReadObjDirection;
    public int loadedReadObjIsParking;
    public int loadedReadObjLane;
    public int loadedReadObjQuality;
    public int loadedReadObjFirstClearAppearance;
    public int loadedReadObjLastClearAppearance;
    public int loadedReadObjClearestShot;
    public float loadedObjPosX;
    public float loadedObjPosY;
    public float loadedObjPosZ;
    public float loadedObjRotX;
    public float loadedObjRotY;
    public float loadedObjRotZ;
    public float loadedObjRotW;
    public float loadedObjLocalScaleX;
    public float loadedObjLocalScaleY;
    public float loadedObjLocalScaleZ;

    public LoadedLabeledObject(int readObjID,
                        int readObjClass,
                        int readObjPriority,
                        int readObjDirection,
                        int readObjIsParking,
                        int readObjLane,
                        int readObjQuality,
                        int readObjFirstClearAppearance,
                        int readObjLastClearAppearance,
                        int readObjClearestShot,
                        float objPosX,
                        float objPosY,
                        float objPosZ,
                        float objRotX,
                        float objRotY,
                        float objRotZ,
                        float objRotW,
                        float objLocalScaleX,
                        float objLocalScaleY,
                        float objLocalScaleZ)
    {
        loadedReadObjID = readObjID;
        loadedReadObjClass = readObjClass;
        loadedReadObjPriority = readObjPriority;
        loadedReadObjDirection = readObjDirection;
        loadedReadObjIsParking = readObjIsParking;
        loadedReadObjLane = readObjLane;
        loadedReadObjQuality = readObjQuality;
        loadedReadObjFirstClearAppearance = readObjFirstClearAppearance;
        loadedReadObjLastClearAppearance = readObjLastClearAppearance;
        loadedReadObjClearestShot = readObjClearestShot;
        loadedObjPosX = objPosX;
        loadedObjPosY = objPosY;
        loadedObjPosZ = objPosZ;
        loadedObjRotX = objRotX;
        loadedObjRotY = objRotY;
        loadedObjRotZ = objRotZ;
        loadedObjRotW = objRotW;
        loadedObjLocalScaleX = objLocalScaleX;
        loadedObjLocalScaleY = objLocalScaleY;
        loadedObjLocalScaleZ = objLocalScaleZ;
    }
}