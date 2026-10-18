using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The one owner of the score. Coins report to it, it draws the text, and it decides when the game is won.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] int targetScore = 10;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject winPanel;

    int score;

    void Awake() => Instance = this;

    void Start() => UpdateUI();

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();                                  // the text changes when the score changes, not every frame
        if (score >= targetScore) Win();             // the check sits here, so the moment the target is reached cannot be missed
    }

    void UpdateUI() => scoreText.text = $"Score: {score} / {targetScore}";

    void Win()
    {
        winPanel.SetActive(true);
        Time.timeScale = 0f;                         // stops physics, the spawner and everything that uses Time.deltaTime
    }

    // wired to the Restart button on the win panel
    public void Restart()
    {
        Time.timeScale = 1f;                         // timeScale survives a scene load, so put it back first
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
