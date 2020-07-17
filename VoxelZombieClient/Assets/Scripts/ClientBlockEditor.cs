using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using System;

namespace Client
{
    public class ClientBlockEditor : MonoBehaviour
    {

        private Camera playerCam;
        public float editDistance;
        public double stepDistance;
        private World currentWorld;
        private VoxelClient vClient;

        const ushort BLOCK_TAG = 3;

        private ushort placeBlockTag = 1;

        public LineRenderer blockOutline;

        private Vector3[] _frontVertices = new[]
    {
        new Vector3 (0, 0, -.05f),
        new Vector3 (1, 0, -.05f),
        new Vector3 (1, 1, -.05f),
        new Vector3 (0, 1, -.05f),
        new Vector3 (0, 0, -.05f)
    };

        private Vector3[] _topVertices = new[]
        {
        new Vector3 (1, 1.05f, 0),
        new Vector3 (0, 1.05f, 0),
        new Vector3 (0, 1.05f, 1),
        new Vector3 (1, 1.05f, 1),
        new Vector3 (1, 1.05f, 0)
    };


        private Vector3[] _topHalfVertices = new[]
        {
        new Vector3 (1, .55f, 0),
        new Vector3 (0, .55f, 0),
        new Vector3 (0, .55f, 1),
        new Vector3 (1, .55f, 1),
         new Vector3 (1, .55f, 0)
    };

        private Vector3[] _rightVertices = new[]
        {
        new Vector3 (1.05f, 0, 0),
        new Vector3 (1.05f, 1, 0),
        new Vector3 (1.05f, 1, 1),
        new Vector3 (1.05f, 0, 1),
        new Vector3 (1.05f, 0, 0)
    };

        private Vector3[] _leftVertices = new[]
        {
      new Vector3 (-.05f, 0, 0),
      new Vector3 (-.05f, 1, 0),
      new Vector3 (-.05f, 1, 1),
      new Vector3 (-.05f, 0, 1),
      new Vector3 (-.05f, 0, 0)
    };

        private Vector3[] _backVertices = new[]
        {
        new Vector3 (0, 1, 1.05f),
        new Vector3 (1, 1, 1.05f),
        new Vector3 (1, 0, 1.05f),
        new Vector3 (0, 0, 1.05f),
        new Vector3 (0, 1, 1.05f)
    };

        private Vector3[] _bottomVertices = new[]
        {
       new Vector3 (0, -.05f, 0),
       new Vector3 (1, -.05f, 0),
       new Vector3 (1, -.05f, 1),
       new Vector3 (0, -.05f, 1),
       new Vector3 (0, -.05f, 0)
    };

        private Vector3 selectionPosition;
        private Vector3 selectionNormal;

        private Vector3 halfBlockNormal = new Vector3(0, .1f, 0);
        // Start is called before the first frame update
        void Start()
        {
            playerCam = GetComponentInChildren<Camera>();
            currentWorld = GameObject.FindGameObjectWithTag("Network").GetComponent<ClientVoxelEngine>().world;
            vClient = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelClient>();

            blockOutline.positionCount = 0;
        }

        // Update is called once per frame
        void Update()
        {

            ShowSelection();

            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                BreakBlock();
            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                PlaceBlock();
            }
            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                SelectBlock();
            }
        }

        void ShowSelection()
        {
            RaycastHit[] hitData = Physics.RaycastAll(playerCam.transform.position - playerCam.transform.forward, playerCam.transform.forward, editDistance);

            foreach (RaycastHit hit in hitData)
            {
                if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Water"))
                {
                  
                    float pointX = hit.point.x;
                    float pointY = hit.point.y;
                    float pointZ = hit.point.z;

                    pointX += playerCam.transform.forward.x * .0001f;
                    pointY += playerCam.transform.forward.y * .0001f;
                    pointZ += playerCam.transform.forward.z * .0001f;

                    int x = Mathf.FloorToInt(pointX);
                    int y = Mathf.FloorToInt(pointY);
                    int z = Mathf.FloorToInt(pointZ);

                    if (currentWorld[x, y, z] == 0)
                    {
                        blockOutline.positionCount = 0;

                        //if nothing is hit make normal 0
                        selectionNormal = Vector3.zero;
                        return;
                    }

                    Vector3 blockOffset = new Vector3(x, y, z);
                    blockOutline.positionCount = 5;
                    if (hit.normal == Vector3.up)
                    {
                        if(currentWorld[x,y,z] == 44)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                blockOutline.SetPosition(i, blockOffset + _topHalfVertices[i]);

                            }


                            selectionPosition = new Vector3(x, y, z);
                            selectionNormal = halfBlockNormal;

                            return;
                        }
                        else
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                blockOutline.SetPosition(i, blockOffset + _topVertices[i]);
                            }
                        }
                      
                    }
                    else if (hit.normal == Vector3.back)
                    {

                        for (int i = 0; i < 5; i++)
                        {
                            blockOutline.SetPosition(i, blockOffset + _frontVertices[i]);
                        }

                    }
                    else if (hit.normal == Vector3.right)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            blockOutline.SetPosition(i, blockOffset + _rightVertices[i]);
                        }

                    }
                    else if (hit.normal == Vector3.left)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            blockOutline.SetPosition(i, blockOffset + _leftVertices[i]);
                        }

                    }
                    else if (hit.normal == Vector3.forward)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            blockOutline.SetPosition(i, blockOffset + _backVertices[i]);
                        }

                    }
                    else if (hit.normal == Vector3.down)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            blockOutline.SetPosition(i, blockOffset + _bottomVertices[i]);
                        }

                    }

                    selectionPosition = new Vector3(x, y, z);
                    selectionNormal = hit.normal;

                    return;
                }

            }

          
            blockOutline.positionCount = 0;

            //if nothing is hit make normal 0
            selectionNormal = Vector3.zero;
        }

        void BreakBlock()
        {
            if (selectionNormal != Vector3.zero)
            {
                int x = (int)selectionPosition.x;
                int y = (int)selectionPosition.y;
                int z = (int)selectionPosition.z;

                ushort breakSpotTag = currentWorld[x, y, z];

                if (breakSpotTag == 0)
                {
                    Debug.Log("Error, no block there");
                    return;
                }

                //bedrock, water, and lava can not be broken
                if (breakSpotTag == 7 || breakSpotTag == 9 || breakSpotTag == 11)
                {
                    return;
                }

                vClient.SendBlockEdit((ushort)x, (ushort)y, (ushort)z, 0);
                return;
            }

        }

        void PlaceBlock()
        {
            if(selectionNormal == halfBlockNormal)
            {
                int x = (int)(selectionPosition.x);
                int y = (int)(selectionPosition.y);
                int z = (int)(selectionPosition.z);

                if(placeBlockTag == 44)
                {
                    vClient.SendBlockEdit((ushort)x, (ushort)y, (ushort)z, 43);
                }
                else
                {
                    y++;
                    ushort placeSpotTag = currentWorld[x, y, z];

                    if (placeSpotTag == 0 || placeSpotTag == 9 || placeSpotTag == 11)
                    {
                        vClient.SendBlockEdit((ushort)x, (ushort)y, (ushort)z, placeBlockTag);
                    }
                }

            }
            else if (selectionNormal != Vector3.zero)
            {
                int x = (int)(selectionPosition.x + selectionNormal.x);
                int y = (int)(selectionPosition.y + selectionNormal.y);
                int z = (int)(selectionPosition.z + selectionNormal.z);

                ushort placeSpotTag = currentWorld[x, y, z];

                if (placeSpotTag == 0 || placeSpotTag == 9 || placeSpotTag == 11)
                {
                    vClient.SendBlockEdit((ushort)x, (ushort)y, (ushort)z, placeBlockTag);
                }

                return;
            }
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
}

