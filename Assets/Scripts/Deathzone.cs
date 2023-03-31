using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Deathzone : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            PlayerController pc = other.gameObject.GetComponent<PlayerController>();
            if(pc)
            {
                pc.gameObject.transform.position = pc.RespawnPoint;
                pc.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
        }
        else if(other.gameObject.CompareTag("Ball"))
        {
            BallScript ball = other.gameObject.GetComponent<BallScript>();
            if(ball)
            {
                ball.gameObject.transform.position = ball.RespawnPoint;
                ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
            }
        }
    }
}
