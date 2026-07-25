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

    [Header("Roam Settings (巡逻设置)")]
    public float roamRadius = 4f;       // 巡逻圆形的半径
    public float roamSpeed = 2f;
    public float roamWaitTime = 2f;
    private Vector2 startPosition;      // 初始出生点（圆心）
    private Vector2 roamTarget;
    private float roamTimer;

    // 防卡死相关变量
    private float stuckTimer = 0f;
    private Vector2 lastPosition;

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
        if (rb != null) rb.freezeRotation = true;
        startPosition = rb.position;
        lastPosition = rb.position;
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

    private void CheckVision()
    {
        float distanceToPlayer = Vector2.Distance(rb.position, targetPlayer.position);
        if (currentState == EnemyState.Roaming)
        {
            if (distanceToPlayer <= viewRadius)
            {
                Vector2 directionToPlayer = ((Vector2)targetPlayer.position - rb.position).normalized;
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
                startPosition = rb.position; // 丢失目标后，以当前位置为新的巡逻圆心
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
            stuckTimer = 0f;
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

            // 巡逻防卡死检测
            if (Vector2.Distance(rb.position, lastPosition) < 0.01f)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > 1.5f)
                {
                    PickNewRoamTarget();
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
            lastPosition = rb.position;
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

    // 防止怪物之间互相推搡
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.GetComponent<EnemyController>() != null)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 currentVel = rb.linearVelocity;
                float dot = Vector2.Dot(currentVel, contact.normal);
                if (dot < 0)
                {
                    rb.linearVelocity -= contact.normal * dot;
                }
            }
        }
    }

    private void HandleFacing(Vector2 direction)
    {
        if (direction.x > 0.01f)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
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

    private void PickNewRoamTarget()
    {
        // 【核心修改】使用 Random.insideUnitCircle 在以 startPosition 为中心的圆圈内随机挑一个点（支持上下左右）
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        roamTarget = startPosition + randomOffset;
        roamTimer = roamWaitTime;
        lastPosition = rb.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
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
            // 【核心修改】在 Scene 视图中以出生点为圆心，画出巡逻的圆形范围
            Gizmos.DrawWireSphere(startPosition, roamRadius);
        }

        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}