using UnityEngine;

public class CapController : MonoBehaviour
{
    public GameConfig config;

    private Rigidbody rb;
    private float angle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        angle += config.capRotationSpeed * Time.fixedDeltaTime;
        Quaternion rot = Quaternion.AngleAxis(angle * Mathf.Rad2Deg, Vector3.up);
        rb.MoveRotation(rot);
    }

    public void ResetAngle()
    {
        angle = 0f;
        rb.MoveRotation(Quaternion.identity);
    }
}
