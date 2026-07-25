using UnityEngine;

// 路径: Assets/Scripts/Camera/CameraController.cs
public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    private void LateUpdate()
    {
        if (target == null) return;

        // 抛弃所有的 Lerp 和平滑计算，直接把摄像机“钉”在玩家身上
        transform.position = target.position + offset;
    }
}