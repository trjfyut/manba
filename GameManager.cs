using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Game Settings")]
    [SerializeField] private float initialGameSpeed = 1f;
    [SerializeField] private float maxGameSpeed = 2f;
    [SerializeField] private float gameSpeedIncreaseRate = 0.1f;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    
    [Header("References")]
    [SerializeField] private HealthSystem playerHealthSystem;
    
    private int score;
    private float distance;
    private float currentGameSpeed;
    private bool isGameOver;
    
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        StartGame();
    }
    
    private void Update()
    {
        if (!isGameOver)
        {
            // 随时间增加游戏速度
            if (currentGameSpeed < maxGameSpeed)
            {
                currentGameSpeed += gameSpeedIncreaseRate * Time.deltaTime;
                Time.timeScale = currentGameSpeed;
            }
        }
    }
    
    public void StartGame()
    {
        // 初始化游戏状态
        score = 0;
        distance = 0;
        currentGameSpeed = initialGameSpeed;
        isGameOver = false;
        Time.timeScale = currentGameSpeed;
        
        // 重置玩家血量
        if (playerHealthSystem != null)
        {
            playerHealthSystem.ResetHealth();
        }
        
        // 更新UI
        UpdateScore(0);
        UpdateDistance(0);
        UpdateHighScore();
        
        // 隐藏游戏结束面板
        gameOverPanel.SetActive(false);
    }
    
    public void GameOver()
    {
        if (isGameOver) return;
        
        isGameOver = true;
        Time.timeScale = 0;
        
        // 保存高分
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (score > highScore)
        {
            PlayerPrefs.SetInt("HighScore", score);
            PlayerPrefs.Save();
        }
        
        // 显示游戏结束面板
        finalScoreText.text = "Score: " + score + "\nDistance: " + Mathf.FloorToInt(distance) + "m";
        gameOverPanel.SetActive(true);
    }
    
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void AddScore(int points)
    {
        score += points;
        UpdateScore(score);
    }
    
    public void UpdateDistance(float newDistance)
    {
        distance = newDistance;
        distanceText.text = "Distance: " + Mathf.FloorToInt(distance) + "m";
        
        // 每100米增加分数
        if (Mathf.FloorToInt(distance) % 100 == 0 && Mathf.FloorToInt(distance) > 0)
        {
            AddScore(5);
        }
    }
    
    private void UpdateScore(int newScore)
    {
        scoreText.text = "Score: " + newScore;
    }
    
    private void UpdateHighScore()
    {
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        highScoreText.text = "High Score: " + highScore;
    }
} 