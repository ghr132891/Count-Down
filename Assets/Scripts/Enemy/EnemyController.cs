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

    // 视线可视化相关（全部改为私有，由代码全自动接管）
    public int meshResolution = 30;
    private GameObject visionConeObj; // 独立的光束物体
    private MeshFilter viewMeshFilter;
    private Mesh viewMesh;

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

        // --- 【终极剥离法：全自动生成独立光束，彻底粉碎翻转隐形Bug】 ---
        visionConeObj = new GameObject("AutoVisionCone_" + gameObject.name);
        // 【核心】：不设置为子物体，放在世界根目录，避开怪物负缩放的毒害
        visionConeObj.transform.position = transform.position;

        viewMeshFilter = visionConeObj.AddComponent<MeshFilter>();
        MeshRenderer mr = visionConeObj.AddComponent<MeshRenderer>();

        // 使用底层 UI 材质：这个材质自带透明通道、无视环境光、且永远不会被剔除
        Material mat = new Material(Shader.Find("UI/Default"));
        mr.material = mat;
        mr.sortingOrder = 32000; // 极高图层，稳压所有地图瓦片

        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;

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
        // 每帧让光束物体跟着怪物跑，并更新网格
        if (visionConeObj != null && viewMesh != null)
        {
            visionConeObj.transform.position = transform.position;
            DrawFieldOfView();
        }
    }

    // 防止怪物死亡或被销毁时，光束残留
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

    // --- 【防弹级：绝对坐标网格绘制法】 ---
    private void DrawFieldOfView()
    {
        int rayCount = meshResolution;
        float angle = -viewAngle / 2f;
        float angleStep = viewAngle / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2];
        int[] triangles = new int[rayCount * 6];

        vertices[0] = Vector3.zero; // 局部圆心始终为 0

        Vector2 currentFacing = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        float startingAngle = currentFacing.x > 0 ? 0f : 180f;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = startingAngle + angle;
            Vector2 dir = new Vector2(Mathf.Cos(currentAngle * Mathf.Deg2Rad), Mathf.Sin(currentAngle * Mathf.Deg2Rad));

            RaycastHit2D hit = Physics2D.Raycast(rb.position, dir, viewRadius, obstacleLayer);

            if (hit.collider != null)
            {
                // 因为光束物体脱离了父级，不再受缩放影响，直接用世界坐标差值！
                Vector3 localPos = (Vector3)hit.point - visionConeObj.transform.position;
                localPos.z = 0f;
                vertices[i + 1] = localPos;
            }
            else
            {
                Vector3 localPos = (Vector3)(dir * viewRadius);
                localPos.z = 0f;
                vertices[i + 1] = localPos;
            }

            if (i < rayCount)
            {
                // 双面三角形绘制，无论摄像机怎么看都绝对不可能隐形
                triangles[i * 6] = 0;
                triangles[i * 6 + 1] = i + 2;
                triangles[i * 6 + 2] = i + 1;

                triangles[i * 6 + 3] = 0;
                triangles[i * 6 + 4] = i + 1;
                triangles[i * 6 + 5] = i + 2;
            }

            angle += angleStep;
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;

        // 直接注入黄色半透明顶点色
        Color[] colors = new Color[vertices.Length];
        Color fovColor = new Color(1f, 0.9f, 0f, 0.35f);
        for (int i = 0; i < vertices.Length; i++)
        {
            colors[i] = fovColor;
        }
        viewMesh.colors = colors;

        viewMesh.RecalculateNormals();
        viewMesh.RecalculateBounds();
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