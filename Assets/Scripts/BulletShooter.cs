using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletShooter : MonoBehaviour
{
    [Range(0.1f, 10.0f)]
    public float ShootInterval = 2.0f;
    float timer = 2.0f;

    public GameObject BulletPrefab;

    public Transform BulletShotSpawnPoint;

    [Range(10, 1000)]
    public float LaunchVelocity = 100.0f;

    [Range(0.5f, 10.0f)]
    public float BulletLifetime = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        timer = Mathf.Max(0.1f, ShootInterval);
        if(BulletPrefab == null)
        {
            enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer -= Time.deltaTime;
        if(timer <= 0.0f)
        {
            timer = ShootInterval;
            GameObject newBullet = GameObject.Instantiate(BulletPrefab, BulletShotSpawnPoint);
            newBullet.GetComponent<Rigidbody>().velocity = newBullet.transform.forward * LaunchVelocity;
            GameObject.Destroy(newBullet, BulletLifetime);
        }
    }
}
