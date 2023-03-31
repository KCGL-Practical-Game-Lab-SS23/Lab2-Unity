using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotator : MonoBehaviour
{
    public Vector3 Rotation = Vector3.zero;
    Transform Transform;

    Rigidbody body;
    public void Start()
    {
        body = GetComponent<Rigidbody>();
        Transform = transform;
    }
    public void FixedUpdate()
    {
        body.MoveRotation(body.rotation * Quaternion.Euler(Rotation.x, Rotation.y, Rotation.z));
    }
}
