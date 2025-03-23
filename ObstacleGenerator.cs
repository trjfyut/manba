using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

public class ObstacleGenerator : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private GameObject[] collectiblePrefabs;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float spawnDistance = 100f;
    [SerializeField] private float horizontalRange = 10f;
    [SerializeField] private float verticalRange = 5f;
    
    [Header("Movement Settings")]
    [SerializeField] private float scrollSpeed = 20f; // 与SceneScroller中的速度保持一致
    [SerializeField] private float accelerationRate = 0.1f; // 与SceneScroller中的加速率保持一致
    [SerializeField] private float maxScrollSpeed = 50f; // 与SceneScroller中的最大速度保持一致
    
    private float spawnTimer;
    private float currentInterval;
    private float currentSpeed;
    private List<GameObject> activeObjects = new List<GameObject>();
    
    private void Start()
    {
        currentInterval = spawnInterval;
        spawnTimer = 0;
        currentSpeed = scrollSpeed;
    }
    
    private void Update()
    {
        // 随时间减少生成间隔（增加难度）
        if (currentInterval > minSpawnInterval)
        {
            currentInterval -= Time.deltaTime * 0.01f;
        }
        
        // 随时间增加速度
        if (currentSpeed < maxScrollSpeed)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
        }
        
        // 生成障碍物和收集品
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentInterval)
        {
            SpawnObstacle();
            spawnTimer = 0;
        }
        
        // 移动所有活动物体
        MoveObjects();
        
        // 清理已经过去的物体
        CleanupObjects();
    }
    
    private void SpawnObstacle()
    {
        // 检查预制体数组是否为空
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0 || 
            collectiblePrefabs == null || collectiblePrefabs.Length == 0)
        {
            Debug.LogError("[ObstacleGenerator] 预制体数组为空！请在检查器中分配预制体。");
            return;
        }
        
        // 随机决定是生成障碍物还是收集品
        bool spawnCollectible = Random.value > 0.7f;
        GameObject[] prefabArray = spawnCollectible ? collectiblePrefabs : obstaclePrefabs;
        
        // 再次检查选择的数组是否为空
        if (prefabArray.Length == 0)
        {
            Debug.LogError($"[ObstacleGenerator] 选择的预制体数组 ({(spawnCollectible ? "collectiblePrefabs" : "obstaclePrefabs")}) 为空！");
            return;
        }
        
        // 随机选择一个预制体
        int randomIndex = Random.Range(0, prefabArray.Length);
        GameObject prefab = prefabArray[randomIndex];
        
        // 检查选择的预制体是否为空
        if (prefab == null)
        {
            Debug.LogError($"[ObstacleGenerator] 选择的预制体为空！索引: {randomIndex}");
            return;
        }
        
        // 随机位置
        float randomX = Random.Range(-horizontalRange, horizontalRange);
        float randomY = Random.Range(-verticalRange, verticalRange);
        Vector3 spawnPosition = new Vector3(randomX, randomY, spawnDistance);
        
        // 随机旋转 - 只绕Z轴旋转
        Quaternion spawnRotation = Quaternion.identity;
        if (!spawnCollectible && Random.value > 0.5f) // 只对障碍物应用旋转，不对收集品
        {
            // 随机选择Z轴旋转角度 - 可以是90度的倍数或任意角度
            float rotationAngle = Random.Range(0, 4) * 90f; // 0, 90, 180, 270度
            // 或者使用完全随机的角度: float rotationAngle = Random.Range(0f, 360f);
            
            spawnRotation = Quaternion.Euler(0, 0, rotationAngle);
        }
        
        // 实例化物体，应用旋转
        GameObject newObject = Instantiate(prefab, spawnPosition, spawnRotation, transform);
        
        // 设置标签
        string desiredTag = spawnCollectible ? "Collectible" : "Obstacle";
        
        // 使用安全的方式设置标签
        try
        {
            newObject.tag = desiredTag;
            Debug.Log($"[ObstacleGenerator] 设置物体标签为: {desiredTag}");
        }
        catch (UnityException e)
        {
            Debug.LogError($"[ObstacleGenerator] 无法设置标签 '{desiredTag}': {e.Message}. 请在 Edit > Project Settings > Tags and Layers 中添加此标签。");
        }
        
        // 确保有碰撞体
        Collider objCollider = newObject.GetComponent<Collider>();
        if (objCollider == null)
        {
            // 如果没有碰撞体，添加一个
            BoxCollider boxCollider = newObject.AddComponent<BoxCollider>();
            
            // 确保碰撞体足够大
            boxCollider.size = new Vector3(3f, 3f, 3f); // 调整大小以确保碰撞检测
            boxCollider.center = Vector3.zero; // 确保碰撞体居中
            
            boxCollider.isTrigger = true;  // 设置为触发器
            Debug.LogWarning($"[ObstacleGenerator] 为物体添加了触发器碰撞体: {newObject.name}, 大小: {boxCollider.size}");
        }
        else if (!objCollider.isTrigger)
        {
            // 确保碰撞体是触发器
            objCollider.isTrigger = true;
            Debug.Log($"[ObstacleGenerator] 将物体的碰撞体设置为触发器: {newObject.name}");
        }
        
        // 输出层级信息
        Debug.Log($"[ObstacleGenerator] 障碍物层级: {newObject.layer} ({LayerMask.LayerToName(newObject.layer)})");
        
        // 添加到活动物体列表
        activeObjects.Add(newObject);
    }
    
    private void MoveObjects()
    {
        // 移动所有活动物体
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
            {
                obj.transform.Translate(Vector3.back * currentSpeed * Time.deltaTime);
            }
        }
    }
    
    private void CleanupObjects()
    {
        // 移除已经过去的物体
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeObjects[i];
            if (obj == null || obj.transform.position.z < -10)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
                activeObjects.RemoveAt(i);
            }
        }
    }
    
    public void ClearAllObjects()
    {
        // 清除所有活动物体
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        activeObjects.Clear();
        
        // 重置计时器和间隔
        spawnTimer = 0;
        currentInterval = spawnInterval;
        currentSpeed = scrollSpeed;
    }
} 