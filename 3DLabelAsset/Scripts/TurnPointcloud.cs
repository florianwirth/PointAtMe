using UnityEngine;



public class TurnPointcloud : MonoBehaviour
{
    // relative transformation
    Matrix4x4 tf_rel;
    // pose/ transformation of pointcloud
    Matrix4x4 tf_pcd;
    // unit vector to keep scale
    Vector3 ones = new Vector3(1.0f, 1.0f, 1.0f);
    // hand trigger position
    bool hand_trigger_pushed = false;
    // index trigger position
    bool index_trigger_pushed = false;
    // left controller orientation
    Quaternion left_rot;
    // left controller position
    Vector3 left_pos;

    // turning speed multiplier
    float speed_mltplr = 10.0f;
    // left controller position
    Vector3 left_pos_init;


    // Use this for initialization
    void Start()
    {
        transform.localScale = new Vector3(1.0f, 1.0f, 1.0f) * LabelToolManager.scaleFactors[LabelToolManager.current_scale_idx];
    }

    // Update is called once per frame
    void Update()
    {
        OVRInput.Update();

        left_rot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
        left_pos = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);

        if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > LabelToolManager.threshold)
        {
            left_rot = Quaternion.Euler(new Vector3(0.0f, left_rot.eulerAngles[1], 0.0f));
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, transform.rotation.eulerAngles[1], 0.0f));

        }

        if (LabelToolManager.X_pressed)
        {
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f) * LabelToolManager.scaleFactors[LabelToolManager.current_scale_idx];
        }

        if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > LabelToolManager.threshold && !hand_trigger_pushed)
        {
            left_pos_init = left_pos;
            //Debug.Log("Grabbed pcd");
            tf_rel = Matrix4x4.Inverse(
                Matrix4x4.TRS(left_pos,
                left_rot,
                ones))
                * Matrix4x4.TRS(transform.position, transform.rotation, ones);
            hand_trigger_pushed = true;
        }
        else if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > LabelToolManager.threshold && hand_trigger_pushed)
        {
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > LabelToolManager.threshold && !index_trigger_pushed)
            {
                tf_rel = Matrix4x4.Inverse(
                    Matrix4x4.TRS(left_pos,
                    left_rot,
                    ones))
                    * Matrix4x4.TRS(transform.position, Quaternion.Euler(new Vector3(0.0f, transform.rotation.eulerAngles[1], 0.0f)), ones);
                index_trigger_pushed = true;
            }
            else if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) < LabelToolManager.threshold && index_trigger_pushed)
            {
                tf_rel = Matrix4x4.Inverse(
                    Matrix4x4.TRS(left_pos,
                    left_rot,
                    ones))
                    * Matrix4x4.TRS(transform.position, Quaternion.Euler(new Vector3(0.0f, transform.rotation.eulerAngles[1], 0.0f)), ones);
                index_trigger_pushed = false;
            }
            tf_pcd = Matrix4x4.TRS(left_pos,
                left_rot,
                new Vector3(1.0f, 1.0f, 1.0f)) * Matrix4x4.Translate((speed_mltplr) * (left_pos - left_pos_init)) * tf_rel;

            transform.rotation = QuaternionFromMatrix(tf_pcd);
            transform.position = PositionFromMatrix(tf_pcd);
        }
        else if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) == 0.0f && hand_trigger_pushed)
        {
            //Debug.Log("Released pcd");
            hand_trigger_pushed = false;
        }

    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Make quaternion from pose
        return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
    }

    public Vector3 PositionFromMatrix(Matrix4x4 m)
    {
        // Extract position from pose
        return new Vector3(m[12], m[13], m[14]);
    }

    public Quaternion GetNormalized(Quaternion q)
    {
        // normalize quaternion
        float f = 1f / Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        return new Quaternion(q.x * f, q.y * f, q.z * f, q.w * f);
    }

}

