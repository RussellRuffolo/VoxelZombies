using System.Collections;
using System.Collections.Generic;
using System;
using DarkRift;
using DarkRift.Server;
using DarkRift.Server.Unity;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography;

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
    public const ushort CREATE_ACCOUNT_TAG = 13;

    XmlUnityServer XMLServer;
    DarkRiftServer Server;
    VoxelEngine vEngine;
    ServerPlayerManager PlayerManager;
    ServerGameManager gManager;
    ServerBlockEditor bEditor;

    

    //players who have loaded the current map
    List<IClient> loadedPlayers = new List<IClient>();

    private string addAccountURL = "http://localhost/VoxelZombies/addAccount.php?";

    private string loginAttemptURL = "http://localhost/VoxelZombies/loginAttempt.php?";

    private string saltURL = "http://localhost/VoxelZombies/getSalt.php?";


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

    //When a player connects they are sent the current map to load
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
       
        if(PlayerManager.PlayerDictionary.ContainsKey(e.Client.ID))
        {
            bool wasHuman = false;
            if (PlayerManager.PlayerDictionary[e.Client.ID].stateTag == 0)
            {
                wasHuman = true;
            }

            SendPublicChat(playerNames[e.Client.ID] + " has left the game.", 2);

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

            if (wasHuman)
            {
                gManager.CheckZombieWin();
            }
            else
            {
                gManager.CheckNoZombies();
            }

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
           else if(e.Tag == CREATE_ACCOUNT_TAG)
            {
                TryCreateAccount(e);
    
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
                    case "/message":
                        if(commands.Length > 2)
                        {
                            string playerName = commands[1];
                            if(playerNames.ContainsValue(playerName))
                            {
                                ushort targetID = GetIDFromName(playerName);
                                string message = "";
                                for(int i = 2; i < commands.Length; i++)
                                {
                                    message += commands[i];
                                    message += " ";
                                }

                                SendPrivateChat("From: " + playerNames[e.Client.ID] + ": " + message, colorTag, targetID);
                                SendPrivateChat("To: " + playerNames[targetID] + ": " + message, colorTag, e.Client.ID);
                            }
                            else
                            {
                                SendPrivateChat("Player: " + playerName + " not found.", 2, e.Client.ID);
                            }
                        }
                        else
                        {
                            SendPrivateChat("Improper format, use: /message [player] [message]", 2, e.Client.ID);
                        }
                        break;
                    case "/commands":
                        SendPrivateChat("The available commands are: /vote, and /message", 2, e.Client.ID);
                        break;
                    default:
                        SendPrivateChat("The command: " + commands[0] + " does not exist. Use /commands to see available commands.", 2, e.Client.ID);
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

    public ushort GetIDFromName(string playerName)
    {
        foreach(ushort key in playerNames.Keys)
        {
            if(playerNames[key] == playerName)
            {
                return key;
            }
        }

        return 1000;
    }

    public void SendPublicChat(string chatMessage, ushort colorTag)
    {
        using (DarkRiftWriter chatWriter = DarkRiftWriter.Create())
        {

            chatWriter.Write(chatMessage);
            chatWriter.Write(colorTag);
            Debug.Log("Chat message is: " + chatMessage);
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

    private void TryCreateAccount(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            string userName = reader.ReadString();
            string password = reader.ReadString();

            StartCoroutine(PostNewAccount(userName, password, e));

  
        }
    }

    IEnumerator PostNewAccount(string userName, string password, MessageReceivedEventArgs e)
    {
        //create a salt
        byte[] salt = new byte[32]; //change this to be the length of the hash later
        RNGCryptoServiceProvider CSPRNG = new RNGCryptoServiceProvider();
        CSPRNG.GetBytes(salt);

        //get the password as bytes
        byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

        //prepend the salt to the password and hash the result
        byte[] saltedPassword = CombineByteArrays(salt, passwordBytes);
        byte[] hashValue;

        using (SHA256 mySHA256 = SHA256.Create())
        {
            hashValue = mySHA256.ComputeHash(saltedPassword);
        }
        
        string hashedPassword = ByteArrayToString(hashValue);
        string saltString = ByteArrayToString(salt);
      
      

        WWWForm form = new WWWForm();

        form.AddField("name", userName);
        form.AddField("password", saltString + hashedPassword);

        UnityWebRequest account_post = UnityWebRequest.Post(addAccountURL, form);

        yield return account_post.SendWebRequest();

        string returnText = account_post.downloadHandler.text;

        bool createdAccount;
        
        if(returnText == "Name in Database")
        {
            createdAccount = false;
            Debug.Log("Name in database");
        }
        else if(returnText == "Added Account to Database")
        {
            createdAccount = true;
            Debug.Log("created account");
        }
        else
        {
            createdAccount = false;
            Debug.LogError(returnText);
        }

        using (DarkRiftWriter createAccountWriter = DarkRiftWriter.Create())
        {
            createAccountWriter.Write(createdAccount);
            using (Message accountMessage = Message.Create(CREATE_ACCOUNT_TAG, createAccountWriter))
            {
                e.Client.SendMessage(accountMessage, SendMode.Reliable);
       
            }

        }
    }

    public static byte[] CombineByteArrays(byte[] first, byte[] second)
    {
        byte[] bytes = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
        Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
        return bytes;
    }

    public static string ByteArrayToString(byte[] ba)
    {
        return BitConverter.ToString(ba).Replace("-", "");
    }

    public static byte[] StringToByteArray(String hex)
    {
        int NumberChars = hex.Length;
        byte[] bytes = new byte[NumberChars / 2];
        for (int i = 0; i < NumberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }



    IEnumerator PostLoginAttempt(string name, string password, MessageReceivedEventArgs e)
    {
        //get salt first
        WWWForm saltForm = new WWWForm();
        saltForm.AddField("name", name);
        UnityWebRequest saltPost = UnityWebRequest.Post(saltURL, saltForm);
        yield return saltPost.SendWebRequest();

        string returnSaltText = saltPost.downloadHandler.text;

        string returnText = "";

        if(returnSaltText.Length == 128)
        {
            string saltString = returnSaltText.Substring(0, 64);
            string hashedPassword = returnSaltText.Substring(64, 64);

            byte[] salt = StringToByteArray(saltString);

            //get the password as bytes
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);

            //prepend the salt to the password and hash the result
            byte[] saltedPassword = CombineByteArrays(salt, passwordBytes);
            byte[] hashValue;

            using (SHA256 mySHA256 = SHA256.Create())
            {
                hashValue = mySHA256.ComputeHash(saltedPassword);
            }

            string newHashedPassword = ByteArrayToString(hashValue);

            if(newHashedPassword == hashedPassword)
            {
                returnText = "Login Succesful";
            }
            else
            {
                Debug.Log("New hashed password: " + newHashedPassword + " in db: " + hashedPassword);
                returnText = "Password Mismatch";
            }

        }
        else
        {
            Debug.Log("Error: " + saltPost.downloadHandler.text);
            if(saltPost.downloadHandler.text == "No Username")
            {
                returnText = "No Username";
            }
        }


        

        Debug.Log(returnText);

        ushort succesfulLogin;

        if (returnText == "Login Succesful")
        {
            playerNames.Add(e.Client.ID, name);

            InitializePlayer(e, name);

            SendPublicChat(playerNames[e.Client.ID] + " has joined the fray.", 2);

            succesfulLogin = 0;
        }
        else if (returnText == "No Username")
        {
            succesfulLogin = 1;
        }
        else if (returnText == "Password Mismatch")
        {
            succesfulLogin = 2;

        }
        else
        {
            succesfulLogin = 3;
            Debug.LogError(returnText);
        }

        //Tells the client if the login was succesful.
        //If it was not the client is prepared to send another login attempt message
        using (DarkRiftWriter loginWriter = DarkRiftWriter.Create())
        {
            loginWriter.Write(succesfulLogin);
            using (Message loginMessage = Message.Create(LOGIN_ATTEMPT_TAG, loginWriter))
            {
                e.Client.SendMessage(loginMessage, SendMode.Reliable);
            }

        }

    }

    //If login is succesful then initialize player
    //otherwise tell client unsuccesful so they can attempt again
    private void HandleLogin(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {

            string name = reader.ReadString();
            string password = reader.ReadString();

            StartCoroutine(PostLoginAttempt(name, password, e));
   

        }
    }

    //On succesful login player is initialized.
    //New player is added to PlayerManager
    //All players are told about new player
    private void InitializePlayer(MessageReceivedEventArgs e, string name)
    {  
        ushort stateTag;
        if (gManager.inStartTime)
        {
            stateTag = 0;
        }
        else
        {
            stateTag = 1;
        }
        PlayerManager.AddPlayer(e.Client.ID, stateTag, vEngine.currentMap.SpawnX, vEngine.currentMap.SpawnY, vEngine.currentMap.SpawnZ, name);

        //This message is to the new player and tells them the ID, state, position, and name of every player
        using (DarkRiftWriter PlayerWriter = DarkRiftWriter.Create())
        {

            PlayerWriter.Write(PlayerManager.PlayerDictionary.Count);
            
            foreach (ushort playerID in PlayerManager.PlayerDictionary.Keys)
            {                
                Transform playerTransform = PlayerManager.PlayerDictionary[playerID].transform;

                PlayerWriter.Write(playerID);
                PlayerWriter.Write(PlayerManager.PlayerDictionary[playerID].stateTag);
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

        //This message is sent to every player besides the new one
        //It contains all information about the new player
        using (DarkRiftWriter NewPlayerWriter = DarkRiftWriter.Create())
        {
            ushort playerID = e.Client.ID;

            Transform playerTransform = PlayerManager.PlayerDictionary[playerID].transform;

            NewPlayerWriter.Write(playerID);
            NewPlayerWriter.Write(playerTransform.position.x);
            NewPlayerWriter.Write(playerTransform.position.y);
            NewPlayerWriter.Write(playerTransform.position.z);

            NewPlayerWriter.Write(playerTransform.rotation.eulerAngles.x);
            NewPlayerWriter.Write(playerTransform.rotation.eulerAngles.y);
            NewPlayerWriter.Write(playerTransform.rotation.eulerAngles.z);

            NewPlayerWriter.Write(PlayerManager.PlayerDictionary[playerID].stateTag);

            NewPlayerWriter.Write(playerNames[playerID]);

            using (Message NewPlayerMessage = Message.Create(ADD_PLAYER_TAG, NewPlayerWriter))
            {
                foreach (IClient c in XMLServer.Server.ClientManager.GetAllClients())
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
                e.Client.SendMessage(blockEditMessage, SendMode.Reliable);
             
            }

        }

     
    }

    private void ReInitializePlayer(MessageReceivedEventArgs e)
    {

        string mapLoaded = e.GetMessage().GetReader().ReadString();

        if(mapLoaded == vEngine.currentMap.Name)
        {
            if(!loadedPlayers.Contains(e.Client))               
            {
                Debug.Log("re added player");
                loadedPlayers.Add(e.Client);
            }

            using (DarkRiftWriter BlockEditsWriter = DarkRiftWriter.Create())
            {
                foreach (BlockEdit bEdit in bEditor.EditedBlocks)
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

                    }
                }


            }
        }
        else
        {
            using (DarkRiftWriter mapWriter = DarkRiftWriter.Create())
            {
                mapWriter.Write(vEngine.currentMap.Name);

                using (Message mapMessage = Message.Create(MAP_TAG, mapWriter))
                {
                    e.Client.SendMessage(mapMessage, SendMode.Reliable);

                }

            }
        }





        /*
      Vector3 spawnPosition = new Vector3(vEngine.currentMap.SpawnX, vEngine.currentMap.SpawnY, vEngine.currentMap.SpawnZ);


      foreach (ushort id in PlayerManager.PlayerDictionary.Keys)
      {
          using (DarkRiftWriter positionWriter = DarkRiftWriter.Create())
          {
              positionWriter.Write(id);


              PlayerManager.PlayerDictionary[id].position = spawnPosition;
              Vector3 playerPosition = PlayerManager.PlayerDictionary[id].position;

              Rigidbody rb = PlayerManager.PlayerDictionary[id].GetComponent<Rigidbody>();
              rb.velocity = Vector3.zero;

              positionWriter.Write(playerPosition.x);
              positionWriter.Write(playerPosition.y);
              positionWriter.Write(playerPosition.z);


              if(id != e.Client.ID)
              {
                  using (Message positionMessage = Message.Create(OTHER_POSITION_TAG, positionWriter))
                  {
                      e.Client.SendMessage(positionMessage, SendMode.Unreliable);
                  }
              }          

          }
          

    }
    */

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
        PlayerManager.PlayerDictionary[ID].stateTag = stateTag;
        Color newColor;

        if(stateTag == 0)
        {
            newColor = Color.white;
        }
        else
        {
            newColor = Color.red;
        }

        PlayerManager.PlayerDictionary[ID].transform.GetComponent<MeshRenderer>().material.color = newColor;
        using (DarkRiftWriter stateWriter = DarkRiftWriter.Create())
        {
            stateWriter.Write(ID);
            stateWriter.Write(stateTag);

            using (Message stateMessage = Message.Create(PLAYER_STATE_TAG, stateWriter))
            {
                foreach (IClient c in loadedPlayers)
                {
                    c.SendMessage(stateMessage, SendMode.Reliable);
                    Debug.Log("Sent Player State Message");
                }

            }

        }

    }

    public void StartRound()
    {
        bEditor.EditedBlocks.Clear();

        foreach (ushort ID in PlayerManager.PlayerDictionary.Keys)
        {
            UpdatePlayerState(ID, 0);
        }

        Vector3 spawnPosition = new Vector3(vEngine.currentMap.SpawnX, vEngine.currentMap.SpawnY, vEngine.currentMap.SpawnZ);

        using (DarkRiftWriter mapWriter = DarkRiftWriter.Create())
        {      
            mapWriter.Write(vEngine.currentMap.Name);

            using (Message mapMessage = Message.Create(MAP_TAG, mapWriter))
            {
                foreach (IClient c in XMLServer.Server.ClientManager.GetAllClients())
                {

                    //TO DO FIX THIS LOGIC
                    c.SendMessage(mapMessage, SendMode.Reliable);

                    if(PlayerManager.PlayerDictionary.ContainsKey(c.ID))
                    {
                        PlayerManager.PlayerDictionary[c.ID].transform.GetComponent<Rigidbody>().velocity = Vector3.zero;
                        PlayerManager.PlayerDictionary[c.ID].transform.position = spawnPosition;

                    }
                    loadedPlayers.Remove(c);                    
                }

            }

        }
    }

    public ushort GetRandomPlayer()
    {
        int numClients = loadedPlayers.Count;
        int playerIndex = UnityEngine.Random.Range(0, numClients);
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


    

