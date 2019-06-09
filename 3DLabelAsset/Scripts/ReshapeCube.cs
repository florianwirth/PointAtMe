using UnityEngine;

public class ReshapeCube : MonoBehaviour
{
    // placeholder for orientation of controller
    Vector3 rotation = new Vector3(0.0f, 0.0f, 0.0f);

    // dots at Touch Controllers
    public GameObject DotRight;
    public GameObject DotLeft;

    // Unit axis pointing upwards
    Vector3 e2 = new Vector3(0.0f, 1.0f, 0.0f);

    private void Start()
    {

    }

    private void Update()
    {

        if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.2f)
        {
            // only 1-axis rotation allowed if not explicitly set to 3-axis rotation
            rotation = DotRight.transform.rotation.eulerAngles[1] * e2;
        }
        else
        {
            // 3-axis rotation if required
            rotation = DotRight.transform.rotation.eulerAngles;
        }

        transform.localScale = Quaternion.Inverse(Quaternion.Euler(rotation)) * (DotLeft.transform.position - DotRight.transform.position);
        transform.position = (DotRight.transform.position + DotLeft.transform.position) / 2.0f;
        transform.rotation = Quaternion.Euler(rotation);

    }
}

