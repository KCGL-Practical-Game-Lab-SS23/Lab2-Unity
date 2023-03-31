using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallScript : MonoBehaviour
{
    public Vector3 RespawnPoint = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        RespawnPoint = transform.position;
    }
}
