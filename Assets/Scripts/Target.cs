using UnityEngine;

public class Target : MonoBehaviour
{
    // Removed health from here
    // EnemyHealth script handles everything now

    public void TakeDamage(float amount)
    {
        EnemyHealth enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage((int)amount);
        }
    }
}