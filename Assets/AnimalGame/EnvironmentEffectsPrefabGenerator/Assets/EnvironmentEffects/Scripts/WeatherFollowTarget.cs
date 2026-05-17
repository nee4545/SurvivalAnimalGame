using UnityEngine;

public class WeatherFollowTarget : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = Vector3.zero;
    public bool followY = false;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 pos = target.position + offset;

        if (!followY)
            pos.y = transform.position.y;

        transform.position = pos;
    }
}
