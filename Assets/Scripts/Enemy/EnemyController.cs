using UnityEngine;

// 路径: Assets/Scripts/Enemy/EnemyController.cs
public class EnemyController : BaseEntity
{
    public enum EnemyState
    {
        Roaming, // 局部走动（巡逻）
        Chasing  // 追击玩家
    }

    [Header("Current State")]
    public EnemyState currentState = EnemyState.Roaming;

    [Header("Vision Settings (Flashlight)")]
    public float viewRadius = 8f;        // 视线最远距离
    [Range(0, 360)]
    public float viewAngle = 60f;        // 视线张角（手电筒的宽度）

    [Header("Roam Settings")]
    public float roamRadius = 4f;        // 局部走动的活动范围
    public float roamSpeed = 2f;         // 走动速度
    public float roamWaitTime = 2f;      // 走到目标点后发呆的时间

    private Vector2 startPosition;       // 怪物出生的初始位置
    private Vector2 roamTarget;          // 当前正在前往的巡逻目标点
    private float roamTimer;             // 发呆倒计时

    [Header("Chase & Combat Settings")]
    public float chaseSpeed = 5f;
    public float loseTargetDistance = 12f;

    // --- 新增的近战属性 ---
    public Transform attackPoint;      // 怪物的攻击判定点
    public float attackRange = 1.2f;   // 怪物的攻击半径 (通常比追击停止距离稍微大一点点)
    public float stopDistance = 1f;    // 距离玩家多近时停下脚步砍人
    public float attackDamage = 15f;   // 怪物攻击力
    public float attackRate = 1f;      // 怪物攻击间隔
    public LayerMask playerLayer;      // 玩家所在的图层

    private float nextAttackTime = 0f;

    private Transform targetPlayer;

    protected override void Awake()
    {
        base.Awake();

        // 记录初始位置，怪物只会在这个位置附近小范围走动
        startPosition = rb.position;

        // 寻找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
        }


        // 随机生成第一个巡逻点
        PickNewRoamTarget();
    }

    private void FixedUpdate()
    {
        if (targetPlayer == null)
        {
            Debug.LogWarning("【排错】怪物找不到 Player！FixedUpdate 拒绝执行。");
            return;
        }

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

    // --- 核心：扇形视线检测 ---
    private void CheckVision()
    {
        float distanceToPlayer = Vector2.Distance(rb.position, targetPlayer.position);

        if (currentState == EnemyState.Roaming)
        {
            // 1. 玩家必须在手电筒照射距离内
            if (distanceToPlayer <= viewRadius)
            {
                // 计算从怪物指向玩家的向量
                Vector2 directionToPlayer = ((Vector2)targetPlayer.position - rb.position).normalized;

                // 2. 计算怪物当前正前方(transform.up)与玩家方向的夹角
                float angleToPlayer = Vector2.Angle(transform.up, directionToPlayer);

                // 3. 如果夹角小于总视角的一半，说明在扇形区域内！
                if (angleToPlayer <= viewAngle / 2f)
                {
                    // 发现目标，切换为追击状态
                    currentState = EnemyState.Chasing;
                }
            }
        }
        else if (currentState == EnemyState.Chasing)
        {
            // 如果玩家逃跑距离超过了放弃追击的距离
            if (distanceToPlayer > loseTargetDistance)
            {
                // 放弃追击，回到巡逻状态
                currentState = EnemyState.Roaming;
                startPosition = rb.position; // 以当前位置为新中心重新巡逻
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



            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = Mathf.LerpAngle(rb.rotation, targetAngle, Time.fixedDeltaTime * 3f);
        }
    }

    // --- 追击逻辑 ---
    private void HandleChasing()
    {
        float distanceToPlayer = Vector2.Distance(rb.position, targetPlayer.position);

        // 笔直冲向玩家
        Vector2 direction = ((Vector2)targetPlayer.position - rb.position).normalized;
        rb.linearVelocity = direction * chaseSpeed; // (已替换为 linearVelocity)

        // 瞬间转身死死盯着玩家
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;

        // 如果还没到达攻击距离，继续追
        if (distanceToPlayer > stopDistance)
        {
            rb.linearVelocity = direction * chaseSpeed;
        }
        else
        {
            // 距离足够近，停下脚步
            rb.linearVelocity = Vector2.zero;

            // 攻击冷却完毕，开始咬人
            if (Time.time >= nextAttackTime)
            {
                MeleeAttack();
                nextAttackTime = Time.time + attackRate;
            }
        }
    }

    private void MeleeAttack()
    {
        if (attackPoint == null) return;

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, playerLayer);

        foreach (Collider2D player in hitPlayers)
        {
            BaseEntity entity = player.GetComponent<BaseEntity>();
            if (entity != null)
            {
                entity.TakeDamage(attackDamage);
                Debug.Log($"怪物咬中了玩家！造成 {attackDamage} 点伤害！");
            }
        }
    }

    // --- 随机选取巡逻点 ---
    private void PickNewRoamTarget()
    {
        // 在初始位置周围的圆内随机取一个点
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        roamTarget = startPosition + randomOffset;

        // 重置发呆时间
        roamTimer = roamWaitTime;
    }

    // --- 编辑器可视化：画出手电筒视线，方便你调整数值 ---
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // 怪物的正前方 (因为 2D 精灵转了 -90 度，所以 transform.up 就是它的正前方)
        Vector3 forward = transform.up * viewRadius;

        // 根据视角计算左边缘和右边缘的射线方向
        Quaternion leftRayRotation = Quaternion.Euler(0, 0, viewAngle / 2f);
        Quaternion rightRayRotation = Quaternion.Euler(0, 0, -viewAngle / 2f);

        Vector3 leftRay = leftRayRotation * forward;
        Vector3 rightRay = rightRayRotation * forward;

        // 画出扇形的两条边
        Gizmos.DrawRay(transform.position, leftRay);
        Gizmos.DrawRay(transform.position, rightRay);

        // 画出巡逻范围（红色线框）
        if (Application.isPlaying)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(startPosition, roamRadius);
        }

        // --- 画出攻击范围 ---
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}