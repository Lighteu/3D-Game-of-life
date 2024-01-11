using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector3 targetPosition;
    public float zoomSpeed = 4.0f;
    private float minZoom = 5.0f;
    private float maxZoom = 15.0f;
    public float yawSpeed = 500.0f;

    private float currentZoom = 10.0f;
    private float currentYaw = 0.0f;
    private float currentPitch = 0.0f;

    void Update()
    {
        currentZoom -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        if (Input.GetMouseButton(0))
        {
            currentYaw += Input.GetAxis("Mouse X") * yawSpeed * Time.deltaTime;
            currentPitch -= Input.GetAxis("Mouse Y") * yawSpeed * Time.deltaTime;
        }

    }

    void LateUpdate()
    {
        Vector3 dir = new Vector3(0, 0, -currentZoom);
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        transform.position = targetPosition + rotation * dir;

        transform.LookAt(targetPosition);
    }

    public void SetTarget(Vector3 newTarget)
    {
        this.targetPosition = newTarget;
    }
    public void SetMaxZoom(float max)
    {
        this.maxZoom = max;
    }
    public void SetCurrentZoom(float newZoom)
    {
        this.currentZoom = newZoom;
    }
    public float getMinZoom()
    {
        return this.minZoom;
    }
}
