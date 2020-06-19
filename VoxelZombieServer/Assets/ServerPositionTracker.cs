using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerPositionTracker : MonoBehaviour
{
    public float minMoveDelta;
    private Vector3 lastPosition;
    public ushort ID;

    public ushort stateTag;

    VoxelServer vServer;
    ServerPlayerManager pManager;

    private World world;

    private void Awake()
    {
        lastPosition = transform.position;
        vServer = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelServer>();
        pManager = GameObject.FindGameObjectWithTag("Network").GetComponent<ServerPlayerManager>();
        world = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelEngine>().world;
    }

    private void Update()
    {    

        if (Vector3.Distance(lastPosition, transform.position) > minMoveDelta)
        {
            vServer.SendPositionUpdate(ID, transform.position);
            lastPosition = transform.position;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.CompareTag("Player"))
        {
            ServerPositionTracker otherTracker = collision.transform.GetComponent<ServerPositionTracker>();
            if (stateTag == 1)
            {
            
                if(otherTracker.stateTag == 0)
                {
                    Debug.Log("Infection!");
                    vServer.UpdatePlayerState(otherTracker.ID, 1);
                }

            }
     
        }
     
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Water"))
        {
            Debug.Log("In water");
            pManager.InputDictionary[ID].moveState = 1;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            Vector3 feetPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);
            Vector3 headPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);

            if(world[Mathf.FloorToInt(feetPosition.x), Mathf.FloorToInt(feetPosition.y), Mathf.FloorToInt(feetPosition.z)] != 9 && world[Mathf.FloorToInt(headPosition.x), Mathf.FloorToInt(headPosition.y), Mathf.FloorToInt(headPosition.z)] != 9)
            {
                Debug.Log("Out of water");
                pManager.InputDictionary[ID].moveState = 0;
            }
              
        }
    }
}
