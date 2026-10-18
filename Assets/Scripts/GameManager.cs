using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The one owner of the score. Coins report to it, it draws the text, and it decides when the game is won.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 20, not 10: the red coin can appear as late as score 10, and the game must not be won before the player can reach it
    [SerializeField] int targetScore = 20;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] GameObject winPanel;

    [Header("Red coin")]
    [SerializeField] Spawner spawner;

    [Header("Bonus coins")]
    [SerializeField] int bonusTotal = 50;
    [SerializeField] TMP_Text bonusText;       // hidden until the red coin is collected

    int score;
    int bonusCollected;
    int redCoinScore;          // the score at which the red coin appears, picked once per game
    bool redCoinSpawned;       // so that only one ever spawns

    void Awake()
    {
        Instance = this;
        redCoinScore = Random.Range(1, 11);       // the int version is exclusive at the top: 1 to 10
        Debug.Log($"[Red coin] will appear when the score reaches {redCoinScore}");
    }

    void Start()
    {
        bonusText.gameObject.SetActive(false);   // the bonus text is hidden until the bonus round starts
        UpdateUI();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateUI();                                  // the text changes when the score changes, not every frame

        if (!redCoinSpawned && score >= redCoinScore)
        {
            redCoinSpawned = true;
            spawner.SpawnRedCoin();
        }

        if (score >= targetScore) Win();             // the check sits here, so the moment the target is reached cannot be missed
    }

    // the red coin: start the bonus round (50 blue coins around the player), and it also counts as a coin
    public void CollectRedCoin(int value)
    {
        bonusCollected = 0;
        spawner.SpawnBonusCoins(bonusTotal);
        bonusText.gameObject.SetActive(true);    // it shows up: BonusCoins: 0/50
        UpdateBonusUI();
        AddScore(value);
    }

    // a blue coin: it counts towards the bonus, not the score
    public void CollectBonusCoin()
    {
        bonusCollected++;
        UpdateBonusUI();
    }

    void UpdateBonusUI() => bonusText.text = $"BonusCoins: {bonusCollected}/{bonusTotal}";

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
