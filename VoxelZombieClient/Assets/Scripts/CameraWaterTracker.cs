using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraWaterTracker : MonoBehaviour
{
    private World world;

    private GameObject waterEffect;
    // Start is called before the first frame update
    void Start()
    {
        world = GameObject.FindGameObjectWithTag("Network").GetComponent<ClientVoxelEngine>().world;
        waterEffect = Instantiate(Resources.Load<GameObject>("WaterCanvas"));
       // waterEffect = GameObject.FindGameObjectWithTag("WaterCanvas").GetComponent<Canvas>();
       // waterEffect.GetComponentInChildren<Image>().enabled = false;

    }

    // Update is called once per frame
    void Update()
    {
        int x = Mathf.FloorToInt(transform.position.x);
        int y = Mathf.FloorToInt(transform.position.y);
        int z = Mathf.FloorToInt(transform.position.z);

        if(world[x, y, z] == 9)
        {
            waterEffect.GetComponentInChildren<Image>().enabled = true;
        }
        else
        {
            waterEffect.GetComponentInChildren<Image>().enabled = false;

        }
    }
}
