using System.Collections;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

public class VoxelClient : MonoBehaviour
{
    const ushort MAP_TAG = 0;
    const ushort PLAYER_INIT_TAG = 1;
    const ushort ADD_PLAYER_TAG = 2;
    const ushort INPUT_TAG = 3;
    const ushort BLOCK_EDIT_TAG = 4;
    const ushort POSITION_UPDATE_TAG = 5;
    const ushort PLAYER_STATE_TAG = 6;
    const ushort REMOVE_PLAYER_TAG = 7;


    UnityClient Client;

    ClientVoxelEngine vEngine;
    private World world;

    public GameObject NetworkPlayerPrefab;
    public GameObject LocalPlayerPrefab;

    Dictionary<ushort, Transform> NetworkPlayerDictionary = new Dictionary<ushort, Transform>();
    Transform localPlayerTransform;

    private void Awake()
    {

        Client = GetComponent<UnityClient>();

        vEngine = GetComponent<ClientVoxelEngine>();
        world = vEngine.world;

        //Client.Connect(IPAddress.Parse("127.0.0.1"), 4296, false);
        Client.MessageReceived += MessageReceived;
    }

    void MessageReceived(object sender, MessageReceivedEventArgs e)
    {

      
        using (Message message = e.GetMessage() as Message)
        {

            switch (message.Tag)
            {
                case MAP_TAG:
                    Debug.Log("Received Map Message");
                    LoadMap(e);
                    break;
                case PLAYER_INIT_TAG:
                    Debug.Log("Received Player Init Message");
                    InitPlayers(e);
                    break;
                case ADD_PLAYER_TAG:
                    Debug.Log("Received Player Add Message");
                    AddPlayer(e);
                    break;
                case BLOCK_EDIT_TAG:
                    Debug.Log("Received Block Edit Message");
                    ApplyBlockEdit(e);
                    break;
                case POSITION_UPDATE_TAG:
                    Debug.Log("Received Position Update Message");
                    MovePlayer(e);
                    break;
                case PLAYER_STATE_TAG:
                    Debug.Log("Received Player State Message");
                    SetPlayerState(e);
                    break;
                case REMOVE_PLAYER_TAG:
                    Debug.Log("Received Player Remove Message");
                    RemovePlayer(e);
                    break;

            }
            
       }       

    }

    void LoadMap(MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            
            using (DarkRiftReader reader = message.GetReader())
            {
                int Width = reader.ReadInt32();
                int Length = reader.ReadInt32();             
                int Height = reader.ReadInt32();
                byte[] mapBytes = reader.ReadBytes();
                Debug.Log(mapBytes.Length);
                vEngine.LoadMap(Width, Length, Height, mapBytes);
            }
            
            
        }

    }

    void InitPlayers(MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                int numPlayers = reader.ReadInt32();

                for(int i = 0; i < numPlayers; i++)
                {
                    ushort PlayerID = reader.ReadUInt16();
                    ushort StateTag = reader.ReadUInt16();
                    
                    Vector3 position = new Vector3(reader.ReadSingle(), 
                                       reader.ReadSingle(), reader.ReadSingle());

                    Vector3 eulerRotation = new Vector3(reader.ReadSingle(),
                                            reader.ReadSingle(), reader.ReadSingle());
                    if(PlayerID == Client.ID)
                    {
                        Debug.Log("Spawn Local Player");
                        GameObject LocalPlayer = GameObject.Instantiate(LocalPlayerPrefab,
                                     position, Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z));
                        if(StateTag == 0)
                        {
                            LocalPlayer.GetComponent<MeshRenderer>().material.color = Color.white;
                        }
                        else
                        {
                            LocalPlayer.GetComponent<MeshRenderer>().material.color = Color.red;
                        }
                        localPlayerTransform = LocalPlayer.transform;
                    }
                    else
                    {
                        Debug.Log("Spawn Network Player");
                        GameObject NetworkPlayer = GameObject.Instantiate(NetworkPlayerPrefab,
                                     position, Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z));

                        if (StateTag == 0)
                        {
                            NetworkPlayer.GetComponent<MeshRenderer>().material.color = Color.white;
                        }
                        else
                        {
                            NetworkPlayer.GetComponent<MeshRenderer>().material.color = Color.red;
                        }

                        NetworkPlayerDictionary.Add(PlayerID, NetworkPlayer.transform);
                    }
          

                }
            }
        }
    }

    void AddPlayer(MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                ushort PlayerID = reader.ReadUInt16();


                Vector3 position = new Vector3(reader.ReadSingle(),
                                   reader.ReadSingle(), reader.ReadSingle());

                Vector3 eulerRotation = new Vector3(reader.ReadSingle(),
                                        reader.ReadSingle(), reader.ReadSingle());
              
                GameObject NetworkPlayer = GameObject.Instantiate(NetworkPlayerPrefab,
                                position, Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z));

                NetworkPlayerDictionary.Add(PlayerID, NetworkPlayer.transform);
                
            }
        }

    }

    void RemovePlayer(MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
            using (DarkRiftReader reader = message.GetReader())
            {
                ushort PlayerID = reader.ReadUInt16();

                Transform playerToRemove = NetworkPlayerDictionary[PlayerID];

                NetworkPlayerDictionary.Remove(PlayerID);

                Destroy(playerToRemove.gameObject);
            }
        }

    }

    public void SendInputs(Vector3 moveVector, bool Jump)
    {
        using (DarkRiftWriter InputWriter = DarkRiftWriter.Create())
        {
            InputWriter.Write(moveVector.x);
            InputWriter.Write(moveVector.y);
            InputWriter.Write(moveVector.z);

            InputWriter.Write(Jump);

            using (Message InputMessage = Message.Create(INPUT_TAG, InputWriter))
            {
                Client.SendMessage(InputMessage, SendMode.Reliable);
            }

        }
    }

    public void SendBlockEdit(ushort x, ushort y, ushort z, ushort blockTag)
    {
        using (DarkRiftWriter blockWriter = DarkRiftWriter.Create())
        {
            blockWriter.Write(x);
            blockWriter.Write(y);
            blockWriter.Write(z);
            blockWriter.Write(blockTag);

            using (Message message = Message.Create(BLOCK_EDIT_TAG, blockWriter))
                Client.SendMessage(message, SendMode.Reliable);
        }
    }

    void ApplyBlockEdit(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            ushort x = reader.ReadUInt16();
            ushort y = reader.ReadUInt16();
            ushort z = reader.ReadUInt16();

            ushort blockTag = reader.ReadUInt16();        
            world[x, y, z] = blockTag;               

            world.Chunks[ChunkID.FromWorldPos(x, y, z)].dirty = true;
            

        }
    }

    void MovePlayer(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            ushort ID = reader.ReadUInt16();

            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();

            Vector3 position = new Vector3(x, y, z);

            if(ID == Client.ID)
            {
                if(localPlayerTransform != null)
                    localPlayerTransform.position = position;
            }
            else
            {
                if(NetworkPlayerDictionary.ContainsKey(ID))
                {
                    NetworkPlayerDictionary[ID].position = position;
                }
                else
                {
                    Debug.LogError("No Network Player corresponds to given ID: " + ID);
                }
   
            }


        }
    }

    void SetPlayerState(MessageReceivedEventArgs e)
    {
       
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            ushort ID = reader.ReadUInt16();
            //0 is human, 1 is zombie
            ushort stateTag = reader.ReadUInt16();

            Color newColor;

            if(stateTag == 0)
            {
                newColor = Color.white;
            }
            else
            {
                newColor = Color.red;
            }

            if (ID == Client.ID)
            {
                localPlayerTransform.GetComponent<MeshRenderer>().material.color = newColor;
            }
            else
            {
                if (NetworkPlayerDictionary.ContainsKey(ID))
                {
                    NetworkPlayerDictionary[ID].GetComponent<MeshRenderer>().material.color = newColor;
                }
                else
                {
                    Debug.LogError("No Network Player corresponds to given ID: " + ID);
                }

            }

        }

    }

}
