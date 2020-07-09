using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Client
{
    public class LoginClient : MonoBehaviour
    {

        private UnityClient Client;

        public Text nameText;
        public Text usernameTakenText;

        public Canvas loginCanvas;
        public Canvas chatCanvas;
        private ClientChatManager cManager;

        // Start is called before the first frame update
        void Start()
        {
            Client = GetComponent<UnityClient>();
            cManager = GetComponent<ClientChatManager>();

            Client.MessageReceived += MessageReceived;

            usernameTakenText.enabled = false;

        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Tag == Tags.LOGIN_ATTEMPT_TAG)
            {
                using (DarkRiftReader reader = e.GetMessage().GetReader())
                {
                    bool succesful = reader.ReadBoolean();
                    if (!succesful)
                    {
                        usernameTakenText.enabled = true;

                    }
                    else
                    {
                        cManager.enabled = true;
                        chatCanvas.enabled = true;
                        Destroy(loginCanvas.gameObject); //login Canvas generated errors after being disabled
                    }
                }
            }



        }

        public void OnLogin()
        {
            if (nameText.text != "")
            {
                Debug.Log("Name is: " + nameText.text);

                using (DarkRiftWriter LoginWriter = DarkRiftWriter.Create())
                {
                    LoginWriter.Write(nameText.text);

                    using (Message loginMessage = Message.Create(Tags.LOGIN_ATTEMPT_TAG, LoginWriter))
                    {
                        Client.SendMessage(loginMessage, SendMode.Reliable);
                    }

                }
            }
            else
            {
                Debug.Log("No Name Entered");
            }
        }

    }
}

