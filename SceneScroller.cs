using UnityEngine;
using System.Collections.Generic;

public class SceneScroller : MonoBehaviour
{
    [Header("Scrolling Settings")]
    [SerializeField] private float scrollSpeed = 20f;
    [SerializeField] private float accelerationRate = 0.1f;
    [SerializeField] private float maxScrollSpeed = 50f;
    
    [Header("Scene Elements")]
    [SerializeField] private Transform[] sceneSections;
    [SerializeField] private float sectionLength = 100f;
    [SerializeField] private float yOffset = -10f;      // Y轴偏移量，负值表示向下偏移
    
    private List<Transform> activeSections = new List<Transform>();
    private float currentSpeed;
    private float totalDistance;
    
    private void Start()
    {
        currentSpeed = scrollSpeed;
        
        // 初始化场景部分 - 简单的线性布局
        for (int i = 0; i < 3; i++)
        {
            SpawnSection(0, i * sectionLength);
        }
    }
    
    private void Update()
    {
        // 随时间增加速度
        if (currentSpeed < maxScrollSpeed)
        {
            currentSpeed += accelerationRate * Time.deltaTime;
        }
        
        // 移动所有活动场景部分
        for (int i = activeSections.Count - 1; i >= 0; i--)
        {
            Transform section = activeSections[i];
            section.Translate(Vector3.back * currentSpeed * Time.deltaTime);
            
            // 如果部分已经移出视野，则移除并回收
            if (section.position.z < -sectionLength)
            {
                // 回收部分
                section.gameObject.SetActive(false);
                activeSections.RemoveAt(i);
                
                // 找到最远的z位置
                float farthestZ = -sectionLength;
                foreach (Transform activeSection in activeSections)
                {
                    if (activeSection.position.z > farthestZ)
                    {
                        farthestZ = activeSection.position.z;
                    }
                }
                
                // 在前方生成新部分
                SpawnSection(0, farthestZ + sectionLength);
            }
        }
        
        // 更新总距离
        totalDistance += currentSpeed * Time.deltaTime;
        GameManager.Instance.UpdateDistance(totalDistance);
    }
    
    private void SpawnSection(float xPosition, float zPosition)
    {
        // 随机选择一个场景部分
        int randomIndex = Random.Range(0, sceneSections.Length);
        Transform sectionPrefab = sceneSections[randomIndex];
        
        // 实例化或从对象池获取
        Transform newSection = Instantiate(sectionPrefab, transform);
        newSection.position = new Vector3(xPosition, yOffset, zPosition);  // 使用Y轴偏移
        newSection.gameObject.SetActive(true);
        
        activeSections.Add(newSection);
    }
    
    public void ResetScroller()
    {
        // 重置速度和距离
        currentSpeed = scrollSpeed;
        totalDistance = 0;
        
        // 清除所有活动部分
        foreach (Transform section in activeSections)
        {
            Destroy(section.gameObject);
        }
        activeSections.Clear();
        
        // 重新初始化场景
        for (int i = 0; i < 3; i++)
        {
            SpawnSection(0, i * sectionLength);
        }
    }
} 