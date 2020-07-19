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
    const ushort OTHER_POSITION_TAG = 5;
    const ushort PLAYER_STATE_TAG = 6;
    const ushort REMOVE_PLAYER_TAG = 7;
    const ushort MAP_LOADED_TAG = 8;
    const ushort MAP_RELOADED_TAG = 9;
    public const ushort LOGIN_ATTEMPT_TAG = 10;
    public const ushort CHAT_TAG = 11;
    public const ushort CLIENT_POSITION_TAG = 12;

    XmlUnityServer XMLServer;
    DarkRiftServer Server;
    VoxelEngine vEngine;
    ServerPlayerManager PlayerManager;
    ServerGameManager gManager;
    ServerBlockEditor bEditor;

    

    //players who have loaded the current map
    List<IClient> loadedPlayers = new List<IClient>();

    

    public Dictionary<ushort, string> playerNames = new Dictionary<ushort, string>();

    private void Awake()
    {
        vEngine = GetComponent<VoxelEngine>();
        XMLServer = GetComponent<XmlUnityServer>();
        PlayerManager = GetComponent<ServerPlayerManager>();
        gManager = GetComponent<ServerGameManager>();
        bEditor = GetComponent<ServerBlockEditor>();

       
    }

    private void Start()
    {
        XMLServer.Server.ClientManager.ClientConnected += PlayerConnected;
        XMLServer.Server.ClientManager.ClientDisconnected += PlayerDisconnected;
    }

    void PlayerConnected(object sender, ClientConnectedEventArgs e)
    {        
        using (DarkRiftWriter mapWriter = DarkRiftWriter.Create())
        {
            mapWriter.Write(vEngine.currentMap.Name);

            using (Message mapMessage = Message.Create(MAP_TAG, mapWriter))
            {
                e.Client.SendMessage(mapMessage, SendMode.Reliable);              
           }
        }

        e.Client.MessageReceived += ClientMessageReceived;
  
      
    }

    void PlayerDisconnected(object sender, ClientDisconnectedEventArgs e)
    {
        bool wasHuman = false;
        if(PlayerManager.PlayerDictionary[e.Client.ID].GetComponent<ServerPositionTracker>().stateTag == 0)
        {
            wasHuman = true;
        }

        SendPublicChat(playerNames[e.Client.ID] + " has left the game.", 2);
        if(PlayerManager.PlayerDictionary.ContainsKey(e.Client.ID))
        {
            ushort playerID = e.Client.ID;
            PlayerManager.RemovePlayer(playerID);
            loadedPlayers.Remove(e.Client);
            playerNames.Remove(playerID);
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

        if(wasHuman)
        {
            gManager.CheckZombieWin();
        }
        else
        {
            gManager.CheckNoZombies();
        }


       
    }

    void ClientMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        using (Message message = e.GetMessage() as Message)
        {
           if(e.Tag == INPUT_TAG)
           {
                PlayerManager.ReceiveInputs(e);
           }
           else if(e.Tag == BLOCK_EDIT_TAG)
           {
                ApplyBlockEdit(e);
           }
           else if(e.Tag == MAP_LOADED_TAG)
            {
              //  InitializePlayer(e);
            }
           else if(e.Tag == MAP_RELOADED_TAG)
            {
                ReInitializePlayer(e);
            }
           else if(e.Tag == LOGIN_ATTEMPT_TAG)
            {
                HandleLogin(e);
            }
           else if(e.Tag == CHAT_TAG)
            {
                HandlePlayerChat(e);
            }
        }

    }

    char[] chatParams = { ' ' };

    private void HandlePlayerChat(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            string chatMessage = reader.ReadString();
            ushort colorTag = reader.ReadUInt16();

            if(chatMessage[0] == '/')
            {                
                string[] commands = chatMessage.Split(chatParams, System.StringSplitOptions.RemoveEmptyEntries);
           
       
             
                switch(commands[0])
                {
                    case "/vote":
                        if(commands.Length > 1)
                        {
                            string mapName = commands[1];
                            if(gManager.inVoteTime)
                            {
                                if(gManager.AddVote(mapName))
                                {
                                    SendPrivateChat("Your vote for " + mapName + " has been recorded. Thanks for voting!", 2, e.Client.ID);
                                }
                                else
                                {
                                    SendPrivateChat(mapName + " does not match a map candidate." , 2, e.Client.ID);
                                }
                            }
                            else
                            {
                                SendPrivateChat("Map voting is closed until the end of the round.", 2, e.Client.ID);
                            }
                        }
                        break;
                    default:
                        SendPrivateChat("The command: " + commands[0] + " does not exist", 2, e.Client.ID);
                        break;
                }
                
            }
            else
            {
                string namedChatMessage = playerNames[e.Client.ID] + ": " + chatMessage;

                SendPublicChat(namedChatMessage, colorTag);
            }

        
        }
    }

    public void SendPublicChat(string chatMessage, ushort colorTag)
    {
        using (DarkRiftWriter chatWriter = DarkRiftWriter.Create())
        {

            chatWriter.Write(chatMessage);
            chatWriter.Write(colorTag);

            using (Message newChatMessage = Message.Create(CHAT_TAG, chatWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    c.SendMessage(newChatMessage, SendMode.Reliable);
                }
            }
        }
    }

    public void SendPrivateChat(string chatMessage, ushort colorTag, ushort recipientID)
    {
        using (DarkRiftWriter chatWriter = DarkRiftWriter.Create())
        {

            chatWriter.Write(chatMessage);
            chatWriter.Write(colorTag);

            using (Message newChatMessage = Message.Create(CHAT_TAG, chatWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    if(c.ID == recipientID)
                      c.SendMessage(newChatMessage, SendMode.Reliable);
                }
            }
        }
    }

    private void HandleLogin(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            bool succesfulLogin;

            string newPlayerName = reader.ReadString();

            if(playerNames.ContainsValue(newPlayerName))
            {
                succesfulLogin = false;
            }
            else
            {
                playerNames.Add(e.Client.ID, newPlayerName);
                succesfulLogin = true;

                InitializePlayer(e);

                SendPublicChat(playerNames[e.Client.ID] + " has joined the fray.", 2);
            }
            
            using (DarkRiftWriter loginWriter = DarkRiftWriter.Create())
            {
                loginWriter.Write(succesfulLogin);
                using (Message loginMessage = Message.Create(LOGIN_ATTEMPT_TAG, loginWriter))
                {
                    e.Client.SendMessage(loginMessage, SendMode.Reliable);
                }

            }

            

        }
    }

    private void InitializePlayer(MessageReceivedEventArgs e)
    {   
       
        loadedPlayers.Add(e.Client);

        ushort stateTag;
        if (gManager.inStartTime)
        {
            stateTag = 0;
        }
        else
        {
            stateTag = 1;
        }
        PlayerManager.AddPlayer(e.Client.ID, stateTag, vEngine.currentMap.SpawnX, vEngine.currentMap.SpawnY, vEngine.currentMap.SpawnZ);

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

                PlayerWriter.Write(playerNames[playerID]);

            }

            using (Message playerMessage = Message.Create(PLAYER_INIT_TAG, PlayerWriter))
            {
                e.Client.SendMessage(playerMessage, SendMode.Reliable);
              
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

            NewPlayerWriter.Write(playerNames[playerID]);

            using (Message NewPlayerMessage = Message.Create(ADD_PLAYER_TAG, NewPlayerWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    if (c.ID != e.Client.ID)
                    {
                        c.SendMessage(NewPlayerMessage, SendMode.Reliable);
                     
                    }
                }
            }

        }

        using (DarkRiftWriter BlockEditsWriter = DarkRiftWriter.Create())
        {
            foreach(BlockEdit bEdit in bEditor.EditedBlocks)
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


                using (Message positionMessage = Message.Create(OTHER_POSITION_TAG, positionWriter))
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
            //world position of the block to be edited
            ushort x = reader.ReadUInt16();
            ushort y = reader.ReadUInt16();
            ushort z = reader.ReadUInt16();

            //the new blockTag the client requested
            ushort blockTag = reader.ReadUInt16();

            if(bEditor.TryApplyEdit(x, y, z, blockTag))
            {
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

            using (Message positionMessage = Message.Create(OTHER_POSITION_TAG, positionWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    if (c.ID != id)
                    {
                        c.SendMessage(positionMessage, SendMode.Unreliable);
                    }

                }
            }

        }
    }

    public void SendPositionUpdate(ushort id, Vector3 newPosition, int ClientTickNumber, Vector3 velocity)
    { 
        using (DarkRiftWriter positionWriter = DarkRiftWriter.Create())
        {
            positionWriter.Write(id);

            positionWriter.Write(newPosition.x);
            positionWriter.Write(newPosition.y);
            positionWriter.Write(newPosition.z);

            positionWriter.Write(ClientTickNumber);               

            positionWriter.Write(velocity.x);
            positionWriter.Write(velocity.y);
            positionWriter.Write(velocity.z);

            using (Message positionMessage = Message.Create(CLIENT_POSITION_TAG, positionWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    if(c.ID == id)
                    {
                        c.SendMessage(positionMessage, SendMode.Unreliable);
                    }                 
           

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
        bEditor.EditedBlocks.Clear();

        foreach (IClient c in XMLServer.Server.ClientManager.GetAllClients())
        {
            UpdatePlayerState(c.ID, 0);
        }

        Vector3 spawnPosition = new Vector3(vEngine.currentMap.SpawnX, vEngine.currentMap.SpawnY, vEngine.currentMap.SpawnZ);

        using (DarkRiftWriter mapWriter = DarkRiftWriter.Create())
        {      
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
        int numClients = loadedPlayers.Count;
        int playerIndex = Random.Range(0, numClients);
        if(numClients != 0)
        {
            return loadedPlayers[playerIndex].ID;
        }
        else
        {
            return 1000;
        }
       

    }
}


    

