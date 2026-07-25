using UnityEngine;

// 路径: Assets/Scripts/Enemy/EnemyController.cs
public class EnemyController : BaseEntity
{
    public enum EnemyState
    {
        Roaming,
        Chasing
    }

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Roaming;

    [Header("Animation Settings")]
    public Animator animator;

    [Header("Vision Settings (Half-Circle)")]
    public float viewRadius = 8f;
    [Range(0, 360)]
    public float viewAngle = 180f;

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

        // 彻底锁死物理系统的 Z 轴旋转，加上双保险
        if (rb != null) rb.freezeRotation = true;

        startPosition = rb.position;

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

    // --- 核心修改 1：移除依赖旋转的 360 度扫描，改为纯左右视觉判定 ---
    private void CheckVision()
    {
        float distanceToPlayer = Vector2.Distance(rb.position, targetPlayer.position);
        if (currentState == EnemyState.Roaming)
        {
            if (distanceToPlayer <= viewRadius)
            {
                Vector2 directionToPlayer = ((Vector2)targetPlayer.position - rb.position).normalized;

                // 获取当前角色的真实朝向（根据缩放正负值判断，而不是 Rotation）
                Vector2 currentFacing = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

                float angleToPlayer = Vector2.Angle(currentFacing, directionToPlayer);
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
            HandleFacing(direction);
        }
    }

    private void HandleChasing()
    {
        float distanceToPlayer = Vector2.Distance(rb.position, targetPlayer.position);
        Vector2 direction = ((Vector2)targetPlayer.position - rb.position).normalized;
        HandleFacing(direction);
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

    // --- 核心修改 2：彻底废除 Quaternion.Euler 旋转，只允许 Scale(缩放) 左右翻转 ---
    private void HandleFacing(Vector2 direction)
    {
        // 面向右边
        if (direction.x > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
        // 面向左边
        else if (direction.x < -0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;
        float currentSpeed = rb.linearVelocity.magnitude;
        animator.SetFloat("Speed", currentSpeed);
    }

    private void MeleeAttack()
    {
        if (animator != null)
        {
            try
            {
                animator.SetTrigger("Attack");
            }
            catch (System.Exception)
            {
                Debug.LogError($"[Animation Error] Cannot find 'Attack' trigger on '{animator.gameObject.name}'! Please check Animator setup.");
            }
        }

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

    // --- 核心修改 3：限制巡逻逻辑只在水平 X 轴随机，告别 360 度圆圈乱走 ---
    private void PickNewRoamTarget()
    {
        float randomX = Random.Range(-roamRadius, roamRadius);
        // 强制 Y 轴偏移为 0
        Vector2 randomOffset = new Vector2(randomX, 0f);
        roamTarget = startPosition + randomOffset;
        roamTimer = roamWaitTime;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // Gizmo 画线也同步改为基于缩放的直线逻辑
        Vector2 currentFacing = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        Vector3 forward = (Vector3)currentFacing * viewRadius;

        Quaternion leftRayRotation = Quaternion.Euler(0, 0, viewAngle / 2f);
        Quaternion rightRayRotation = Quaternion.Euler(0, 0, -viewAngle / 2f);
        Vector3 leftRay = leftRayRotation * forward;
        Vector3 rightRay = rightRayRotation * forward;

        Gizmos.DrawRay(transform.position, leftRay);
        Gizmos.DrawRay(transform.position, rightRay);

        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            // 将圆圈提示改为水平巡逻线提示
            Gizmos.DrawLine(startPosition + Vector2.left * roamRadius, startPosition + Vector2.right * roamRadius);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}