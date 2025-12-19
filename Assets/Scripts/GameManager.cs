using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> EnemyForms;
    [SerializeField] private GameObject gameOverPanel;

    [SerializeField] private float speed = 2.5f;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI missText;
    private uint missCount = 5;
    private float score = 0;
    private bool isGameOver = false;

    public static List<int> spawnPositions = new() { -1, 0, 1 };
    private float minSpawnTime = 1.5f;
    private float maxSpawnTime = 2.3f;
    private float startSpawnDelay = 1.5f;

    void Start()
    {
        gameOverPanel.SetActive(false);
        Invoke(nameof(SpawnEnemy), startSpawnDelay);
        Invoke(nameof(IncreaseDifficulty), 8f);

        // Initialize High Score player prefs if not present
        if (!PlayerPrefs.HasKey("HighScore"))
        {
            PlayerPrefs.SetFloat("HighScore", 0);
        }
    }

    void Update()
    {
        if (isGameOver) return;

        scoreText = scoreText != null ? scoreText : GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>();
        if (scoreText != null)
        {
            score += Time.deltaTime;
            scoreText.text = "Score: " + (int)score;
        }
        missText = missText != null ? missText : GameObject.Find("MissText").GetComponent<TextMeshProUGUI>();
        if (missText != null)
        {
            missText.text = "Miss Count: " + missCount;
        }

        // Check for game over condition
        if (missCount <= 0)
        {
            GameOver();
        }
    }

    void SpawnEnemy()
    {
        if (isGameOver) return;

        int lane = spawnPositions[Random.Range(0, spawnPositions.Count)];
        int index = Random.Range(0, EnemyForms.Count);
        float zPos = Random.Range(40f, 60f);

        GameObject enemy = Instantiate(
            EnemyForms[index],
            new Vector3(0, lane, zPos),
            Quaternion.identity
        );

        ReColorShape(enemy);
        enemy.GetComponent<Rigidbody>().velocity = new Vector3(0, 0, -speed);
        Invoke(nameof(SpawnEnemy), Random.Range(minSpawnTime, maxSpawnTime));
    }

    public void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        Time.timeScale = 0;
        CancelInvoke();

        // Get final score
        int finalScore = (int)score;

        // Check and update high score
        float currentHighScore = PlayerPrefs.GetFloat("HighScore", 0);
        bool isNewHighScore = finalScore > currentHighScore;

        if (isNewHighScore)
        {
            PlayerPrefs.SetFloat("HighScore", finalScore);
            PlayerPrefs.Save();
        }

        // Update game over panel
        TextMeshProUGUI statsText = gameOverPanel.transform.Find("STATS").GetComponent<TextMeshProUGUI>();
        statsText.text = $@"SCORE: {finalScore}
HIGH SCORE: {(int)Mathf.Max(finalScore, currentHighScore)}
{(isNewHighScore ? "NEW HIGH SCORE!" : "")}

Syncing with server...";

        gameOverPanel.SetActive(true);

        // Start coroutine to update backend
        StartCoroutine(UpdateBackendScore(finalScore, statsText));
    }

    IEnumerator UpdateBackendScore(int finalScore, TextMeshProUGUI statsText)
    {
        string token = PlayerPrefs.GetString("authToken", "");
        if (string.IsNullOrEmpty(token))
        {
            statsText.text += "\n\nNot logged in - Score not synced";
            yield break;
        }

        if (DaddyGO.Instance != null)
        {
            bool updateSuccess = false;
            string errorMessage = "";

            yield return DaddyGO.Instance.UpdateAllPollScores(finalScore, (success, message) =>
            {
                updateSuccess = success;
                errorMessage = message;
            });

            float highScore = PlayerPrefs.GetFloat("HighScore", 0);
            bool isNewHighScore = finalScore > highScore;

            if (updateSuccess)
            {
                statsText.text = $@"SCORE: {finalScore}
HIGH SCORE: {(int)highScore}
{(isNewHighScore ? "NEW HIGH SCORE!" : "")}
✓ Synced with server!";
            }
            else
            {
                statsText.text = $@"SCORE: {finalScore}
HIGH SCORE: {(int)highScore}
{(isNewHighScore ? "NEW HIGH SCORE!" : "")}
✗ Sync failed: {errorMessage}";
            }
        }
        else
        {
            statsText.text += "\n\n✗ DaddyGO instance not found";
        }
    }

    void IncreaseDifficulty()
    {
        if (isGameOver) return;

        speed += 0.3f;

        minSpawnTime = Mathf.Max(1f, minSpawnTime - 0.05f);
        maxSpawnTime = Mathf.Max(1.5f, maxSpawnTime - 0.05f);

        Invoke(nameof(IncreaseDifficulty), 8f);
    }

    public static Color ColorFromYIndex(int index)
    {
        return index switch
        {
            -1 => Color.blue,
             0 => Color.red,
             1 => Color.green,
            _ => Color.white
        };
    }

    void ReColorShape(GameObject enemy)
    {
        Color newColor = ColorFromYIndex((int)enemy.transform.position.y);

        foreach (Transform child in enemy.GetComponentsInChildren<Transform>())
        {
            Renderer r = child.GetComponent<Renderer>();
            if (r != null)
                r.material.color = newColor;
        }
    }

    public bool Match(GameObject playerShape, GameObject enemyShape)
    {
        if (playerShape == null || enemyShape == null)
            return false;

        return enemyShape.name.Contains(playerShape.name) && 
               enemyShape.transform.position.y == playerShape.transform.position.y;
    }

    public void RegisterMiss()
    {
        if (isGameOver) return;
        
        missCount--;
        
        if (missCount <= 0)
        {
            GameOver();
        }
    }

    public uint GetMissCount()
    {
        return missCount;
    }

    public float GetScore()
    {
        return score;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }
}