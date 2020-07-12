using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Client
{
    public class ClientPositionTracker : MonoBehaviour
    {
        private World world;
        private ClientPlayerController pController;
  

        Vector3 colliderHalfExtents;
        void Awake()
        {
            world = GameObject.FindGameObjectWithTag("Network").GetComponent<ClientVoxelEngine>().world;
            pController = GetComponent<ClientPlayerController>();

            colliderHalfExtents = new Vector3(.708f / 2, .9f, .708f / 2);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Water"))
            {
                Debug.Log("In water");
                pController.moveState = 1;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Water"))
            {
                Vector3 feetPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);
                Vector3 headPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);

                if (world[Mathf.FloorToInt(feetPosition.x), Mathf.FloorToInt(feetPosition.y), Mathf.FloorToInt(feetPosition.z)] != 9 && world[Mathf.FloorToInt(headPosition.x), Mathf.FloorToInt(headPosition.y), Mathf.FloorToInt(headPosition.z)] != 9)
                {
                
                   pController.moveState = 0;
                }

            }
        }

       public ushort CheckPlayerState(ushort lastState)
        {
            Collider[] thingsHit = Physics.OverlapBox(transform.position + Vector3.down * .1f, colliderHalfExtents);

            foreach(Collider col in thingsHit)
            {
                if(col.CompareTag("Water"))
                {
                    return 1;
                }
            }

            if(lastState == 0)
            {
                return 0;
            }
            else
            {
                Vector3 feetPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);
                Vector3 headPosition = new Vector3(transform.position.x, transform.position.y - .75f, transform.position.z);

                if (world[Mathf.FloorToInt(feetPosition.x), Mathf.FloorToInt(feetPosition.y), Mathf.FloorToInt(feetPosition.z)] != 9 && world[Mathf.FloorToInt(headPosition.x), Mathf.FloorToInt(headPosition.y), Mathf.FloorToInt(headPosition.z)] != 9)
                {
                    return 0;
                  
                }

                return 1;

            }

        }
    }

}
