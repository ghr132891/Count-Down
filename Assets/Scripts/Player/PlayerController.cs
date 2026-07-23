using UnityEngine;

public class PlayerController : BaseEntity
{
    private Vector2 movementInput;
    private Camera mainCamera;
    private Vector2 mousePosition;

    [Header("Melee Combat Settings")]
    public Transform attackPoint;      // 近战攻击判定的中心点 (放在角色正前方)
    public float attackRange = 1f;     // 武器的攻击半径
    public float attackDamage = 25f;   // 攻击力
    public float attackRate = 0.5f;    // 攻击间隔（秒），防止一秒按十次砍十刀
    public LayerMask enemyLayers;      // 【重点】只在这个图层里检测敌人，防止砍到空气或墙壁

    [Header("Stamina & Sprint Settings")]
    public float maxStamina = 100f;          // 最大体力值
    public float currentStamina;             // 当前体力值

    public float sprintSpeed = 8f;           // 奔跑速度 (应大于 BaseEntity 的 moveSpeed)
    public float exhaustedSpeed = 2f;        // 力竭时的缓慢移动速度

    public float staminaDrainRate = 30f;     // 奔跑时每秒消耗的体力
    public float minRecoveryRate = 5f;       // 体力见底时的最低回复速度（极慢）
    public float maxRecoveryRate = 25f;      // 体力快满时的最高回复速度（极快）
    public float recoveryThreshold = 20f;    // 力竭后，体力必须恢复到这个值才能恢复正常速度

    private bool isSprinting = false;        // 是否正在奔跑
    private bool isExhausted = false;        // 是否处于力竭状态 (体力耗尽)

    private float nextAttackTime = 0f;
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
        // 2. 处理体力逻辑 (放在 Update 里保证随时间计算的平滑度)
        HandleStamina();

        // 使用 GetMouseButtonDown(0) 代表必须每次点击才能攻击（手感更好）
        // 如果想按住左键一直自动砍，可以改回 GetMouseButton(0)
        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            MeleeAttack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    private void FixedUpdate()
    {
        // 物理相关的移动和旋转放在 FixedUpdate 中处理
        HandleMovement();
        HandleAiming();
    }

    private void HandleStamina()
    {
        // 判断玩家是否想奔跑：按住左 Shift 键 + 有移动输入 + 且没有处于力竭状态
        bool wantToSprint = Input.GetKey(KeyCode.LeftShift) && movementInput.magnitude > 0;

        if (wantToSprint && !isExhausted)
        {
            isSprinting = true;
            // 扣除体力
            currentStamina -= staminaDrainRate * Time.deltaTime;

            // 体力耗尽，进入力竭状态
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
                isSprinting = false;
                Debug.Log("体力耗尽！移动速度大幅下降！");
            }
        }
        else
        {
            isSprinting = false;

            // 恢复体力逻辑
            if (currentStamina < maxStamina)
            {
                // 【核心机制：体力越高回复越快】
                // 计算当前体力占总上限的百分比 (0 到 1 之间)
                float staminaPercentage = currentStamina / maxStamina;

                // 使用 Lerp 插值，如果百分比低，回复率就接近 minRecoveryRate，如果百分比高，就接近 maxRecoveryRate
                float currentRecoveryRate = Mathf.Lerp(minRecoveryRate, maxRecoveryRate, staminaPercentage);

                currentStamina += currentRecoveryRate * Time.deltaTime;

                // 限制体力不超过上限
                if (currentStamina > maxStamina)
                {
                    currentStamina = maxStamina;
                }
            }

            // 力竭状态的解除逻辑：体力恢复到阈值之上
            if (isExhausted && currentStamina >= recoveryThreshold)
            {
                isExhausted = false;
                Debug.Log($"体力恢复至 {recoveryThreshold}，解除力竭状态，速度恢复正常！");
            }
        }
    }

    private void HandleMovement()
    {
        // 根据当前状态决定移动速度
        float currentSpeed = moveSpeed; // 默认是 BaseEntity 里的 moveSpeed (建议设为 5)

        if (isExhausted)
        {
            currentSpeed = exhaustedSpeed; // 力竭时极慢 (2)
        }
        else if (isSprinting)
        {
            currentSpeed = sprintSpeed;    // 奔跑时极快 (8)
        }

        // 应用速度
        rb.linearVelocity = movementInput.normalized * currentSpeed;
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

    private void MeleeAttack()
    {
        if (attackPoint == null) return;

        // 核心：在 attackPoint 的位置，画一个半径为 attackRange 的圆，找出所有在 enemyLayers 上的 Collider
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // 获取敌人身上的 BaseEntity 脚本并扣血
            BaseEntity entity = enemy.GetComponent<BaseEntity>();
            if (entity != null)
            {
                entity.TakeDamage(attackDamage);
                Debug.Log($"玩家近战砍中了: {enemy.name}，造成了 {attackDamage} 点伤害！");
            }
        }
    }

    // 在编辑器里画出攻击范围，方便你肉眼调整数值
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }


    // --- 原型测试期专用：直接在屏幕上画出简易体力条 ---
    private void OnGUI()
    {
        // 样式设置
        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 20;
        textStyle.normal.textColor = Color.white;
        textStyle.fontStyle = FontStyle.Bold;

        // 绘制当前状态文字
        string stateText = isExhausted ? "<color=red>状态: 力竭 (缓慢恢复中)</color>" :
                          (isSprinting ? "<color=yellow>状态: 奔跑中...</color>" : "<color=green>状态: 正常</color>");
        GUI.Label(new Rect(20, 20, 300, 30), stateText, textStyle);

        // 绘制血条 (顺便加上，方便测试)
        GUI.Label(new Rect(20, 60, 100, 20), "HP: " + Mathf.RoundToInt(currentHealth) + "/" + maxHealth, textStyle);

        // 绘制体力槽底框 (黑色)
        GUI.backgroundColor = Color.black;
        GUI.Box(new Rect(20, 90, 200, 20), "");

        // 绘制当前体力 (如果是力竭状态，条变成红色，否则是蓝色)
        GUI.backgroundColor = isExhausted ? Color.red : Color.cyan;
        float staminaBarWidth = (currentStamina / maxStamina) * 200f; // 计算体力条宽度
        GUI.Box(new Rect(20, 90, staminaBarWidth, 20), "");

        // 绘制体力阈值标记线 (过了这条线才能解除力竭)
        if (isExhausted)
        {
            float thresholdX = 20 + (recoveryThreshold / maxStamina) * 200f;
            GUI.backgroundColor = Color.white;
            GUI.Box(new Rect(thresholdX, 85, 2, 30), "");
        }
    }
}
