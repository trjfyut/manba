using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    private PlayerController playerController;
    
    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("[CollisionDetector] 无法找到 PlayerController 组件！");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[CollisionDetector] 触发器碰撞检测到: {other.gameObject.name}, 标签: {other.tag}");
        
        if (playerController != null)
        {
            playerController.HandleCollision(other.gameObject);
        }
    }
} 