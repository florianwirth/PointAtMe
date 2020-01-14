using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class ImageManager : MonoBehaviour
{

    // All .png files in directory
    static List<string> subfolders;

    public static GameObject frontPlane;
    public static GameObject rearPlane;
    public static GameObject leftPlane;
    public static GameObject rightPlane;

    public static Vector3 frontImageSize;
    public static Vector3 rearImageSize;
    public static Vector3 leftImageSize;
    public static Vector3 rightImageSize;

    public static GameObject SceneObject;

    static Material frontMat;
    static Material rearMat;
    static Material leftMat;
    static Material rightMat;

    static MeshRenderer frontMR;
    static MeshRenderer rearMR;
    static MeshRenderer leftMR;
    static MeshRenderer rightMR;

    // Use this for initialization
    void Start()
    {
        frontMat = new Material(Shader.Find("Diffuse"));
        rearMat = new Material(Shader.Find("Diffuse"));
        leftMat = new Material(Shader.Find("Diffuse"));
        rightMat = new Material(Shader.Find("Diffuse"));

        frontPlane = GameObject.Find("front");
        rearPlane = GameObject.Find("rear");
        leftPlane = GameObject.Find("left");
        rightPlane = GameObject.Find("right");

        SceneObject = GameObject.Find("SceneObject");

        subfolders = LabelToolManager.subfolders;
        subfolders.Remove("pcd");

        if (LabelToolManager.fileNamesImg.Count != 0)
            loadImages_(LabelToolManager.fileNamesImg[0]);

    }

    public static void loadImages_(string FileName)
    {
        
        foreach (string subfolder in subfolders)
        {
            try
            {
                var bytes = System.IO.File.ReadAllBytes(LabelToolManager.PathToData + "/" + subfolder + "/" + FileName + ".png");
                var tex = new Texture2D(1, 1);
                tex.LoadImage(bytes);
                Debug.Log("Loaded image <" + subfolder + "> with size: " + tex.width + " x " + tex.height);
                switch (subfolder)
                {
                    case "rear":
                        rearMat.mainTexture = tex;
                        rearMR = rearPlane.GetComponent<MeshRenderer>();
                        rearMR.material = rearMat;
                        rearMR.transform.localScale = new Vector3((float)tex.width / 1000.0f, 1.0f, (float)tex.height / 1000.0f);
                        break;
                    case "front":
                        frontMat.mainTexture = tex;
                        frontMR = frontPlane.GetComponent<MeshRenderer>();
                        frontMR.material = frontMat;
                        frontMR.transform.localScale = new Vector3((float)tex.width / 1000.0f, 1.0f, (float)tex.height / 1000.0f);
                        break;
                    case "right":
                        rightMat.mainTexture = tex;
                        rightMR = rightPlane.GetComponent<MeshRenderer>();
                        rightMR.material = rightMat;
                        rightMR.transform.localScale = new Vector3((float)tex.width / 1000.0f, 1.0f, (float)tex.height / 1000.0f);
                        break;
                    case "left":
                        leftMat.mainTexture = tex;
                        leftMR = leftPlane.GetComponent<MeshRenderer>();
                        leftMR.material = leftMat;
                        leftMR.transform.localScale = new Vector3((float)tex.width / 1000.0f, 1.0f, (float)tex.height / 1000.0f);
                        break;
                    default:
                        Debug.LogError("Folder names don't fit!");
                        break;
                }
            }
            catch(DirectoryNotFoundException)
            {
                Debug.Log("Folder <" + subfolder + "> does not exist. Images will not be displayed.");
            }
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            transform.position = SceneObject.transform.position;
            transform.rotation = SceneObject.transform.rotation;
        }
        catch
        {
            SceneObject = GameObject.Find("SceneObject");
        }

    }
}
