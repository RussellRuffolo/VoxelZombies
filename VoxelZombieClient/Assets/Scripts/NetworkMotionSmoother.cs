using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkMotionSmoother : MonoBehaviour
{
    private Vector3 targetPosition;

    private void Awake()
    {
        targetPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(transform.position != targetPosition)
        {
            if(Vector3.Distance(transform.position, targetPosition) > .01)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, .4f);
           }
            else
            {
                transform.position = targetPosition;
            }                 
        }        
    }

    public void SetTargetPosition(Vector3 newTarget)
    {
        targetPosition = newTarget;
    }
}
