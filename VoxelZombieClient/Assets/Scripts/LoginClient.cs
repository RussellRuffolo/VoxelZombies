using System.Collections;
using System.Collections.Generic;
using DarkRift;
using DarkRift.Client;
using DarkRift.Client.Unity;
using UnityEngine.UI;
using System.Security.Cryptography;
using System;
using UnityEngine;

namespace Client
{
    public class LoginClient : MonoBehaviour
    {

        private UnityClient Client;

        public Text nameText;
        public Text passwordText;

        public Text usernameTakenText;
        public Text passwordNoMatchText;
        public Text accountCreatedText;
        public Text usernameNotExistText;

        public Canvas loginCanvas;
        public Canvas chatCanvas;
        public Canvas targeterCanvas;

        private ClientChatManager cManager;

        // Start is called before the first frame update
        void Start()
        {
            Client = GetComponent<UnityClient>();
            cManager = GetComponent<ClientChatManager>();

            Client.MessageReceived += MessageReceived;

            usernameTakenText.enabled = false;
            passwordNoMatchText.enabled = false;
            accountCreatedText.enabled = false;
            usernameNotExistText.enabled = false;

            targeterCanvas.enabled = false;

        }

        private void MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Tag == Tags.LOGIN_ATTEMPT_TAG)
            {
                using (DarkRiftReader reader = e.GetMessage().GetReader())
                {
                    ushort loginStatus = reader.ReadUInt16();
                    if (loginStatus == 0) //succesful login
                    {

                        cManager.enabled = true;
                        chatCanvas.enabled = true;
                        targeterCanvas.enabled = true;
                        Destroy(loginCanvas.gameObject); //login Canvas generated errors after being disabled                
             

                    }
                    else if(loginStatus == 1) //Username is not in database
                    {
                        DisableTexts();
                        usernameNotExistText.enabled = true;
                    }
                    else if (loginStatus == 2) //Supplied password does not match password of corresponding name in db
                    {
                        DisableTexts();
                        passwordNoMatchText.enabled = true;
                    }
                    else //error logging in
                    {
                        Debug.Log("Login status is: " + loginStatus);
                    }
                }
              
            }
            else if (e.Tag == Tags.CREATE_ACCOUNT_TAG)
            {
                using (DarkRiftReader reader = e.GetMessage().GetReader())
                {
                    bool succesful = reader.ReadBoolean();
                    if (!succesful)
                    {
                        DisableTexts();
                        usernameTakenText.enabled = true;
                       

                    }
                    else
                    {
                        DisableTexts();
                        accountCreatedText.enabled = true;
                        nameText.text = "";
                        passwordText.text = "";

                    }
                }
            }



        }

        private void DisableTexts()
        {
            usernameTakenText.enabled = false;
            passwordNoMatchText.enabled = false;
            accountCreatedText.enabled = false;
            usernameNotExistText.enabled = false;

        }

        public void OnLogin()
        {
            if (nameText.text != "" && passwordText.text != "")
            {
                string hashedPassword = HashPassword(passwordText.text);

                using (DarkRiftWriter LoginWriter = DarkRiftWriter.Create())
                {
                    LoginWriter.Write(nameText.text);
                    LoginWriter.Write(hashedPassword);

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

        public void OnCreateAccount()
        {
            if (nameText.text != "" && passwordText.text != "")
            {
                string hashedPassword = HashPassword(passwordText.text);

                using (DarkRiftWriter CreateAccountWriter = DarkRiftWriter.Create())
                {
                    CreateAccountWriter.Write(nameText.text);
                    CreateAccountWriter.Write(hashedPassword);

                    using (Message createAccountMessage = Message.Create(Tags.CREATE_ACCOUNT_TAG, CreateAccountWriter))
                    {
                        Client.SendMessage(createAccountMessage, SendMode.Reliable);
                    }

                }
            }
            else
            {
                Debug.Log("Name or password not entered");
            }
        }

        public string HashPassword(string password)
        {
            //get the password as bytes
            byte[] passwordBytes = System.Text.Encoding.UTF8.GetBytes(password);
            byte[] hashedPasswordBytes;

            using (SHA256 mySHA256 = SHA256.Create())
            {
                hashedPasswordBytes = mySHA256.ComputeHash(passwordBytes);
            }

            string hashedPassword = ByteArrayToString(hashedPasswordBytes);
            return hashedPassword;

        }

        public static string ByteArrayToString(byte[] ba)
        {
            return BitConverter.ToString(ba).Replace("-", "");
        }





    }




}

