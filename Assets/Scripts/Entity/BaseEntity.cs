using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class BaseEntity : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 5f;
    public float maxHealth = 100f;
    public float currentHealth;

    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // 俯视视角不需要重力
        rb.gravityScale = 0f;
        currentHealth = maxHealth;
    }

    // 预留受伤接口
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }


}
