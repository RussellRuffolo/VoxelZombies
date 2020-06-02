using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System;

public class ClientBlockEditor : MonoBehaviour
{

    private Camera playerCam;
    public float editDistance;
    public double stepDistance;
    private World currentWorld;
    private VoxelClient vClient;

    const ushort BLOCK_TAG = 3;

    private ushort placeBlockTag = 1;
    // Start is called before the first frame update
    void Start()
    {
        playerCam = GetComponentInChildren<Camera>();
        currentWorld = GameObject.FindGameObjectWithTag("Network").GetComponent<ClientVoxelEngine>().world;
        vClient = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelClient>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            BreakBlock();
        }

        if(Input.GetKeyDown(KeyCode.Mouse1))
        {
            PlaceBlock();
        }
        if(Input.GetKeyDown(KeyCode.Mouse2))
        {
            SelectBlock();
        }
    }

    void BreakBlock()
    {
        RaycastHit[] hitData = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, editDistance);
        
        foreach(RaycastHit hit in hitData)
        {
            if(!hit.collider.CompareTag("Player"))
            {                
                float pointX = hit.point.x;
                float pointY = hit.point.y;
                float pointZ = hit.point.z;

                pointX += playerCam.transform.forward.x * .01f;
                pointY += playerCam.transform.forward.y * .01f;
                pointZ += playerCam.transform.forward.z * .01f;
             
                int x = Mathf.FloorToInt(pointX);
                int y = Mathf.FloorToInt(pointY);
                int z = Mathf.FloorToInt(pointZ);
              
                if(currentWorld[x,y,z] == 0)
                {
                    Debug.Log("Error, no block there");
                    return;
                }

                //7 is bedrock
                if (currentWorld[x, y, z] == 7)
                {
                    return;
                }

                vClient.SendBlockEdit((ushort)x, (ushort)y, (ushort)z, 0);
                return;
            }
        }
    }

    void PlaceBlock()
    {
        float rayDistance = 0;
        double rayX, rayY, rayZ;
        double xDir, yDir, zDir;

        int x, y, z;

        rayX = playerCam.transform.position.x;
        rayY = playerCam.transform.position.y;
        rayZ = playerCam.transform.position.z;

        xDir = playerCam.transform.forward.x;
        yDir = playerCam.transform.forward.y;
        zDir = playerCam.transform.forward.z;


        while(rayDistance < editDistance)
        {
            rayX += xDir * stepDistance;
            rayY += yDir * stepDistance;
            rayZ += zDir * stepDistance;

            x = (int)Math.Floor(rayX);
            y = (int)Math.Floor(rayY);
            z = (int)Math.Floor(rayZ);

            if (currentWorld[x, y, z] != 0)
            {
                Debug.Log("found a block");
                RaycastHit[] hitData = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, editDistance);

                foreach (RaycastHit hit in hitData)
                {
                    // This finds the face of the block hit 
                    if (!hit.collider.CompareTag("Player"))
                    {
                        x += (int)hit.normal.x;
                        y += (int)hit.normal.y;
                        z += (int)hit.normal.z;

                        if (currentWorld[x, y, z] == 0)
                        {
                            vClient.SendBlockEdit((ushort)x, (ushort)y, (ushort)z, placeBlockTag);
                        }
                        else
                        {
                            /*
                            Debug.Log("Block already there" + hit.point + " " + x + y + z);
                            y += 1;
                            currentWorld[x, y, z] = 1;
                            */

                        }

                        //ChunkID ID = ChunkID.FromWorldPos(x, y, z);
                       // currentWorld.Chunks[ID].RenderToMesh();
                        return;

                    }
                }
            }
            rayDistance += (float)stepDistance;
        }
      
                return;
   
    }

    void SelectBlock()
    {
        RaycastHit[] hitData = Physics.RaycastAll(playerCam.transform.position, playerCam.transform.forward, editDistance);

        foreach (RaycastHit hit in hitData)
        {
            if (!hit.collider.CompareTag("Player"))
            {
                float pointX = hit.point.x;
                float pointY = hit.point.y;
                float pointZ = hit.point.z;

                pointX += playerCam.transform.forward.x * .01f;
                pointY += playerCam.transform.forward.y * .01f;
                pointZ += playerCam.transform.forward.z * .01f;

                int x = Mathf.FloorToInt(pointX);
                int y = Mathf.FloorToInt(pointY);
                int z = Mathf.FloorToInt(pointZ);

                if (currentWorld[x, y, z] == 0)
                {
                    Debug.Log("Error, no block there");
                    return;
                }

                //7 is bedrock
                if (currentWorld[x, y, z] == 7)
                {
                    return;
                }

                placeBlockTag = currentWorld[x, y, z];
                return;
            }
        }
    }
}
