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
    ServerGameManager gManager;

    private World world;

    Rigidbody rb;

    Vector3 colliderHalfExtents;

    private void Awake()
    {
        lastPosition = transform.position;

        vServer = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelServer>();
        pManager = GameObject.FindGameObjectWithTag("Network").GetComponent<ServerPlayerManager>();
        gManager = GameObject.FindGameObjectWithTag("Network").GetComponent<ServerGameManager>();
        world = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelEngine>().world;
        rb = GetComponent<Rigidbody>();

        colliderHalfExtents = new Vector3(.708f / 2, .9f, .708f / 2);
    }

    private void FixedUpdate()
    {    

          if (Vector3.Distance(lastPosition, transform.position) > minMoveDelta)
            {
            //serverTimeDelta is the time between the last input being received and this position update occurring. 
            float serverTimeDelta = Time.time - pManager.InputDictionary[ID].ServerTimeStamp;
            vServer.SendPositionUpdate(ID, transform.position);
            vServer.SendPositionUpdate(ID, transform.position, pManager.InputDictionary[ID].ClientTimeStamp, serverTimeDelta, rb.velocity);
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
                    vServer.SendPublicChat(vServer.playerNames[otherTracker.ID] + " was infected by " + vServer.playerNames[ID] + "!", 2);
                    gManager.CheckZombieWin();
                }

            }
     
        }
     
    }

    private void OnTriggerEnter(Collider other)
    {
        /*
        if(other.CompareTag("Water"))
        {
            Debug.Log("In water");
            pManager.InputDictionary[ID].moveState = 1;
        }
        */
    }

    private void OnTriggerExit(Collider other)
    {
        /*
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
        */
    }

    public ushort CheckPlayerState(ushort lastState)
    {
        Collider[] thingsHit = Physics.OverlapBox(transform.position + Vector3.down * .1f, colliderHalfExtents);

        foreach (Collider col in thingsHit)
        {
            if (col.CompareTag("Water"))
            {
                return 1;
            }
        }

        if (lastState == 0)
        {
            return 0;
        }
        else
        {
            Vector3 feetPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);
            Vector3 headPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);

            if (world[Mathf.FloorToInt(feetPosition.x), Mathf.FloorToInt(feetPosition.y), Mathf.FloorToInt(feetPosition.z)] != 9 && world[Mathf.FloorToInt(headPosition.x), Mathf.FloorToInt(headPosition.y), Mathf.FloorToInt(headPosition.z)] != 9)
            {
                return 0;

            }

            return 1;

        }

    }
}
