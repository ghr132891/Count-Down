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

    [Tooltip("在此勾选可以遮挡怪物视线的障碍物图层（如墙壁、建筑物）")]
    public LayerMask obstacleLayer;

    // --- 【核心修改】全部换成 LineRenderer ---
    [Header("Vision Visualization")]
    public int meshResolution = 30;
    private GameObject visionConeObj;
    private LineRenderer lineRenderer;

    [Header("Roam Settings")]
    public float roamRadius = 4f;
    public float roamSpeed = 2f;
    public float roamWaitTime = 2f;
    private Vector2 startPosition;
    private Vector2 roamTarget;
    private float roamTimer;
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

        // --- 【全自动生成激光扫描轮廓】 ---
        visionConeObj = new GameObject("VisionScannerLine_" + gameObject.name);
        lineRenderer = visionConeObj.AddComponent<LineRenderer>();

        // 强行寻找保底无光照材质
        Shader lineShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (lineShader == null) lineShader = Shader.Find("Sprites/Default");
        lineRenderer.material = new Material(lineShader);

        // 设置线条颜色、宽度和闭合状态
        lineRenderer.startColor = new Color(1f, 0.9f, 0f, 0.8f); // 亮黄色
        lineRenderer.endColor = new Color(1f, 0.9f, 0f, 0.8f);
        lineRenderer.startWidth = 0.06f; // 线条粗细
        lineRenderer.endWidth = 0.06f;

        // 【核心修复：直接抄怪物的图层！】因为怪物能显示在地图上，光线跟怪物同一个图层就绝对不会被挡住
        SpriteRenderer enemySprite = GetComponentInChildren<SpriteRenderer>();
        if (enemySprite != null)
        {
            lineRenderer.sortingLayerID = enemySprite.sortingLayerID; // 复制怪物的大图层
            lineRenderer.sortingOrder = enemySprite.sortingOrder + 100; // 在怪物上方 100 层
        }
        else
        {
            lineRenderer.sortingOrder = 32000;
        }

        lineRenderer.useWorldSpace = true; // 无视父级，直接在世界画线
        lineRenderer.loop = true; // 将起点和终点连起来，形成闭合的扇形

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

    private void LateUpdate()
    {
        if (lineRenderer != null)
        {
            DrawFieldOfView();
        }
    }

    // 防止怪物死亡后扫描线残留
    private void OnDestroy()
    {
        if (visionConeObj != null)
        {
            Destroy(visionConeObj);
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
                    if (!Physics2D.Raycast(rb.position, directionToPlayer, distanceToPlayer, obstacleLayer))
                    {
                        currentState = EnemyState.Chasing;
                    }
                }
            }
        }
        else if (currentState == EnemyState.Chasing)
        {
            Vector2 directionToPlayer = ((Vector2)targetPlayer.position - rb.position).normalized;
            bool isPlayerHidden = Physics2D.Raycast(rb.position, directionToPlayer, distanceToPlayer, obstacleLayer);

            if (distanceToPlayer > loseTargetDistance || isPlayerHidden)
            {
                currentState = EnemyState.Roaming;
                startPosition = rb.position;
                PickNewRoamTarget();
            }
        }
    }

    private void DrawFieldOfView()
    {
        int rayCount = meshResolution;
        float angle = -viewAngle / 2f;
        float angleStep = viewAngle / rayCount;

        // 点的数量 = 发射的射线数 + 1个圆心起点
        lineRenderer.positionCount = rayCount + 2;

        float zOffset = -1f;

        // 顶点 0 始终是怪物圆心
        lineRenderer.SetPosition(0, new Vector3(rb.position.x, rb.position.y, zOffset));

        Vector2 currentFacing = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float startingAngle = currentFacing.x > 0 ? 0f : 180f;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = startingAngle + angle;
            Vector2 dir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));

            RaycastHit2D hit = Physics2D.Raycast(rb.position, dir, viewRadius, obstacleLayer);

            if (hit.collider != null)
            {
                // 如果撞墙了，轮廓线就在墙边停下
                lineRenderer.SetPosition(i + 1, new Vector3(hit.point.x, hit.point.y, zOffset));
            }
            else
            {
                // 没撞墙，延伸到最远距离
                Vector3 edgePos = rb.position + dir * viewRadius;
                lineRenderer.SetPosition(i + 1, new Vector3(edgePos.x, edgePos.y, zOffset));
            }

            angle += angleStep;
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
            Gizmos.DrawWireSphere(startPosition, roamRadius);
        }
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}