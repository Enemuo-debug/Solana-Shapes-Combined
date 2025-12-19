using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public class JoinResponse
{
    public string message;
    public string pollId;
    public string pollTitle;
    public string walletAddress;
    public float amountPaid;
}

[Serializable]
public class VerifyTransactionRequest
{
    public string transactionVerification;
}

[Serializable]
public class Data 
{
    public string name;
    public string walletAddress;
}

[Serializable]
public class NewPollMsg
{
    public string id;
    public string title;
    public string joinCode;
    public string walletAddress;
}

[Serializable]
public class VCode
{
    public string transactionVerification;
}

[Serializable]
public class ResponseData
{
    public string token;
}

[Serializable]
public class CreatePollDto
{
    public string title;
}

[Serializable]
public class ScoreUpdate
{
    public float score;
}

[Serializable]
public class ScoreUpdateResponse
{
    public string message;
    public int updatedPolls;
    public float score;
}

public class DaddyGO : MonoBehaviour
{
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_InputField walletAddress;
    [SerializeField] private string baseUrl = "http://localhost:4000";
    [SerializeField] private GameObject alertPanel;
    public static DaddyGO Instance;
    private TMP_InputField pollTitle;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(1);
    }
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
    public void OpenSettings()
    {   
        Time.timeScale = 1f;
        SceneManager.LoadScene(2);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void CreateAccount()
    {
        username = GameObject.FindWithTag("Username").GetComponent<TMP_InputField>();
        walletAddress = GameObject.FindWithTag("Wallet").GetComponent<TMP_InputField>();
        StartCoroutine(CreateAccountCoroutine((success, message) =>
        {
            if (success)
            {
                Debug.Log("Account created successfully: " + message);
                displayAlert("Account created successfully!!");
            }
            else
            {
                Debug.LogError("Account creation failed: " + message);
                displayAlert("Account creation failed: " + message, true);
            }
        }, "/auth/register"));
    }

    public void Login()
    {
        username = GameObject.FindWithTag("Username").GetComponent<TMP_InputField>();
        walletAddress = GameObject.FindWithTag("Wallet").GetComponent<TMP_InputField>();
        StartCoroutine(LoginCoroutine((success, message) =>
        {
            if (success)
            {
                Debug.Log("Login successful: " + message);
                PlayerPrefs.SetString("Username", username.text);
                PlayerPrefs.SetString("Wallet Address", walletAddress.text);

                displayAlert("Login successful!!");
            }
            else
            {
                displayAlert("Login Failed: " + message, true);
            }
        }, "/auth/login"));
    }

    // Fetch user polls and return the raw JSON via callback when available
    public void FetchUserPolls(Action<string> onResult)
    {
        StartCoroutine(GetUserPollsCoroutine(onResult));
    }

    public void displayAlert(string message, bool isError = false)
    {
        GameObject instance = Instantiate(alertPanel, GameObject.Find("Canvas").transform);
        if (isError)
        {
            instance.GetComponent<Image>().color = new Color(73f / 255f, 4f / 255f, 4f / 255f);
        }
        instance.transform.GetChild(1).GetComponent<TMP_Text>().text = message;
        instance.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
        {
            Destroy(instance);
        });
    }

    public void CreatePollFn(Action<string, string> onResult)
    {
        pollTitle = GameObject.FindWithTag("PollTitle").GetComponent<TMP_InputField>();
        StartCoroutine(CreatePoll((string output, string output2) =>
        {
            onResult?.Invoke(output, output2);
        }));
    }

    public void JoinPoll()
    {
        pollTitle = GameObject.FindWithTag("PollTitle").GetComponent<TMP_InputField>();
        string vCode = GameObject.Find("VCode").GetComponent<TMP_InputField>().text;
        StartCoroutine(JoinAPoll(vCode));
    }

    public IEnumerator JoinYourPoll(string verificationCode, string joinCode, Action<string, bool> callback)
    {
        string token = PlayerPrefs.GetString("authToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No auth token found. Please log in first.");
            callback?.Invoke("Not authenticated", true);
            yield break;
        }

        if (string.IsNullOrEmpty(verificationCode))
        {
            Debug.LogError("Transaction verification code is required");
            callback?.Invoke("Transaction verification code is required", true);
            yield break;
        }

        if (string.IsNullOrEmpty(joinCode))
        {
            Debug.LogError("Join code is required");
            callback?.Invoke("Join code is required", true);
            yield break;
        }

        VerifyTransactionRequest request = new VerifyTransactionRequest
        {
            transactionVerification = verificationCode
        };

        string json = JsonUtility.ToJson(request);
        string URI = baseUrl + $"/polls/mine/{joinCode.ToUpper()}";
        
        Debug.Log($"Joining poll with verification: {URI}");
        Debug.Log($"Transaction: {verificationCode}");

        UnityWebRequest req = new(URI, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + token);

        yield return req.SendWebRequest();

        bool isGood = req.result == UnityWebRequest.Result.Success;
        long responseCode = req.responseCode;
        string responseBody = req.downloadHandler != null ? req.downloadHandler.text : "";

        Debug.Log($"Response Code: {responseCode}, isGood: {isGood}");
        Debug.Log($"Response Body: {responseBody}");

        if (isGood)
        {
            try
            {
                JoinResponse response = JsonUtility.FromJson<JoinResponse>(responseBody);
                string successMsg = $"Joined successfully! Amount: {response.amountPaid} SOL";
                callback?.Invoke(successMsg, false);
            }
            catch
            {
                callback?.Invoke(responseBody, false);
            }
        }
        else
        {
            string errorMsg = $"Join failed: {responseBody}";
            Debug.LogError(errorMsg);
            callback?.Invoke(errorMsg, true);
        }

        req.Dispose();
    }

    public IEnumerator CreateAccountCoroutine(Action<bool, string> callback, string URL)
    {
        Data data = new()
        {
            name = username.text,
            walletAddress = walletAddress.text
        };

        string json = JsonUtility.ToJson(data);
        string URI = baseUrl + URL;
        Debug.Log("Sending JSON: " + json + " to " + URI);

        UnityWebRequest req = new(URI, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            callback(true, "Registration successful");
        }
        else
        {
            callback(false, req.error + ": " + req.downloadHandler.text);
        }
    }

    public IEnumerator LoginCoroutine(Action<bool, string> callback, string URL)
    {
        Data data = new()
        {
            name = username.text,
            walletAddress = walletAddress.text
        };

        string json = JsonUtility.ToJson(data);
        string URI = baseUrl + URL;
        Debug.Log("Sending JSON: " + json + " to " + URI);

        UnityWebRequest req = new(URI, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            ResponseData responseData = JsonUtility.FromJson<ResponseData>(req.downloadHandler.text);
            PlayerPrefs.SetString("authToken", responseData.token);
            callback(true, "Login successful " + responseData.token);
        }
        else
        {
            callback(false, req.error + ": " + req.downloadHandler.text);
        }
    }

    public IEnumerator GetUserPollsCoroutine(Action<string> onResult)
    {
        string token = PlayerPrefs.GetString("authToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No auth token found. Please log in first.");
            onResult?.Invoke("");
            yield break;
        }

        Debug.Log("Using token: " + token);

        string URI = baseUrl + "/polls/me/list";
        UnityWebRequest req = new(URI, "GET");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", "Bearer " + token);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("User Polls: " + req.downloadHandler.text);
            onResult?.Invoke(req.downloadHandler.text);
        }
        else
        {
            Debug.LogError("Error fetching user polls: " + req.error + ": " + req.downloadHandler.text);
            onResult?.Invoke("");
        }
    }

    public IEnumerator CreatePoll(Action<string, string> callback)
    {
        string token = PlayerPrefs.GetString("authToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No auth token found. Please log in first.");
            yield break;
        }

        CreatePollDto pollDto = new()
        {
            title = pollTitle.text
        };

        string json = JsonUtility.ToJson(pollDto);

        string URI = baseUrl + "/polls";
        UnityWebRequest req = new(URI, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Poll created successfully: " + req.downloadHandler.text);
            NewPollMsg pollData = JsonUtility.FromJson<NewPollMsg>(req.downloadHandler.text);
            callback?.Invoke(pollData.joinCode, pollData.walletAddress);
        }
        else
        {
            Debug.LogError("Error creating poll: " + req.error);
        }
    }

    public IEnumerator JoinAPoll(string vCode)
    {
        string token = PlayerPrefs.GetString("authToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No auth token found. Please log in first.");
            yield break;
        }
        string URI = baseUrl + $"/polls/mine/{pollTitle.text}";

        VerifyTransactionRequest request = new() {
            transactionVerification = vCode
        };

        string jsonBody = JsonUtility.ToJson(request);

        UnityWebRequest req = new(URI, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Content-Type", "application/json");


        yield return req.SendWebRequest();

        // Log status code and response body for diagnostics
        string responseBody = req.downloadHandler != null ? req.downloadHandler.text : "";
        long responseCode = req.responseCode;

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Joined poll successfully (HTTP {responseCode}): {responseBody}");
            displayAlert($"Joined poll successfully");
            FindAnyObjectByType<DataMgt>().LOADPoll();
        }
        else
        {
            Debug.LogError($"Error joining poll (HTTP {responseCode}): {req.error} - {responseBody}");
            displayAlert("An error occured " + responseBody);
        }
    }

    public IEnumerator UpdateAllPollScores(float scoreToAdd, Action<bool, string> callback)
    {
        string token = PlayerPrefs.GetString("authToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No auth token found. Please log in first.");
            callback?.Invoke(false, "Not authenticated");
            yield break;
        }

        ScoreUpdate scoreData = new(){ score = scoreToAdd };
        string json = JsonUtility.ToJson(scoreData);

        string URI = baseUrl + "/polls/update";
        Debug.Log($"Updating scores: POST {URI} with score={scoreToAdd}");

        UnityWebRequest req = new(URI, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Scores updated successfully: " + req.downloadHandler.text);
            
            try
            {
                ScoreUpdateResponse response = JsonUtility.FromJson<ScoreUpdateResponse>(req.downloadHandler.text);
                callback?.Invoke(true, $"Updated {response.updatedPolls} poll(s)");
            }
            catch
            {
                callback?.Invoke(true, "Scores updated");
            }
        }
        else
        {
            string errorMsg = req.downloadHandler != null ? req.downloadHandler.text : req.error;
            Debug.LogError($"Error updating scores: {errorMsg}");
            callback?.Invoke(false, errorMsg);
        }

        req.Dispose();
    }

    // Optional: Update score for specific poll only
    public IEnumerator UpdatePollScore(string joinCode, float scoreToAdd, Action<bool, string> callback)
    {
        string token = PlayerPrefs.GetString("authToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("No auth token found. Please log in first.");
            callback?.Invoke(false, "Not authenticated");
            yield break;
        }

        ScoreUpdate scoreData = new ScoreUpdate { score = scoreToAdd };
        string json = JsonUtility.ToJson(scoreData);

        string URI = baseUrl + $"/polls/update/{joinCode}";
        Debug.Log($"Updating poll score: POST {URI} with score={scoreToAdd}");

        UnityWebRequest req = new UnityWebRequest(URI, "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Poll score updated: " + req.downloadHandler.text);
            callback?.Invoke(true, "Score updated successfully");
        }
        else
        {
            string errorMsg = req.downloadHandler != null ? req.downloadHandler.text : req.error;
            Debug.LogError($"Error updating poll score: {errorMsg}");
            callback?.Invoke(false, errorMsg);
        }

        req.Dispose();
    }
}
