using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using UnityEngine;

public class VoxelServer : MonoBehaviour
{
    const ushort MAP_TAG = 0;
    const ushort PLAYER_INIT_TAG = 1;
    const ushort ADD_PLAYER_TAG = 2;
    const ushort INPUT_TAG = 3;
    const ushort BLOCK_EDIT_TAG = 4;
    const ushort POSITION_UPDATE_TAG = 5;
    const ushort PLAYER_STATE_TAG = 6;
    const ushort REMOVE_PLAYER_TAG = 7;
    const ushort MAP_LOADED_TAG = 8;
    const ushort MAP_RELOADED_TAG = 9;

    XmlUnityServer XMLServer;
    DarkRiftServer Server;
    VoxelEngine vEngine;
    ServerPlayerManager PlayerManager;
    ServerGameManager gManager;

    World world;

    //players who have loaded the current map
    List<IClient> loadedPlayers = new List<IClient>();

    List<BlockEdit> EditedBlocks = new List<BlockEdit>();

    private void Awake()
    {
        vEngine = GetComponent<VoxelEngine>();
        XMLServer = GetComponent<XmlUnityServer>();
        PlayerManager = GetComponent<ServerPlayerManager>();
        gManager = GetComponent<ServerGameManager>();

        world = vEngine.world;
    }

    private void Start()
    {
        XMLServer.Server.ClientManager.ClientConnected += PlayerConnected;
        XMLServer.Server.ClientManager.ClientDisconnected += PlayerDisconnected;
    }

    void PlayerConnected(object sender, ClientConnectedEventArgs e)
    {
        ushort stateTag;
        if(gManager.inStartTime)
        {
            stateTag = 0;
        }
        else
        {
            stateTag = 1;
        }
        PlayerManager.AddPlayer(e.Client.ID, stateTag, vEngine.currentMap.SpawnX, vEngine.currentMap.SpawnY, vEngine.currentMap.SpawnZ);
            
        /*
        
        */
        using (DarkRiftWriter mapWriter = DarkRiftWriter.Create())
        {

            mapWriter.Write(vEngine.currentMap.Name);
            /*
            mapWriter.Write(vEngine.currentMap.Width);
            mapWriter.Write(vEngine.currentMap.Length);
            mapWriter.Write(vEngine.currentMap.Height);
            mapWriter.Write(vEngine.mapBytes);
            Debug.Log(vEngine.mapBytes.Length);
            */
            using (Message mapMessage = Message.Create(MAP_TAG, mapWriter))
            {
                e.Client.SendMessage(mapMessage, SendMode.Reliable);
                //Debug.Log("Sent Map Message");
            }
        }

        e.Client.MessageReceived += ClientMessageReceived;
  
      
    }



    void PlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        ushort playerID = e.Client.ID;
        PlayerManager.RemovePlayer(playerID);
        loadedPlayers.Remove(e.Client);
        using (DarkRiftWriter RemovePlayerWriter = DarkRiftWriter.Create())
        {        
            RemovePlayerWriter.Write(playerID);

            using (Message RemovePlayerMessage = Message.Create(REMOVE_PLAYER_TAG, RemovePlayerWriter))
            {
                foreach (IClient c in XMLServer.Server.ClientManager.GetAllClients())
                {
                    c.SendMessage(RemovePlayerMessage, SendMode.Reliable);
                    //Debug.Log("Sent Player Remove Message");
                }
            }
        }
    }

    void ClientMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
           if(e.Tag == INPUT_TAG)
           {
                PlayerManager.ApplyInput(e);
           }
           else if(e.Tag == BLOCK_EDIT_TAG)
           {
                ApplyBlockEdit(e);
           }
           else if(e.Tag == MAP_LOADED_TAG)
            {
                InitializePlayer(e);
            }
           else if(e.Tag == MAP_RELOADED_TAG)
            {
                ReInitializePlayer(e);
            }
        }

    }
    
    private void InitializePlayer(MessageReceivedEventArgs e)
    {
        
        loadedPlayers.Add(e.Client);

        using (DarkRiftWriter PlayerWriter = DarkRiftWriter.Create())
        {

            PlayerWriter.Write(loadedPlayers.Count);
            
            foreach (IClient playerClient in loadedPlayers)
            {
                ushort playerID = playerClient.ID;


                Transform playerTransform = PlayerManager.PlayerDictionary[playerID];

                PlayerWriter.Write(playerID);
                PlayerWriter.Write(playerTransform.GetComponent<ServerPositionTracker>().stateTag);
                PlayerWriter.Write(playerTransform.position.x);
                PlayerWriter.Write(playerTransform.position.y);
                PlayerWriter.Write(playerTransform.position.z);

                PlayerWriter.Write(playerTransform.rotation.eulerAngles.x);
                PlayerWriter.Write(playerTransform.rotation.eulerAngles.y);
                PlayerWriter.Write(playerTransform.rotation.eulerAngles.z);

            }

            using (Message playerMessage = Message.Create(PLAYER_INIT_TAG, PlayerWriter))
            {
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
                //Debug.Log("Sent Player Init Message");
            }

        }

        using (DarkRiftWriter NewPlayerWriter = DarkRiftWriter.Create())
        {


            ushort playerID = e.Client.ID;

            Transform playerTransform = PlayerManager.PlayerDictionary[playerID];

            NewPlayerWriter.Write(playerID);
            NewPlayerWriter.Write(playerTransform.position.x);
            NewPlayerWriter.Write(playerTransform.position.y);
            NewPlayerWriter.Write(playerTransform.position.z);

            NewPlayerWriter.Write(playerTransform.rotation.eulerAngles.x);
            NewPlayerWriter.Write(playerTransform.rotation.eulerAngles.y);
            NewPlayerWriter.Write(playerTransform.rotation.eulerAngles.z);

            NewPlayerWriter.Write(playerTransform.GetComponent<ServerPositionTracker>().stateTag);

            using (Message NewPlayerMessage = Message.Create(ADD_PLAYER_TAG, NewPlayerWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    if (c.ID != e.Client.ID)
                    {
                        c.SendMessage(NewPlayerMessage, SendMode.Reliable);
                       // Debug.Log("Sent Player Add Message");
                    }
                }
            }

        }

        using (DarkRiftWriter BlockEditsWriter = DarkRiftWriter.Create())
        {

            foreach(BlockEdit bEdit in EditedBlocks)
            {
                BlockEditsWriter.Write(bEdit.x);
                BlockEditsWriter.Write(bEdit.y);
                BlockEditsWriter.Write(bEdit.z);
                BlockEditsWriter.Write(bEdit.blockTag);
            }
        

            using (Message blockEditMessage = Message.Create(BLOCK_EDIT_TAG, BlockEditsWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    c.SendMessage(blockEditMessage, SendMode.Reliable);
                    //Debug.Log("Sent Block Edit Message");
                }
            }
        }

     
    }

    private void ReInitializePlayer(MessageReceivedEventArgs e)
    {
        loadedPlayers.Add(e.Client);
        foreach(ushort id in PlayerManager.PlayerDictionary.Keys)
        {
            using (DarkRiftWriter positionWriter = DarkRiftWriter.Create())
            {
                positionWriter.Write(id);

                Vector3 playerPosition = PlayerManager.PlayerDictionary[id].position;

                positionWriter.Write(playerPosition.x);
                positionWriter.Write(playerPosition.y);
                positionWriter.Write(playerPosition.z);


                using (Message positionMessage = Message.Create(POSITION_UPDATE_TAG, positionWriter))
                {
                    e.Client.SendMessage(positionMessage, SendMode.Unreliable);
                }

            }

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

            if(world[x, y, z] != blockTag)
            {
                //check to see if block intersects players
                Collider[] thingsHit = Physics.OverlapBox(new Vector3(x + .5f, y + .5f, z + .5f), new Vector3(.45f, .45f, .45f));

                foreach(Collider col in thingsHit)
                {
                    if(col.CompareTag("Player"))
                    {
                        Debug.Log("Hit player");
                        return;
                    }
                }

                world[x, y, z] = blockTag;

                EditedBlocks.Add(new BlockEdit(x, y, z, blockTag));

                vEngine.mapBytes[z + x * vEngine.currentMap.Length + y * vEngine.currentMap.Length * vEngine.currentMap.Width] = (byte)blockTag;

                using (DarkRiftWriter blockWriter = DarkRiftWriter.Create())
                {

                    blockWriter.Write(x);
                    blockWriter.Write(y);
                    blockWriter.Write(z);
                    blockWriter.Write(blockTag);

                    using (Message blockEditMessage = Message.Create(BLOCK_EDIT_TAG, blockWriter))
                    {
                        foreach (IClient c in loadedPlayers)
                        {
                              c.SendMessage(blockEditMessage, SendMode.Reliable);
                            //Debug.Log("Sent Block Edit Message");
                        }
                    }

                }

                world.Chunks[ChunkID.FromWorldPos(x, y, z)].dirty = true;
            }

        }
    }

    public void SendPositionUpdate(ushort id, Vector3 newPosition)
    {
        using (DarkRiftWriter positionWriter = DarkRiftWriter.Create())
        {
            positionWriter.Write(id);

            positionWriter.Write(newPosition.x);
            positionWriter.Write(newPosition.y);
            positionWriter.Write(newPosition.z);
        

            using (Message positionMessage = Message.Create(POSITION_UPDATE_TAG, positionWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    c.SendMessage(positionMessage, SendMode.Unreliable);
                   // Debug.Log("Sent Position Update Message");

                }
            }

        }

    }

    public void UpdatePlayerState(ushort ID, ushort stateTag)
    {
        PlayerManager.PlayerDictionary[ID].GetComponent<ServerPositionTracker>().stateTag = stateTag;
        Color newColor;

        if(stateTag == 0)
        {
            newColor = Color.white;
        }
        else
        {
            newColor = Color.red;
        }

        PlayerManager.PlayerDictionary[ID].GetComponent<MeshRenderer>().material.color = newColor;
        using (DarkRiftWriter stateWriter = DarkRiftWriter.Create())
        {
            stateWriter.Write(ID);
            stateWriter.Write(stateTag);

            using (Message positionMessage = Message.Create(PLAYER_STATE_TAG, stateWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    c.SendMessage(positionMessage, SendMode.Reliable);
                    Debug.Log("Sent Player State Message");
                }
            }

        }

    }

    public void StartRound()
    {
        foreach (IClient c in XMLServer.Server.ClientManager.GetAllClients())
        {
            UpdatePlayerState(c.ID, 0);
        }

        Vector3 spawnPosition = new Vector3(vEngine.currentMap.SpawnX, vEngine.currentMap.SpawnY, vEngine.currentMap.SpawnZ);

        using (DarkRiftWriter mapWriter = DarkRiftWriter.Create())
        {
            /*
            mapWriter.Write(vEngine.currentMap.Width);
            mapWriter.Write(vEngine.currentMap.Length);
            mapWriter.Write(vEngine.currentMap.Height);
            mapWriter.Write(vEngine.mapBytes);
            Debug.Log(vEngine.currentMap.Name);
            */

            mapWriter.Write(vEngine.currentMap.Name);

            using (Message mapMessage = Message.Create(MAP_TAG, mapWriter))
            {
                foreach (IClient c in XMLServer.Server.ClientManager.GetAllClients())
                {
                    c.SendMessage(mapMessage, SendMode.Reliable);

                    PlayerManager.PlayerDictionary[c.ID].GetComponent<Rigidbody>().velocity = Vector3.zero;
                    PlayerManager.PlayerDictionary[c.ID].position = spawnPosition;

                    loadedPlayers.Remove(c);
                    
                }
            }  
        }
    }

    public ushort GetRandomPlayer()
    {
        int numClients = XMLServer.Server.ClientManager.GetAllClients().Length;
        int playerIndex = Random.Range(0, numClients);
        if(numClients != 0)
        {
            return XMLServer.Server.ClientManager.GetAllClients()[playerIndex].ID;
        }
        else
        {
            return 1000;
        }
       

    }
}

class BlockEdit
{
    public ushort x;
    public ushort y;
    public ushort z;

    public ushort blockTag;

    public BlockEdit(ushort editX, ushort editY, ushort editZ, ushort newTag)
    {
        x = editX;
        y = editY;
        z = editZ;

        blockTag = newTag;
    }
}
    

