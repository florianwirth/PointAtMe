using UnityEngine;


public class ShowImages : MonoBehaviour
{
    // state of Y button
    bool y_pressed = false;
    // overall scaling factor of image planes
    public float Scale = 1.7f;

    Vector3 plane_size;

    bool isFirstUpdate = true;


    // Use this for initialization
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        OVRInput.Update();
        //OVRInput.FixedUpdate();
        if (isFirstUpdate)
        {
            plane_size = transform.localScale;
            transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
            isFirstUpdate = false;
        }

        if (!OVRInput.Get(OVRInput.RawButton.Y) && y_pressed)
        {
            transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
            y_pressed = false;
        }
        else if (OVRInput.Get(OVRInput.RawButton.Y) && !y_pressed)
        {
            transform.localScale = plane_size * Scale;
            y_pressed = true;

        }

    }
}

