using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public Transform spawnPoint;

    public bool IsDead { get; private set; } // ← ADD THIS LINE HERE

    private Renderer[] renderers;
    private Collider[] colliders;
    private NavMeshAgent agent;

    void Start()
    {
        currentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        agent = GetComponent<NavMeshAgent>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            StartCoroutine(Respawn());
        }
    }

    IEnumerator Respawn()
    {
        IsDead = true; // ← SET TRUE AT START

        foreach (Renderer r in renderers) r.enabled = false;
        foreach (Collider c in colliders) c.enabled = false;
        if (agent != null) agent.enabled = false;

        yield return new WaitForSeconds(3f);

        transform.position = spawnPoint.position;
        currentHealth = maxHealth;

        if (agent != null) agent.enabled = true;
        foreach (Renderer r in renderers) r.enabled = true;
        foreach (Collider c in colliders) c.enabled = true;

        IsDead = false; // ← SET FALSE AT END
    }
}