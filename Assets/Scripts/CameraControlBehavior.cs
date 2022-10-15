using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraControlBehavior : MonoBehaviour
{

    public GameObject target;
    public float margin = 0.3f;

    [HideInInspector] public Camera cam;
    private void Reset()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        Vector3 viewPos = cam.WorldToViewportPoint(target.transform.position);
        Vector3 upperLeft = cam.ViewportToWorldPoint(margin * Vector3.one);
        Vector3 lowerRight = cam.ViewportToWorldPoint((1 - margin) * Vector3.one);
        Vector3 targetPos = transform.position;
        if (viewPos.x < margin)
        {
            targetPos.x += target.transform.position.x - upperLeft.x;
        }
        if (viewPos.x > 1 - margin)
        {
            targetPos.x += target.transform.position.x - lowerRight.x;
        }
        if (viewPos.y < margin)
        {
            targetPos.y += target.transform.position.y - upperLeft.y;
        }
        if (viewPos.y > 1 - margin)
        {
            targetPos.y += target.transform.position.y - lowerRight.y;
        }
        transform.position = targetPos;
    }
}
