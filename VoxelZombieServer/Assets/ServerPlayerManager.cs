using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Server;
using UnityEngine;

public class ServerPlayerManager : MonoBehaviour
{
    public GameObject PlayerPrefab;

    public Dictionary<ushort, Transform> PlayerDictionary = new Dictionary<ushort, Transform>();
    public Dictionary<ushort, PlayerInputs> InputDictionary = new Dictionary<ushort, PlayerInputs>();

    public float JumpSpeed = 5.2f;

    public void AddPlayer(ushort PlayerID, ushort stateTag, int xPos, int yPos, int zPos)
    {
        Vector3 spawnPosition = new Vector3((float)xPos, (float)yPos, (float)zPos);

        GameObject newPlayer = GameObject.Instantiate(PlayerPrefab, spawnPosition, Quaternion.identity);
        newPlayer.GetComponent<ServerPositionTracker>().ID = PlayerID;
        newPlayer.GetComponent<ServerPositionTracker>().stateTag = stateTag;
        PlayerDictionary.Add(PlayerID, newPlayer.transform);
        InputDictionary.Add(PlayerID, new PlayerInputs(Vector3.zero, false));
    }

    public void RemovePlayer(ushort PlayerID)
    {
        GameObject toDestroy = PlayerDictionary[PlayerID].gameObject;
        PlayerDictionary.Remove(PlayerID);
        InputDictionary.Remove(PlayerID);
        Destroy(toDestroy);
    }

    public void ApplyInput(MessageReceivedEventArgs e)
    {
        using (DarkRiftReader reader = e.GetMessage().GetReader())
        {
            ushort clientID = e.Client.ID;
            Transform targetPlayer = PlayerDictionary[clientID];
            Rigidbody playerRB = targetPlayer.GetComponent<Rigidbody>();

            //possibly only need to send x and z, examine later.
            Vector3 moveVector = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            bool Jump = reader.ReadBoolean();

            InputDictionary[clientID].moveVector = moveVector;
            if(Jump)
            {              
            
                if (Physics.Raycast(targetPlayer.position, Vector3.down, 1.05f))
                {
                    
                    playerRB.AddForce(Vector3.up * JumpSpeed, ForceMode.Impulse);
                }
                
            }
            InputDictionary[clientID].Jump = Jump;

      

            
        }
    }


}

public class PlayerInputs
{
    public Vector3 moveVector;
    public bool Jump;

    public PlayerInputs(Vector3 moveVec, bool jump)
    {
        moveVector = moveVec;
        Jump = jump;
    }
}
