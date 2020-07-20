using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Client
{
    public class ClientChatManager : MonoBehaviour
    {
        public GameObject chatCanvas;
        public Text inputText;


        public Image logPanel;
        public Image inputPanel;

        public bool chatEnabled;

        private List<VoxelMessage> chatLog = new List<VoxelMessage>();

        public Text[] DisplayedLogs = new Text[8];

        public float chatFadeTime;

        private VoxelClient vClient;

        private ushort playerState = 0;

        private int chatsDisplayed = 0;

        void Awake()
        {
            vClient = GetComponent<VoxelClient>();

            logPanel.enabled = false;
            inputPanel.enabled = false;
            chatEnabled = false;

        }

        // Update is called once per frame
        void Update()
        {

            if (chatEnabled)
            {
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    string inputMessage = inputText.text;
                    inputText.text = "";
                    if (inputMessage != "")
                    {        
                        //send message to server here
                        vClient.SendChatMessage(inputMessage, playerState);
                    }

                    
                    UpdateDisplayedChats();
                    inputPanel.enabled = false;
                
                    chatEnabled = false;


                }
                else if(Input.GetKeyDown(KeyCode.Backspace) || Input.GetKey(KeyCode.Backspace))
                {                    
                    string toShorten = inputText.text;
                    if(toShorten.Length > 0)
                    {                   
                        inputText.text = toShorten.Substring(0, toShorten.Length - 1);
                    }                  

                }
                else
                {
                    string newInput = Input.inputString;
                    inputText.text += newInput;
                }


            }
            else
            {     

                if (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Return))
                {
                    chatEnabled = true;                  
                    inputPanel.enabled = true;

                    logPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 220);
                    logPanel.enabled = true;

                    for (int i = 0; i < DisplayedLogs.Length; i++)
                    {
                        DisplayedLogs[i].enabled = true;
                    }

                }
            }


        }

        public void DisplayMessage(string newMessage, ushort colorTag)
        {
            Color messageColor;

            switch(colorTag)
            {
                case 0: //human messages
                    messageColor = Color.white;
                    break;
                case 1: //zombie messages
                    messageColor = Color.red;
                    break;
                case 2: //system messages;
                    messageColor = Color.yellow;
                    break;
                default: //default to system
                    messageColor = Color.yellow;
                    break;

            }

            VoxelMessage message = new VoxelMessage(newMessage, messageColor);
     

            chatLog.Insert(0, message);

            for (int i = 0; i < DisplayedLogs.Length && i < chatLog.Count; i++)
            {
                DisplayedLogs[i].text = chatLog[i].text;
                DisplayedLogs[i].color = chatLog[i].color;
            }

            chatsDisplayed++;

            UpdateDisplayedChats();

            StartCoroutine(ChatDelay());
        
        }

        public void UpdateDisplayedChats()
        {
        
            logPanel.enabled = true;

            for (int i = 0; i < chatsDisplayed; i++)
            {
                DisplayedLogs[i].enabled = true;
            }
            for (int i = chatsDisplayed; i < DisplayedLogs.Length; i++)
            {
                DisplayedLogs[i].enabled = false;
            }

            logPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 20 * chatsDisplayed);
            
            
        }

        IEnumerator ChatDelay()
        {
            yield return new WaitForSeconds(chatFadeTime);
            if(chatsDisplayed > 0)
            {
                chatsDisplayed--;
                UpdateDisplayedChats();
            }
        }

        public void SetInputColor(ushort colorTag)
        {
            Color messageColor;

            switch (colorTag)
            {
                case 0: //system messages
                    messageColor = Color.yellow;
                    break;
                case 1: //human messages
                    messageColor = Color.white;
                    break;            
                default: //default to system
                    messageColor = Color.yellow;
                    break;

            }

            playerState = colorTag;

            inputText.color = messageColor;

        }
    }

    public class VoxelMessage
    {
        public string text;
        public Color color;

        public VoxelMessage(string messageText, Color messageColor)
        {
            text = messageText;
            color = messageColor;
        }
    }
   
}

