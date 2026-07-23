using UnityEngine;

// 路径: Assets/Scripts/Enemy/EnemyController.cs
public class EnemyController : BaseEntity
{
    public enum EnemyState
    {
        Roaming, // 游荡
        Chasing  // 追击
    }

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Roaming;

    [Header("Animation Settings")]
    public Animator animator; // 动画状态机

    [Header("Vision Settings (Half-Circle)")]
    public float viewRadius = 8f;
    [Range(0, 360)]
    public float viewAngle = 180f;       // 【修改】默认为180度，即半圆形视野

    [Header("Roam Settings")]
    public float roamRadius = 4f;
    public float roamSpeed = 2f;
    public float roamWaitTime = 2f;
    private Vector2 startPosition;
    private Vector2 roamTarget;
    private float roamTimer;

    [Header("Chase & Combat Settings")]
    public float chaseSpeed = 5f;
    public float loseTargetDistance = 12f;

    public Transform attackPoint;
    public float attackRange = 1.2f;
    public float stopDistance = 1f;
    public float attackDamage = 15f;
    public float attackRate = 1f;
    public LayerMask playerLayer;

    private float nextAttackTime = 0f;
    private Transform targetPlayer;

    protected override void Awake()
    {
        base.Awake();
        startPosition = rb.position;

        // 自动获取动画组件
        if (animator == null) animator = GetComponentInChildren<Animator>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
        }

        PickNewRoamTarget();
    }

    private void Update()
    {
        // 每帧更新动画状态
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (targetPlayer == null) return;

        CheckVision();

        switch (currentState)
        {
            case EnemyState.Roaming:
                HandleRoaming();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
        }
    }

    // --- 视野检测 ---
    private void CheckVision()
    {
        float distanceToPlayer = Vector2.Distance(rb.position, targetPlayer.position);

        if (currentState == EnemyState.Roaming)
        {
            if (distanceToPlayer <= viewRadius)
            {
                Vector2 directionToPlayer = ((Vector2)targetPlayer.position - rb.position).normalized;

                // 【核心修改】：由于只进行左右翻转，2D正前方应当是 transform.right (X轴)
                float angleToPlayer = Vector2.Angle(transform.right, directionToPlayer);

                if (angleToPlayer <= viewAngle / 2f)
                {
                    currentState = EnemyState.Chasing;
                }
            }
        }
        else if (currentState == EnemyState.Chasing)
        {
            if (distanceToPlayer > loseTargetDistance)
            {
                currentState = EnemyState.Roaming;
                startPosition = rb.position;
                PickNewRoamTarget();
            }
        }
    }

    private void HandleRoaming()
    {
        float distanceToTarget = Vector2.Distance(rb.position, roamTarget);
        if (distanceToTarget < 0.2f)
        {
            rb.linearVelocity = Vector2.zero;
            roamTimer -= Time.fixedDeltaTime;
            if (roamTimer <= 0)
            {
                PickNewRoamTarget();
            }
        }
        else
        {
            Vector2 direction = (roamTarget - rb.position).normalized;
            rb.linearVelocity = direction * roamSpeed;
            HandleFacing(direction); // 左右翻转
        }
    }

    private void HandleChasing()
    {
        float distanceToPlayer = Vector2.Distance(rb.position, targetPlayer.position);
        Vector2 direction = ((Vector2)targetPlayer.position - rb.position).normalized;

        HandleFacing(direction); // 左右翻转

        if (distanceToPlayer > stopDistance)
        {
            rb.linearVelocity = direction * chaseSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (Time.time >= nextAttackTime)
            {
                MeleeAttack();
                nextAttackTime = Time.time + attackRate;
            }
        }
    }

    // --- 【新增】抛弃 360 度旋转，改为纯粹的左右翻转 ---
    private void HandleFacing(Vector2 direction)
    {
        rb.rotation = 0f; // 彻底锁死 Z 轴物理旋转

        if (direction.x > 0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 0, 0); // 朝右
        }
        else if (direction.x < -0.01f)
        {
            transform.rotation = Quaternion.Euler(0, 180, 0); // 朝左
        }
    }

    // --- 【新增】动画系统控制 ---
    private void UpdateAnimations()
    {
        if (animator == null) return;

        // 将刚体的实际移动速度传给 Animator
        float currentSpeed = rb.linearVelocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);
    }

    private void MeleeAttack()
    {
        // 触发攻击动画
        if (animator != null) animator.SetTrigger("Attack");

        if (attackPoint == null) return;
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);
        foreach (Collider2D player in hitPlayers)
        {
            BaseEntity entity = player.GetComponent<BaseEntity>();
            if (entity != null)
            {
                entity.TakeDamage(attackDamage);
            }
        }
    }

    private void PickNewRoamTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        roamTarget = startPosition + randomOffset;
        roamTimer = roamWaitTime;
    }

    // --- 绘制视野辅助线 ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // 视野中心线改为 transform.right
        Vector3 forward = transform.right * viewRadius;

        Quaternion leftRayRotation = Quaternion.Euler(0, 0, viewAngle / 2f);
        Quaternion rightRayRotation = Quaternion.Euler(0, 0, -viewAngle / 2f);

        Vector3 leftRay = leftRayRotation * forward;
        Vector3 rightRay = rightRayRotation * forward;

        // 画出半圆的两条边缘切割线
        Gizmos.DrawRay(transform.position, leftRay);
        Gizmos.DrawRay(transform.position, rightRay);

        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(startPosition, roamRadius);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}