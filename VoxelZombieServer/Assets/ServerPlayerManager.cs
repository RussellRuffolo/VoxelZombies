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

    public float PlayerSpeed;
    public float JumpSpeed;
    public float gravAcceleration;

    public float horizontalWaterSpeed;
    public float verticalWaterSpeed;

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
       
            InputDictionary[clientID].Jump = Jump;    

            
        }
    }

    private void Update()
    {
        RunInputs();

    }

    private void RunInputs()
    {

        foreach(ushort id in PlayerDictionary.Keys)
        {
            Transform playerTransform = PlayerDictionary[id];
            Rigidbody playerRB = playerTransform.GetComponent<Rigidbody>();

            PlayerInputs inputs = InputDictionary[id];


            float yVel = playerRB.velocity.y;

            if(inputs.moveState == 0) //normal movement
            {
                bool onGround = false;
                RaycastHit[] hitData = Physics.RaycastAll(playerTransform.position, Vector3.down, 1.05f);

                foreach (RaycastHit hData in hitData)
                {
                    if (hData.collider.CompareTag("Ground"))
                    {
                        onGround = true;
                    }
                }

                if (onGround)
                {
                    if (inputs.Jump)
                    {
                        yVel = JumpSpeed;
                    }

                }
                else
                {
                    yVel -= gravAcceleration * Time.deltaTime;
                }

                playerRB.velocity = inputs.moveVector * PlayerSpeed;
                playerRB.velocity += yVel * Vector3.up;

            }
            else if(inputs.moveState == 1) //water movement
            {
                if(inputs.Jump)
                {
                    yVel = verticalWaterSpeed;
                }
                else
                {
                    yVel = -verticalWaterSpeed;
                }

                playerRB.velocity = inputs.moveVector * horizontalWaterSpeed;
                playerRB.velocity += yVel * Vector3.up;
            }

        }
        
     
    }


}

public class PlayerInputs
{
    public Vector3 moveVector;
    public bool Jump;

    //0 is normal, 1 is water, 2 is lava
    public ushort moveState;

    public PlayerInputs(Vector3 moveVec, bool jump)
    {
        moveVector = moveVec;
        Jump = jump;
        moveState = 0;
    }
}
