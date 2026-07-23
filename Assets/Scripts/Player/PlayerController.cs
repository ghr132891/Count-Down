using UnityEngine;

// 路径: Assets/Scripts/Player/PlayerController.cs
public class PlayerController : BaseEntity
{
    private Vector2 movementInput;
    private Camera mainCamera;
    private Vector2 mousePosition;

    [Header("Animation Settings")]
    public Animator animator;

    [Header("Melee Combat Settings")]
    public Transform attackPoint;
    public float attackRange = 1f;
    public float attackDamage = 25f;
    public float attackRate = 0.5f;
    public LayerMask enemyLayers;

    [Header("Stamina & Sprint Settings")]
    public float maxStamina = 100f;
    public float currentStamina;
    public float sprintSpeed = 8f;
    public float exhaustedSpeed = 2f;
    public float staminaDrainRate = 30f;
    public float minRecoveryRate = 5f;
    public float maxRecoveryRate = 25f;
    public float recoveryThreshold = 20f;
    private bool isSprinting = false;
    private bool isExhausted = false;
    private float nextAttackTime = 0f;

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");

        HandleStamina();
        HandleFacing(); // [重写] 基于移动按键的翻转逻辑

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            MeleeAttack();
            nextAttackTime = Time.time + attackRate;
        }

        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;
        float moveSpeedParam = movementInput.magnitude;
        animator.SetFloat("Speed", moveSpeedParam);
        animator.SetBool("IsSprinting", isSprinting);
    }

    private void HandleStamina()
    {
        bool wantToSprint = Input.GetKey(KeyCode.LeftShift) && movementInput.magnitude > 0;
        if (wantToSprint && !isExhausted)
        {
            isSprinting = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                isExhausted = true;
                isSprinting = false;
            }
        }
        else
        {
            isSprinting = false;
            if (currentStamina < maxStamina)
            {
                float staminaPercentage = currentStamina / maxStamina;
                float currentRecoveryRate = Mathf.Lerp(minRecoveryRate, maxRecoveryRate, staminaPercentage);
                currentStamina += currentRecoveryRate * Time.deltaTime;
                if (currentStamina > maxStamina) currentStamina = maxStamina;
            }
            if (isExhausted && currentStamina >= recoveryThreshold) isExhausted = false;
        }
    }

    private void HandleMovement()
    {
        float currentSpeed = moveSpeed;
        if (isExhausted) currentSpeed = exhaustedSpeed;
        else if (isSprinting) currentSpeed = sprintSpeed;

        rb.linearVelocity = movementInput.normalized * currentSpeed;
    }

    // --- [核心修改] 完全根据水平移动按键来决定翻转 ---
    private void HandleFacing()
    {
        rb.rotation = 0f; // 锁定 Z 轴物理旋转

        // 只要按下了 A 键 (<-) 或 D 键 (->)，就直接控制 Y 轴旋转
        if (movementInput.x > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0); // 朝右
        }
        else if (movementInput.x < -0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0); // 朝左
        }
    }

    private void MeleeAttack()
    {
        if (attackPoint == null) return;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            BaseEntity entity = enemy.GetComponent<BaseEntity>();
            if (entity != null) entity.TakeDamage(attackDamage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}