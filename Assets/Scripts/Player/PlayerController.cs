using UnityEngine;

// 路径: Assets/Scripts/Player/PlayerController.cs[cite: 1]
public class PlayerController : BaseEntity
{
    private Vector2 movementInput;
    private Camera mainCamera;
    private Vector2 mousePosition;

    [Header("Animation Settings")]
    public Animator animator;

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

    protected override void Awake()
    {
        base.Awake();
        mainCamera = Camera.main;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        RestoreFullStats();
    }

    private void Update()
    {
        // 加入死区过滤，彻底解决静止时的微小漂移
        float rawH = Input.GetAxisRaw("Horizontal");
        float rawV = Input.GetAxisRaw("Vertical");

        movementInput.x = Mathf.Abs(rawH) > 0.1f ? rawH : 0f;
        movementInput.y = Mathf.Abs(rawV) > 0.1f ? rawV : 0f;

        HandleStamina();
        HandleFacing();
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

    private void HandleFacing()
    {
        rb.rotation = 0f;
        if (movementInput.x > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        else if (movementInput.x < -0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0);
        }
    }

    public void ModifyCoreStats(float hpDelta, float maxHpDelta, float maxStaminaDelta)
    {
        maxHealth = Mathf.Max(1f, maxHealth + maxHpDelta);
        maxStamina = Mathf.Max(1f, maxStamina + maxStaminaDelta);
        currentHealth = Mathf.Clamp(currentHealth + hpDelta, 0, maxHealth);
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        Debug.Log($"MaxHp={maxHealth}, MaxStamina={maxStamina}");
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected override void Die()
    {
        if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
    }

    public void RestoreFullStats()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        isExhausted = false;
    }
}