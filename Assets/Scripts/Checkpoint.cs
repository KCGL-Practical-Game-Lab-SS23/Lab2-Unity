using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnTriggerEnter(Collider collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if(pc)
            {
                pc.RespawnPoint = transform.position;
            }
        }
        else if (collision.gameObject.CompareTag("Ball"))
        {
            BallScript ball = collision.gameObject.GetComponent<BallScript>();
            if (ball)
            {
                ball.RespawnPoint = transform.position;
            }
        }
    }
}
