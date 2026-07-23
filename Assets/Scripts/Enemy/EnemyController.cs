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

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;        // 发现玩家后的追击速度
    public float loseTargetDistance = 12f;// 玩家逃出多远后放弃追击

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
        else
        {
            Debug.Log("怪物成功锁定玩家，开始巡逻！");
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

            // 打印发呆状态
            Debug.Log($"【排错】到达目标点，正在发呆... 剩余时间: {roamTimer:F1} 秒");

            if (roamTimer <= 0)
            {
                PickNewRoamTarget();
                Debug.Log($"【排错】发呆结束！生成了新的巡逻点: {roamTarget}");
            }
        }
        else
        {
            Vector2 direction = (roamTarget - rb.position).normalized;
            rb.linearVelocity = direction * roamSpeed;

            // 打印移动状态
            Debug.Log($"【排错】正在走向目标 {roamTarget} | 当前速度: {rb.linearVelocity} | 距离: {distanceToTarget:F2}");

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            rb.rotation = Mathf.LerpAngle(rb.rotation, targetAngle, Time.fixedDeltaTime * 3f);
        }
    }

    // --- 追击逻辑 ---
    private void HandleChasing()
    {
        // 笔直冲向玩家
        Vector2 direction = ((Vector2)targetPlayer.position - rb.position).normalized;
        rb.linearVelocity = direction * chaseSpeed; // (已替换为 linearVelocity)

        // 瞬间转身死死盯着玩家
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        rb.rotation = angle;
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
    }
}