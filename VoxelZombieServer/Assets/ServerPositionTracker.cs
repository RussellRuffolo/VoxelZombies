using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

public class ServerPositionTracker : MonoBehaviour
{
    public float minMoveDelta;
    private Vector3 lastPosition;
    public ushort ID;

    VoxelServer vServer;

    ServerPlayerManager pManager;
    ServerGameManager gManager;

    private World world;

    Rigidbody rb;

    Vector3 colliderHalfExtents;

    private ushort lastMoveState = 0;

    private bool hasWaterJump = false;

    public List<Transform> collidingPlayers = new List<Transform>();

    private string killDataURL = "http://localhost/VoxelZombies/killData.php?";

    private void Awake()
    {
        lastPosition = transform.position;

        vServer = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelServer>();
        pManager = GameObject.FindGameObjectWithTag("Network").GetComponent<ServerPlayerManager>();
        gManager = GameObject.FindGameObjectWithTag("Network").GetComponent<ServerGameManager>();
        world = GameObject.FindGameObjectWithTag("Network").GetComponent<VoxelEngine>().world;
        rb = GetComponent<Rigidbody>();

        colliderHalfExtents = new Vector3(.708f / 2, 1.76f / 2, .708f / 2);
    }

    private void FixedUpdate()
    {    

          if (Vector3.Distance(lastPosition, transform.position) > minMoveDelta)
          {            
             vServer.SendPositionUpdate(ID, transform.position);
             lastPosition = transform.position;
          }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.CompareTag("Player"))
        {
            ServerPositionTracker otherTracker = collision.transform.GetComponent<ServerPositionTracker>();
            if (pManager.PlayerDictionary[ID].stateTag == 1)
            {
            
                if(pManager.PlayerDictionary[otherTracker.ID].stateTag == 0)
                {                
                    vServer.UpdatePlayerState(otherTracker.ID, 1);
                    vServer.SendPublicChat(vServer.playerNames[otherTracker.ID] + " was infected by " + vServer.playerNames[ID] + "!", 2);
                    gManager.CheckZombieWin();

                    StartCoroutine(PostKillData(ID, otherTracker.ID));

                }
            }     
        }     
    }

    IEnumerator PostKillData(ushort zombieID, ushort humanID)
    {
        string humanName = pManager.PlayerDictionary[humanID].name;
        string zombieName = pManager.PlayerDictionary[zombieID].name;

        WWWForm form = new WWWForm();

        form.AddField("humanName", humanName);
        form.AddField("zombieName", zombieName);

        UnityWebRequest account_post = UnityWebRequest.Post(killDataURL, form);

        yield return account_post.SendWebRequest();

        string returnText = account_post.downloadHandler.text;

        Debug.Log(returnText);
    }


    public ushort CheckPlayerState()
    {
        Vector3 feetPosition = new Vector3(transform.position.x, transform.position.y - .08f - (1.76f / 2), transform.position.z);
        Vector3 headPosition = new Vector3(transform.position.x, transform.position.y - .08f + (1.76f / 2), transform.position.z);

        if (world[Mathf.FloorToInt(feetPosition.x), Mathf.FloorToInt(feetPosition.y + .2f), Mathf.FloorToInt(feetPosition.z)] == 9)
        {
            hasWaterJump = true;
        }

        Collider[] thingsHit = Physics.OverlapBox(transform.position + Vector3.down * .08f, colliderHalfExtents);

        foreach (Collider col in thingsHit)
        {
            if (col.CompareTag("Water"))
            {
                lastMoveState = 1;                
                return 1;
            }
        }
  
        

        if (world[Mathf.FloorToInt(feetPosition.x), Mathf.FloorToInt(feetPosition.y), Mathf.FloorToInt(feetPosition.z)] != 9 && world[Mathf.FloorToInt(headPosition.x), Mathf.FloorToInt(headPosition.y), Mathf.FloorToInt(headPosition.z)] != 9)
        {
            if(lastMoveState == 1)
            {
                lastMoveState = 3;              
                return 3;
            }

            hasWaterJump = false;
            lastMoveState = 0;
            return 0;

        }

        lastMoveState = 1;
        return 1;

        

    }

    public bool CheckWaterJump()
    {
        if(hasWaterJump)
        {           
            return true;
        }

        return false;

    }

    public void UseWaterJump()
    {
        hasWaterJump = false;
    }



    public Vector3 GetCollisionVector()
    {
        Vector3 collisionVector = Vector3.zero;

        if (collidingPlayers.Count == 0)
        {
            return collisionVector;
        }

        Vector2 xzPos = new Vector2(transform.position.x, transform.position.z);
        foreach (Transform networkPlayer in collidingPlayers)
        {
            Vector2 otherXZ = new Vector2(networkPlayer.position.x, networkPlayer.position.z);
            collisionVector += 1 / Mathf.Pow((Vector2.Distance(xzPos, otherXZ)), 2) * (transform.position - networkPlayer.position).normalized;
        }

        return collisionVector;
    }
}
