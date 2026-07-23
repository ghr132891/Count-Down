using UnityEngine;

public class PlayerController : BaseEntity
{
    private Vector2 movementInput;
    private Camera mainCamera;
    private Vector2 mousePosition;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 1. 获取移动输入 (支持 WASD 或 方向键)
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        // 2. 获取鼠标在世界坐标中的位置
        mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }

    private void FixedUpdate()
    {
        // 物理相关的移动和旋转放在 FixedUpdate 中处理
        HandleMovement();
        HandleAiming();
    }

    private void HandleMovement()
    {
        // 归一化输入向量，防止斜向移动过快
        rb.linearVelocity = movementInput.normalized * moveSpeed;
    }

    private void HandleAiming()
    {
        // 计算玩家到鼠标的方向向量
        Vector2 lookDirection = mousePosition - rb.position;

        // 计算旋转角度 (Atan2 返回弧度，需转换为角度，并减去 90 度以修正精灵默认朝向)
        // 注意：这里假设你的 2D 贴图默认是朝上的 (Up)。如果是朝右的，去掉 - 90f。
        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg - 90f;

        // 应用旋转
        rb.rotation = angle;
    }
}
