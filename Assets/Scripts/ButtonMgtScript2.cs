using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonMgtScript2 : MonoBehaviour
{
    private Button button;
    void Start()
    {
        button = GetComponent<Button>();
        switch (gameObject.name)
        {
            case "PlayAgain":
                button.onClick.AddListener(() =>
                {
                    DaddyGO.Instance.StartGame();
                });
                break;
            case "Home":
                button.onClick.AddListener(() =>
                {
                    DaddyGO.Instance.GoToMainMenu();
                });
                break;
            case "Settings":
                button.onClick.AddListener(() =>
                {
                    DaddyGO.Instance.OpenSettings();
                });
                break;

            case "Quit":
                button.onClick.AddListener(() =>
                {
                    Debug.Log("SHUTTING DOWN...");
                    DaddyGO.Instance.QuitGame();
                }); 
                break;

            case "CreateAccount":
                button.onClick.AddListener(() =>
                {
                    DaddyGO.Instance.CreateAccount();
                }); 
                break;

            case "LOGIN":
                button.onClick.AddListener(() =>
                {
                    DaddyGO.Instance.Login();
                }); 
                break;

            case "CREATEVerify":
                GameObject createVerif = GameObject.Find("Create Verification");
                Variables variables = createVerif.GetComponent<Variables>();
                createVerif.SetActive(false);
                button.onClick.AddListener(() =>
                {
                    DaddyGO.Instance.CreatePollFn((pollId, walletAddress) =>
                    {
                        Debug.Log("Created Poll ID: " + pollId);
                        if (createVerif != null)
                        {
                            createVerif.SetActive(true);
                            createVerif.transform.Find("NEW_COPY").GetComponent<Button>().onClick.AddListener(()=>{
                                GUIUtility.systemCopyBuffer = walletAddress;
                                DaddyGO.Instance.displayAlert("Poll Wallet Address Copied to Clipboard!");
                            });
                            // set panel GameObject JoinId Variable to pollId if available
                            if (!string.IsNullOrEmpty(pollId)) variables.declarations.Set("JoinId", pollId);
                            DaddyGO.Instance.displayAlert($"Your Poll is ready, but to create, make a payment of 0.002 SOL to the address you copy...");
                        }
                    });
                });
                break;

            case "JoinYourPoll":
                button.onClick.AddListener(() => {
                    string pollId = (string) GameObject.Find("Create Verification").GetComponent<Variables>().declarations.Get("JoinId");
                    string vCode = GameObject.Find("CVCode").GetComponent<TMP_InputField>().text;
                    Debug.Log(vCode);
                    StartCoroutine(DaddyGO.Instance.JoinYourPoll(vCode, pollId, (message, isError)=>{
                        if (isError) DaddyGO.Instance.displayAlert(message, true);
                        Debug.Log("break");
                        Debug.Log(message);
                        ResponseData responseData = JsonUtility.FromJson<ResponseData>(message);
                        DaddyGO.Instance.displayAlert(responseData.token);
                        FindAnyObjectByType<DataMgt>().LOADPoll();
                    }));
                });
                break;

            case "JOINPOLL":
                button.onClick.AddListener(() =>
                {
                    DaddyGO.Instance.JoinPoll();
                    FindAnyObjectByType<DataMgt>().LOADPoll();
                });
                break;

            case "START":
                button.onClick.AddListener(() => 
                {
                    DaddyGO.Instance.StartGame();
                });
                break;
        }
    }
}
