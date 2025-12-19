using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[Serializable]
public class PollData
{
    public string _id;
    public string title;
    public Wallet wallet;
    public string joinCode;
}

[Serializable]
public class Wallet
{
    public string publicKey;
}

[Serializable]
public class PollDataList
{
    public List<PollData> polls;
}

public class DataMgt : MonoBehaviour
{
    [SerializeField] private GameObject poll;
    [SerializeField] private GameObject pollsContainer;
    [SerializeField] private GameObject createVerifyPanel;
    [SerializeField] private TextMeshProUGUI data;
    void Start()
    {
        LOADPoll();
    }

    public void LOADPoll ()
    {
        foreach (Transform child in pollsContainer.transform)
        {
            Destroy(child.gameObject);
        }
        string username = PlayerPrefs.GetString("Username", "GUEST");
        string wAddress = PlayerPrefs.GetString("Wallet Address", "Not logged in");
        float highScore = PlayerPrefs.GetFloat("HighScore");
        data.text = @$"USER NAME: {username}
WALLET ADDRESS: {wAddress}
HIGH SCORE: {highScore}";
        DaddyGO.Instance.FetchUserPolls((jsonString) =>
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                Debug.LogWarning("No polls returned or an error occurred.");
                return;
            }
            PollDataList wrapper = JsonUtility.FromJson<PollDataList>("{\"polls\":" + jsonString + "}");
            List<PollData> data = wrapper.polls;
            foreach (PollData pollData in data)
            {
                GameObject pollItem = Instantiate(poll, pollsContainer.transform);
                pollItem.transform.Find("PollID").GetComponent<TextMeshProUGUI>().text = "Poll ID: " + pollData.joinCode;
                pollItem.transform.Find("W_Address").GetComponent<TextMeshProUGUI>().text = "WALLET ADDRESS: " + pollData.wallet.publicKey;
                // capture local copy for closure
                PollData captured = pollData;
                pollItem.transform.Find("COPY").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    GUIUtility.systemCopyBuffer = captured.wallet.publicKey;
                    DaddyGO.Instance.displayAlert("Poll Wallet Address Copied to Clipboard!");
                });

                pollItem.transform.Find("CLAIM").GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
                {
                    GUIUtility.systemCopyBuffer = captured.joinCode;
                    DaddyGO.Instance.displayAlert("Poll Join Code Copied to Clipboard!");
                });
            }
        });
    }
}
