using UnityEngine;

public class CameraFollowLogic : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 0.125f;
    [SerializeField] Vector3 offset;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}