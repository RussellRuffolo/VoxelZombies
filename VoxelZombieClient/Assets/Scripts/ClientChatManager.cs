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

        private string inputMessage = "";

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
           
                string newInput = Input.inputString;

                foreach(char c in Input.inputString)
                {
                    //If char is backspace character remove one character.
                    if(c == '\b')
                    {
                        if(inputMessage.Length != 0)
                        {
                            inputMessage = inputMessage.Substring(0, inputMessage.Length - 1);                      
                        }
                    }
                    else if ((c == '\n') || (c == '\r')) // enter/return
                    {
                        string newMessage = inputMessage;
                        inputMessage = "";
                        inputText.text = "";
                        Debug.Log("New message is: " + newMessage);
                        if (newMessage != "")
                        {
                            //send message to server here
                            vClient.SendChatMessage(newMessage, playerState);
                        }

                        inputPanel.enabled = false;
                        chatEnabled = false;
                        UpdateDisplayedChats();
                    
                    }
                    else
                    {                        
                       inputMessage += c;              
                    
                    }
                }
                inputText.text = inputMessage;
          
                while (CalculateLengthOfMessage(inputText.text, inputText) > 300)
                {
                    inputText.text = inputText.text.Remove(0, 1);
                }
              

            }
            else
            {     

                if (Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Return))
                {
                    chatEnabled = true;                  
                    inputPanel.enabled = true;                    
                    logPanel.enabled = true;

                    for (int i = 0; i < DisplayedLogs.Length; i++)
                    {
                        DisplayedLogs[i].enabled = true;
                    }
                    UpdateDisplayedChats();

                }
            }


        }

        public void CloseChat()
        {
            inputText.text = "";
            inputMessage = "";

            inputPanel.enabled = false;
            chatEnabled = false;
            UpdateDisplayedChats();
                      
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

         
            float messageLength = CalculateLengthOfMessage(newMessage, DisplayedLogs[0]);
            
            int numLines = Mathf.FloorToInt(messageLength / 300) + 1;

            Debug.Log("Message Length: " + messageLength + " For message: " + newMessage + " Gives num lines: " + numLines);

            VoxelMessage message = new VoxelMessage(newMessage, messageColor, numLines);


            chatLog.Insert(0, message);

            int chatHeight = 0;
            for (int i = 0; i < DisplayedLogs.Length && i < chatLog.Count; i++)
            {
             
                DisplayedLogs[i].text = chatLog[i].text;                
                DisplayedLogs[i].color = chatLog[i].color;
                int messageHeight = 15 * chatLog[i].numLines;
                DisplayedLogs[i].rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, messageHeight);
                DisplayedLogs[i].rectTransform.anchoredPosition = new Vector2(DisplayedLogs[i].rectTransform.anchoredPosition.x, chatHeight);
                chatHeight += messageHeight;
            }

            chatsDisplayed++;

            UpdateDisplayedChats();

            StartCoroutine(ChatDelay());
        
        }

        float CalculateLengthOfMessage(string message, Text chatText)
        {
            float totalLength = 0;

            Font myFont = chatText.font;  
            CharacterInfo characterInfo = new CharacterInfo();

            char[] arr = message.ToCharArray();

            foreach (char c in arr)
            {
                myFont.GetCharacterInfo(c, out characterInfo, chatText.fontSize);

                totalLength += characterInfo.advance;
            }
            totalLength = totalLength * chatCanvas.GetComponent<Canvas>().scaleFactor;
            return totalLength;
        }

        string AddNewLines(string message, Text chatText)
        {
            string newMessage = "";

            int length = 0;

            Font myFont = chatText.font;  //chatText is my Text component
            CharacterInfo characterInfo = new CharacterInfo();

            char[] arr = message.ToCharArray();

            for(int i = 0; i < arr.Length; i++)
            {
                myFont.GetCharacterInfo(arr[i], out characterInfo, chatText.fontSize);

                length += characterInfo.advance;

                if(length > 300)
                {
                    length -= 300;
                    newMessage += '\n';
                }
                newMessage += arr[i];
            }

            return newMessage;
        }

        public void UpdateDisplayedChats()
        {
            if(chatEnabled == false)
            {
                int chatHeight = 0;
                logPanel.enabled = true;

                for (int i = 0; i < chatsDisplayed; i++)
                {
                    DisplayedLogs[i].enabled = true;
                    DisplayedLogs[i].rectTransform.anchoredPosition = new Vector2(DisplayedLogs[i].rectTransform.anchoredPosition.x, chatHeight);
                    chatHeight += chatLog[i].numLines * 15;
                }
                for (int i = chatsDisplayed; i < DisplayedLogs.Length; i++)
                {
                    DisplayedLogs[i].enabled = false;
                }

                logPanel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, chatHeight);
            }

            
          
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
        public int numLines;

        public VoxelMessage(string messageText, Color messageColor, int numberLines)
        {
            text = messageText;
            color = messageColor;
            numLines = numberLines;
        }
    }
   
}

