using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NameTagManager : MonoBehaviour
{

    public Text nameText;
    Transform localPlayer = null;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(localPlayer != null)
            nameText.transform.LookAt(localPlayer);
    }

    public void SetName(string name)
    {
        nameText.text = name;
    }

    public void SetPlayerTransform(Transform LocalPlayer)
    {

    }
}
