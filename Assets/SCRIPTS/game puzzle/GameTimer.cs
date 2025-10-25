using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float timeLimit = 240f; // 4 minutes = 240 seconds
    public TextMeshProUGUI timerText;

    [Header("Game UI Panels")]
    public GameObject winPanel;
    public GameObject losePanel;

    [Header("Scene Settings")]
    public string mainSceneName = "MainScene";  // your main city scene
    public float delayBeforeReturn = 2f;        // delay before going back

    private bool gameEnded = false;
    private bool timerRunning = false;

    void Update()
    {
        if (!timerRunning || gameEnded) return;

        timeLimit -= Time.deltaTime;
        timeLimit = Mathf.Max(0, timeLimit);

        // Convert to minutes:seconds
        int minutes = Mathf.FloorToInt(timeLimit / 60);
        int seconds = Mathf.FloorToInt(timeLimit % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";

        if (timeLimit <= 0)
        {
            LoseGame();
        }
    }

    public void StartTimer()
    {
        if (!timerRunning && !gameEnded)
        {
            timerRunning = true;
        }
    }

    public void WinGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        timerRunning = false;
        winPanel.SetActive(true);

        Debug.Log("🏆 You Win!");

        // 🔹 Save progress (mark this plot as successfully built)
        int plotIndex = PlayerPrefs.GetInt("CurrentPlotIndex", -1);
        if (plotIndex != -1)
        {
            string key = "Plot_" + (plotIndex + 1) + "_Success";
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
            Debug.Log($"✅ {key} = 1 (Plot marked as built)");
            // 💰 Give player 500 money on win
            int currentMoney = PlayerPrefs.GetInt("PlayerMoney", 0);
            currentMoney += 500;
            PlayerPrefs.SetInt("PlayerMoney", currentMoney);
            PlayerPrefs.Save();
            Debug.Log($"💵 +500 reward added! Total: {currentMoney}");
        }

        // 🔹 Return to Main Scene
        Invoke(nameof(ReturnToMainScene), delayBeforeReturn);
    }

    public void LoseGame()
    {
        if (gameEnded) return;

        gameEnded = true;
        timerRunning = false;
        losePanel.SetActive(true);

        Debug.Log("❌ You Lose!");
        Invoke(nameof(ReturnToMainScene), delayBeforeReturn);
    }

    private void ReturnToMainScene()
    {
        SceneManager.LoadScene(mainSceneName);
    }
}
