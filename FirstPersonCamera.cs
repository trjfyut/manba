using UnityEngine;

public class FirstPersonCamera : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 0.5f, 0.2f);
    [SerializeField] private float smoothSpeed = 10f;
    
    private void LateUpdate()
    {
        if (playerTransform == null)
            return;
            
        // 计算目标位置（飞机内部略微偏上的位置）
        Vector3 targetPosition = playerTransform.TransformPoint(cameraOffset);
        
        // 计算目标旋转（与飞机旋转一致，但有轻微的前瞻效果）
        Quaternion targetRotation = playerTransform.rotation;
        
        // 添加一些前瞻效果，让相机稍微向前看
        Vector3 lookAheadDirection = playerTransform.forward;
        targetRotation = Quaternion.LookRotation(lookAheadDirection);
        
        // 平滑移动相机
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        
        // 平滑旋转相机
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
} 