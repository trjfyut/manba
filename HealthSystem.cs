using UnityEngine;
using TMPro;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI healthBarText;
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f; // 低于30%血量时变红
    
    private void Start()
    {
        currentHealth = maxHealth;
        
        if (healthBarText == null)
        {
            Debug.LogError("[HealthSystem] healthBarText 引用为空，血量条将不会显示");
        }
        else
        {
            Debug.Log("[HealthSystem] healthBarText 引用有效，初始化血量条");
        }
        
        UpdateHealthBar();
    }
    
    public void TakeDamage(int damage)
    {
        Debug.Log($"[HealthSystem] TakeDamage 被调用，当前血量: {currentHealth}, 伤害: {damage}, 调用堆栈: {new System.Diagnostics.StackTrace().ToString()}");
        
        currentHealth -= damage;
        
        // 确保血量不会低于0
        if (currentHealth < 0)
            currentHealth = 0;
            
        Debug.Log($"[HealthSystem] 减血后血量: {currentHealth}");
        
        UpdateHealthBar();
        
        // 如果血量为0，触发游戏结束
        if (currentHealth <= 0)
        {
            Debug.Log("[HealthSystem] 血量为0，游戏结束");
            GameManager.Instance.GameOver();
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth += amount;
        
        // 确保血量不会超过最大值
        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
            
        UpdateHealthBar();
    }
    
    private void UpdateHealthBar()
    {
        if (healthBarText == null)
        {
            Debug.LogError("[HealthSystem] healthBarText 为空，无法更新血量条");
            return;
        }
            
        // 计算血量百分比
        float healthPercentage = (float)currentHealth / maxHealth;
        
        // 创建血量条字符串
        string healthBar = "";
        
        // 创建填充的部分
        int filledBlocks = Mathf.RoundToInt(10 * healthPercentage);
        
        // 设置颜色
        Color barColor = Color.Lerp(lowHealthColor, fullHealthColor, Mathf.Clamp01(healthPercentage / lowHealthThreshold));
        string colorHex = ColorUtility.ToHtmlStringRGB(barColor);
        
        // 构建血量条文本 - 使用通用符号
        healthBar = $"<color=#{colorHex}>";
        
        // 添加填充的方块 - 使用 + 或 # 替代 ■
        for (int i = 0; i < filledBlocks; i++)
        {
            healthBar += "+";  // 或使用 "#"
        }
        
        // 添加空的方块 - 使用 - 或 _ 替代 □
        healthBar += "</color>";
        for (int i = 0; i < 10 - filledBlocks; i++)
        {
            healthBar += "-";  // 或使用 "_"
        }
        
        // 显示血量数值
        healthBar += $" {currentHealth}/{maxHealth}";
        
        // 更新UI
        healthBarText.text = healthBar;
        Debug.Log($"[HealthSystem] 更新血量条: {healthBar}");
    }
    
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateHealthBar();
    }
} 