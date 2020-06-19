using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraWaterTracker : MonoBehaviour
{
    private World world;

    private Canvas waterEffect;
    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.FindGameObjectWithTag("Network").GetComponent<ClientVoxelEngine>().world;
        waterEffect = GameObject.FindGameObjectWithTag("WaterCanvas").GetComponent<Canvas>();
        waterEffect.enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        int x = Mathf.FloorToInt(transform.position.x);
        int y = Mathf.FloorToInt(transform.position.y);
        int z = Mathf.FloorToInt(transform.position.z);

        if(world[x, y, z] == 9)
        {
            waterEffect.enabled = true;
        }
        else
        {
            waterEffect.enabled = false;

        }
    }
}
