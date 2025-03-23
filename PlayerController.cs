using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float horizontalLimit = 15f;
    [SerializeField] private float verticalLimit = 15f;
    [SerializeField] private float verticalSpeedMultiplier = 1.5f;
    
    [Header("Rotation Settings")]
    [SerializeField] private float tiltAmount = 30f;
    [SerializeField] private float tiltSpeed = 8f;
    
    [Header("References")]
    [SerializeField] private HealthSystem healthSystem;
    
    [Header("Damage Settings")]
    [SerializeField] private float invincibilityTime = 1.0f; // 无敌时间，单位秒
    
    [Header("Audio")]
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip collectSound;
    private AudioSource audioSource;
    
    [Header("Camera Effects")]
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeDuration = 0.2f;
    private Transform cameraTransform;
    
    [Header("Collision Settings")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float rayDistance = 5f;
    
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private bool isInvincible = false;
    
    private void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        
        // 如果没有指定健康系统，尝试获取组件
        if (healthSystem == null)
        {
            healthSystem = GetComponent<HealthSystem>();
            if (healthSystem == null)
            {
                Debug.LogError("[PlayerController] 无法找到 HealthSystem 组件，将添加一个新的");
                healthSystem = gameObject.AddComponent<HealthSystem>();
            }
        }
        
        Debug.Log($"[PlayerController] HealthSystem 引用: {(healthSystem != null ? "有效" : "无效")}");
        
        // 检查碰撞体设置
        CheckColliderSetup();
        
        // 输出层级信息
        Debug.Log("飞机层级: " + gameObject.layer + " (" + LayerMask.LayerToName(gameObject.layer) + ")");
        
        // 获取或添加 AudioSource 组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // 获取相机引用
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
        
        // 添加碰撞检测器
        GameObject detector = new GameObject("CollisionDetector");
        detector.transform.parent = transform;
        detector.transform.localPosition = Vector3.zero;
        
        // 添加碰撞体和碰撞检测器脚本
        SphereCollider sphereCollider = detector.AddComponent<SphereCollider>();
        sphereCollider.radius = 2f;
        sphereCollider.isTrigger = true;
        
        detector.AddComponent<CollisionDetector>();
        
        Debug.Log("[PlayerController] 添加了碰撞检测器");
    }
    
    private void CheckColliderSetup()
    {
        Collider playerCollider = GetComponent<Collider>();
        if (playerCollider == null)
        {
            Debug.LogError("[PlayerController] 飞机没有碰撞体组件！添加一个碰撞体...");
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            
            // 确保碰撞体足够大
            boxCollider.size = new Vector3(2f, 2f, 2f); // 调整大小以确保碰撞检测
            boxCollider.center = Vector3.zero; // 确保碰撞体居中
            
            boxCollider.isTrigger = false;  // 玩家碰撞体不是触发器
            Debug.Log($"[PlayerController] 添加了碰撞体，大小: {boxCollider.size}");
        }
        else
        {
            Debug.Log($"[PlayerController] 飞机碰撞体: {playerCollider.GetType().Name}, Is Trigger: {playerCollider.isTrigger}");
            
            // 确保玩家碰撞体不是触发器
            if (playerCollider.isTrigger)
            {
                Debug.Log("[PlayerController] 将玩家碰撞体设置为非触发器");
                playerCollider.isTrigger = false;
            }
            
            // 如果是BoxCollider，确保大小合适
            if (playerCollider is BoxCollider)
            {
                BoxCollider boxCollider = (BoxCollider)playerCollider;
                if (boxCollider.size.magnitude < 1f)
                {
                    boxCollider.size = new Vector3(2f, 2f, 2f);
                    Debug.Log($"[PlayerController] 调整碰撞体大小: {boxCollider.size}");
                }
            }
        }
    }
    
    private void Update()
    {
        HandleInput();
        MovePlayer();
        RotatePlayer();
        
        // 测试血量系统
        if (Input.GetKeyDown(KeyCode.Minus) && healthSystem != null)
        {
            Debug.Log("[PlayerController] 手动减少血量");
            healthSystem.TakeDamage(1);
        }
        
        if (Input.GetKeyDown(KeyCode.Equals) && healthSystem != null)
        {
            Debug.Log("[PlayerController] 手动增加血量");
            healthSystem.Heal(1);
        }
        
        // 添加 K 键快捷键减少血量
        if (Input.GetKeyDown(KeyCode.K) && healthSystem != null)
        {
            Debug.Log("[PlayerController] 使用 K 键手动减少血量");
            healthSystem.TakeDamage(1);
            
            // 播放受伤音效
            if (damageSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(damageSound);
            }
            
            // 启动相机震动
            StartCoroutine(ShakeCamera());
            
            // 启动无敌时间和闪烁效果
            StartCoroutine(InvincibilityCoroutine());
        }
        
        // 使用射线检测障碍物
        CheckRaycastCollisions();
    }
    
    private void HandleInput()
    {
        // 获取输入
        float horizontalInput = Input.GetAxis("Horizontal"); // A和D键或左右箭头
        float verticalInput = Input.GetAxis("Vertical");     // W和S键或上下箭头
        
        // 反转垂直输入，使W对应下降，S对应上升
        verticalInput = -verticalInput;
        
        // 保存原始垂直输入用于倾斜计算
        float rawVerticalInput = verticalInput;
        
        // 应用垂直速度倍增器
        verticalInput *= verticalSpeedMultiplier;
        
        // 计算目标位置
        Vector3 movement = new Vector3(horizontalInput, verticalInput, 0) * moveSpeed * Time.deltaTime;
        targetPosition += movement;
        
        // 限制飞机在可视范围内
        targetPosition.x = Mathf.Clamp(targetPosition.x, -horizontalLimit, horizontalLimit);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -verticalLimit, verticalLimit);
        
        // 根据移动方向设置倾斜角度 - 第一人称视角下可能需要减小倾斜幅度
        float targetTiltZ = -horizontalInput * tiltAmount * 0.7f; // 减小侧倾，避免第一人称视角下过于晃动
        
        // 使用原始输入值计算前后倾斜，这样W键会让飞机向前倾斜
        float targetTiltX = -rawVerticalInput * tiltAmount * 0.8f;
        
        targetRotation = Quaternion.Euler(targetTiltX, 0, targetTiltZ);
    }
    
    private void MovePlayer()
    {
        // 平滑移动到目标位置
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }
    
    private void RotatePlayer()
    {
        // 平滑旋转到目标角度
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * tiltSpeed);
    }
    
    public void HandleCollision(GameObject other)
    {
        try
        {
            // 检测碰撞
            if (other.CompareTag("Obstacle") && !isInvincible)
            {
                Debug.Log($"[PlayerController] 检测到障碍物碰撞，准备减少血量，当前无敌状态: {isInvincible}");
                
                // 减少血量而不是直接游戏结束
                healthSystem.TakeDamage(1);
                
                // 启动无敌时间
                StartCoroutine(InvincibilityCoroutine());
                
                // 销毁障碍物
                Destroy(other);
                
                // 播放受伤音效
                if (damageSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(damageSound);
                }
                
                // 启动相机震动
                StartCoroutine(ShakeCamera());
            }
            else if (other.CompareTag("Collectible"))
            {
                GameManager.Instance.AddScore(10);
                Destroy(other);
                
                // 播放收集音效
                if (collectSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(collectSound);
                }
            }
            else if (other.CompareTag("HealthItem"))
            {
                // 恢复血量
                healthSystem.Heal(1);
                
                // 销毁道具
                Destroy(other);
            }
        }
        catch (UnityException e)
        {
            // 捕获标签不存在的异常
            Debug.LogError($"[PlayerController] 标签错误: {e.Message}. 请确保在 Unity 编辑器中定义了所有需要的标签。");
        }
    }
    
    private IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        
        // 视觉反馈 - 闪烁效果
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float flashInterval = 0.1f;
        
        for (float t = 0; t < invincibilityTime; t += flashInterval)
        {
            // 切换渲染器可见性
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = !renderer.enabled;
            }
            
            yield return new WaitForSeconds(flashInterval);
        }
        
        // 确保在无敌时间结束时渲染器是可见的
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
        
        isInvincible = false;
    }
    
    private IEnumerator ShakeCamera()
    {
        if (cameraTransform == null)
            yield break;
        
        Vector3 originalPosition = cameraTransform.localPosition;
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            
            cameraTransform.localPosition = new Vector3(x, y, originalPosition.z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        cameraTransform.localPosition = originalPosition;
    }
    
    private void CheckRaycastCollisions()
    {
        // 从玩家位置向前发射射线
        RaycastHit hit;
        
        // 绘制射线（仅在编辑器中可见）
        Debug.DrawRay(transform.position, transform.forward * rayDistance, Color.red);
        
        if (Physics.Raycast(transform.position, transform.forward, out hit, rayDistance))
        {
            Debug.Log($"[PlayerController] 射线检测到: {hit.collider.gameObject.name}, 距离: {hit.distance}");
            
            // 处理碰撞
            HandleCollision(hit.collider.gameObject);
        }
        
        // 额外的射线，覆盖更大范围
        Vector3[] directions = new Vector3[] {
            transform.forward + transform.right * 0.5f,
            transform.forward - transform.right * 0.5f,
            transform.forward + transform.up * 0.5f,
            transform.forward - transform.up * 0.5f
        };
        
        foreach (Vector3 direction in directions)
        {
            Debug.DrawRay(transform.position, direction.normalized * rayDistance, Color.yellow);
            
            if (Physics.Raycast(transform.position, direction.normalized, out hit, rayDistance))
            {
                Debug.Log($"[PlayerController] 额外射线检测到: {hit.collider.gameObject.name}, 距离: {hit.distance}");
                
                // 处理碰撞
                HandleCollision(hit.collider.gameObject);
            }
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // 只有在非无敌状态下才检测碰撞
        if (!isInvincible)
        {
            Debug.Log($"[PlayerController] 触发器持续碰撞检测到: {other.gameObject.name}, 标签: {other.tag}");
            
            try
            {
                // 检测碰撞
                if (other.CompareTag("Obstacle"))
                {
                    Debug.Log($"[PlayerController] 检测到障碍物持续碰撞，准备减少血量");
                    
                    // 减少血量而不是直接游戏结束
                    healthSystem.TakeDamage(1);
                    
                    // 启动无敌时间
                    StartCoroutine(InvincibilityCoroutine());
                    
                    // 销毁障碍物
                    Destroy(other.gameObject);
                    
                    // 播放受伤音效
                    if (damageSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(damageSound);
                    }
                    
                    // 启动相机震动
                    StartCoroutine(ShakeCamera());
                }
                else if (other.CompareTag("Collectible"))
                {
                    GameManager.Instance.AddScore(10);
                    Destroy(other.gameObject);
                    
                    // 播放收集音效
                    if (collectSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(collectSound);
                    }
                }
                else if (other.CompareTag("HealthItem"))
                {
                    // 恢复血量
                    healthSystem.Heal(1);
                    
                    // 销毁道具
                    Destroy(other.gameObject);
                }
            }
            catch (UnityException e)
            {
                // 捕获标签不存在的异常
                Debug.LogError($"[PlayerController] 标签错误: {e.Message}");
            }
        }
    }
} 