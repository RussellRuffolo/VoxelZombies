﻿using System.Collections;
using System.Net;
using System.Collections.Generic;
using UnityEngine;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;

namespace Client
{
    public class VoxelClient : MonoBehaviour
    {
     
        public bool loadedFirstMap = false;

        public UnityClient Client;

        ClientVoxelEngine vEngine;
        private World world;

        public GameObject NetworkPlayerPrefab;
        public GameObject LocalPlayerPrefab;
        public GameObject LocalPlayerSimulator;

        Dictionary<ushort, Transform> NetworkPlayerDictionary = new Dictionary<ushort, Transform>();
        Transform localPlayerTransform;
        Transform localSimTransform;

        public GameObject loginCamera;


        ClientChatManager chatManager;

        List<ChunkID> dirtiedChunks = new List<ChunkID>();

        // public Canvas ZombieCanvas;

        private void Awake()
        {

            Client = GetComponent<UnityClient>();

            vEngine = GetComponent<ClientVoxelEngine>();
            world = vEngine.world;

            Client.MessageReceived += MessageReceived;

            chatManager = GetComponent<ClientChatManager>();

            //zombieCanvas.enabled = false;
        }

        void MessageReceived(object sender, MessageReceivedEventArgs e)
        {


            using (Message message = e.GetMessage() as Message)
            {

                switch (message.Tag)
                {
                    case Tags.MAP_TAG:
                        //Debug.Log("Received Map Message");
                        LoadMap(e);
                        break;
                    case Tags.PLAYER_INIT_TAG:
                        //Debug.Log("Received Player Init Message");
                        InitPlayers(e);
                        break;
                    case Tags.ADD_PLAYER_TAG:
                       // Debug.Log("Received Player Add Message");
                        AddPlayer(e);
                        break;
                    case Tags.BLOCK_EDIT_TAG:
                       // Debug.Log("Received Block Edit Message");
                        ApplyBlockEdit(e);
                        break;
                    case Tags.OTHER_POSITION_TAG:
                       // Debug.Log("Received Position Update Message");
                        MovePlayer(e);
                        break;
                    case Tags.CLIENT_POSITION_TAG:
                       // Debug.Log("Received Client Position Message");
                        PerformClientPrediction(e);
                        break;
                    case Tags.PLAYER_STATE_TAG:
                      //  Debug.Log("Received Player State Message");
                        SetPlayerState(e);
                        break;
                    case Tags.REMOVE_PLAYER_TAG:
                       // Debug.Log("Received Player Remove Message");
                        RemovePlayer(e);
                        break;
                    case Tags.CHAT_TAG:
                       // Debug.Log("Received Chat Message");
                        ReceiveChat(e);
                        break;
                    default:
                        Debug.Log("Received message with tag: " + e.Tag);                    

                        break;

                }

            }

        }

        void PerformClientPrediction(MessageReceivedEventArgs e)
        {
            using (Message message = e.GetMessage() as Message)
            {
                using (DarkRiftReader reader = message.GetReader())
                {
                    ushort id = reader.ReadUInt16();

                    Vector3 serverPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
      

                    int clientTickNumber = reader.ReadInt32();                 

                    Vector3 velocity = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

                    localSimTransform.GetComponent<ClientPlayerController>().ClientPrediction(serverPosition, clientTickNumber, velocity);
                }
            }
        }


        void LoadMap(MessageReceivedEventArgs e)
        {
            string mapName = "";
            using (Message message = e.GetMessage() as Message)
            {

                using (DarkRiftReader reader = message.GetReader())
                {            
                    string MapName = reader.ReadString();
                    mapName = MapName;

                    vEngine.LoadMap(MapName);
                }


            }

         
            using (DarkRiftWriter reloadedWriter = DarkRiftWriter.Create())
            {
                reloadedWriter.Write(mapName);
                using (Message MapReloadedMessage = Message.Create(Tags.MAP_RELOADED_TAG, reloadedWriter))
                {
                    Client.SendMessage(MapReloadedMessage, SendMode.Reliable);
                    Debug.Log("Sent map reloaded message");

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

                    for (int i = 0; i < numPlayers; i++)
                    {
                        ushort PlayerID = reader.ReadUInt16();
                        ushort StateTag = reader.ReadUInt16();

                        Vector3 position = new Vector3(reader.ReadSingle(),
                                           reader.ReadSingle(), reader.ReadSingle());

                        Vector3 eulerRotation = new Vector3(reader.ReadSingle(),
                                                reader.ReadSingle(), reader.ReadSingle());

                        string playerName = reader.ReadString();

                        if (PlayerID == Client.ID)
                        {
                            Destroy(loginCamera);
                            GameObject LocalPlayer = GameObject.Instantiate(LocalPlayerPrefab,
                                         position, Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z));

                            GameObject LocalPlayerSim = GameObject.Instantiate(LocalPlayerSimulator,
                                         position, Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z));

                            LocalPlayer.GetComponent<ClientCameraController>().LocalPlayerSim = LocalPlayerSim.transform;
                            LocalPlayerSim.GetComponent<ClientPlayerController>().camController = LocalPlayer.GetComponent<ClientCameraController>();
                            if (StateTag == 0)
                            {
                                LocalPlayer.GetComponent<MeshRenderer>().material.color = Color.white;
                            }
                            else
                            {
                                LocalPlayer.GetComponent<MeshRenderer>().material.color = Color.red;
                            }

                            chatManager.SetInputColor(StateTag);

                            localPlayerTransform = LocalPlayer.transform;
                            localSimTransform = LocalPlayerSim.transform;
                        }
                        else
                        {
                            if(!NetworkPlayerDictionary.ContainsKey(PlayerID))
                            {
                                Debug.Log("Spawn Network Player");
                                GameObject NetworkPlayer = GameObject.Instantiate(NetworkPlayerPrefab,
                                             position, Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z));

                                NetworkPlayer.GetComponentInChildren<NameTagManager>().SetName(playerName);

                                if (StateTag == 0)
                                {
                                    NetworkPlayer.GetComponentInChildren<MeshRenderer>().material.color = Color.white;
                                    NetworkPlayer.GetComponent<NetworkMotionSmoother>().playerAnim.SetBool("IsHuman", true);
                                }
                                else
                                {
                                    NetworkPlayer.GetComponentInChildren<MeshRenderer>().material.color = Color.red;
                                    NetworkPlayer.GetComponent<NetworkMotionSmoother>().playerAnim.SetBool("IsHuman", false);
                                }

                                NetworkPlayerDictionary.Add(PlayerID, NetworkPlayer.transform);
                            }
                    
                        }

                        foreach(Transform netplayerTransform in NetworkPlayerDictionary.Values)
                        {
                            netplayerTransform.GetComponentInChildren<NameTagManager>().SetPlayerTransform(localPlayerTransform);
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

                    if(!NetworkPlayerDictionary.ContainsKey(PlayerID))
                    {
                        Vector3 position = new Vector3(reader.ReadSingle(),
                                      reader.ReadSingle(), reader.ReadSingle());

                        Vector3 eulerRotation = new Vector3(reader.ReadSingle(),
                                                reader.ReadSingle(), reader.ReadSingle());

                        ushort stateTag = reader.ReadUInt16();

                        string playerName = reader.ReadString();

                        GameObject NetworkPlayer = GameObject.Instantiate(NetworkPlayerPrefab,
                                        position, Quaternion.Euler(eulerRotation.x, eulerRotation.y, eulerRotation.z));
                        NetworkPlayer.GetComponentInChildren<NameTagManager>().SetName(playerName);
                        NetworkPlayer.GetComponentInChildren<NameTagManager>().SetPlayerTransform(localPlayerTransform);

                        Color newColor;

                        if (stateTag == 0)
                        {
                            newColor = Color.white;
                            NetworkPlayer.GetComponent<NetworkMotionSmoother>().playerAnim.SetBool("IsHuman", true);
                        }
                        else
                        {
                            newColor = Color.red;
                            NetworkPlayer.GetComponent<NetworkMotionSmoother>().playerAnim.SetBool("IsHuman", false);
                        }

                        NetworkPlayer.GetComponentInChildren<MeshRenderer>().material.color = newColor;
                        NetworkPlayerDictionary.Add(PlayerID, NetworkPlayer.transform);
                    }                   

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

        public void SendInputs(ClientInputs[] inputsArray)
        {
            int numInputs = inputsArray.Length;
            using (DarkRiftWriter InputWriter = DarkRiftWriter.Create())
            {
                for(int i = 0; i < numInputs; i++)
                {
                    InputWriter.Write(inputsArray[i].MoveVector.x);
                    InputWriter.Write(inputsArray[i].MoveVector.y);
                    InputWriter.Write(inputsArray[i].MoveVector.z);

                    InputWriter.Write(inputsArray[i].Jump);

                    InputWriter.Write(inputsArray[i].TickNumber);
                }          

                using (Message InputMessage = Message.Create(Tags.INPUT_TAG, InputWriter))
                {
                    Client.SendMessage(InputMessage, SendMode.Unreliable);
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

                using (Message message = Message.Create(Tags.BLOCK_EDIT_TAG, blockWriter))
                    Client.SendMessage(message, SendMode.Reliable);
            }
        }

        public void SendChatMessage(string chatMessage, ushort colorTag)
        {
            using (DarkRiftWriter chatWriter = DarkRiftWriter.Create())
            {
                chatWriter.Write(chatMessage);
                chatWriter.Write(colorTag);

                using (Message message = Message.Create(Tags.CHAT_TAG, chatWriter))
                    Client.SendMessage(message, SendMode.Reliable);
            }
        }

        void ApplyBlockEdit(MessageReceivedEventArgs e)
        {
            using (DarkRiftReader reader = e.GetMessage().GetReader())
            {

                int numBlocks = reader.Length / 8;

                for (int i = 0; i < numBlocks; i++)
                {
                    ushort x = reader.ReadUInt16();
                    ushort y = reader.ReadUInt16();
                    ushort z = reader.ReadUInt16();

                    ushort blockTag = reader.ReadUInt16();
                    world[x, y, z] = blockTag;

                    dirtiedChunks.Add(ChunkID.FromWorldPos(x, y, z));

                    if (x % 16 == 0)
                    {
                        if (x != 0)
                        {
                            dirtiedChunks.Add(ChunkID.FromWorldPos(x - 1, y, z));
                        }
                    }
                    else if (x % 16 == 15)
                    {
                        if (x != vEngine.Length - 1)
                        {
                            dirtiedChunks.Add(ChunkID.FromWorldPos(x + 1, y, z));
                        }
                    }

                    if (y % 16 == 0)
                    {
                        if (y != 0)
                        {
                            dirtiedChunks.Add(ChunkID.FromWorldPos(x, y - 1, z));
                        }
                    }
                    else if (y % 16 == 15)
                    {
                        if (y != vEngine.Height - 1)
                        {
                            dirtiedChunks.Add(ChunkID.FromWorldPos(x, y + 1, z));
                        }
                    }

                    if (z % 16 == 0)
                    {
                        if (z != 0)
                        {
                            dirtiedChunks.Add(ChunkID.FromWorldPos(x, y, z - 1));
                        }
                    }
                    else if (z % 16 == 15)
                    {
                        if (z != vEngine.Width - 1)
                        {
                            dirtiedChunks.Add(ChunkID.FromWorldPos(x, y, z + 1));
                        }
                    }


                    foreach (ChunkID ID in dirtiedChunks)
                    {
                        world.Chunks[ID].dirty = true;
                    }

                    dirtiedChunks.Clear();

                }

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

                float yRot = reader.ReadSingle();
                Debug.Log("Recieved rotation " + yRot);
                bool inWater = reader.ReadBoolean();

                Vector3 position = new Vector3(x, y, z);

          
                if (NetworkPlayerDictionary.ContainsKey(ID))
                {
                    NetworkPlayerDictionary[ID].GetComponent<NetworkMotionSmoother>().SetValues(position, yRot, inWater);
                }
                else
                {
                    Debug.Log("No Network Player corresponds to given ID: " + ID);
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

                if (stateTag == 0)
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
                    chatManager.SetInputColor(stateTag);
                    if (stateTag == 0)
                    {
                    }
                    else
                    {
                        // ZombieCanvas.enabled = false;
                    }
                }
                else
                {
                    if (NetworkPlayerDictionary.ContainsKey(ID))
                    {
                        NetworkPlayerDictionary[ID].GetComponentInChildren<MeshRenderer>().material.color = newColor;
                        if(stateTag == 0)
                        {
                            NetworkPlayerDictionary[ID].GetComponent<NetworkMotionSmoother>().playerAnim.SetBool("IsHuman", true);

                        }
                        else
                        {
                            NetworkPlayerDictionary[ID].GetComponent<NetworkMotionSmoother>().playerAnim.SetBool("IsHuman", false);
                        }
                        
                    }
                    else
                    {
                        Debug.LogError("No Network Player corresponds to given ID: " + ID);
                    }

                }

            }

        }


        void ReceiveChat(MessageReceivedEventArgs e)
        {
            using (DarkRiftReader reader = e.GetMessage().GetReader())
            {
                string chatMessage = reader.ReadString();
                ushort colorTag = reader.ReadUInt16();

                chatManager.DisplayMessage(chatMessage, colorTag);
            }
        }

    }
}

