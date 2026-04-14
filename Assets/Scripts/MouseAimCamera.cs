using UnityEngine;

public class MouseAimCamera : MonoBehaviour
{
    public GameObject target;
    public float rotateSpeed = 5f;
    public float verticalSpeed = 3f;

    private Vector3 offset;
    private float currentYaw;
    private float currentPitch;

    public float minPitch = -30f;
    public float maxPitch = 60f;

    void Start()
    {
        offset = transform.position - target.transform.position;

        Vector3 angles = transform.eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = Input.GetAxis("Mouse X") * rotateSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSpeed;

        currentYaw += mouseX;
        currentPitch -= mouseY;

        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        transform.position = target.transform.position + rotation * offset;

        transform.LookAt(target.transform);
    }
}