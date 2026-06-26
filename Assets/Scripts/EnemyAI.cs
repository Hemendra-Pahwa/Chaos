using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("Targeting")]
    public Transform player;
    public float shootRange = 10f;
    public float chaseRange = 30f;
    public float fireRate = 1f;

    [Header("Patrol")]
    public Transform[] patrolPoints;
    public float patrolPointTolerance = 1f;
    public bool loopPatrol = true;

    [Header("Movement")]
    public float groundOffset = 0f;

    [Header("Debug")]
    public bool drawNavigationPath = true;
    public Color patrolColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    public Color currentPathColor = new Color(1f, 0.85f, 0.2f, 0.9f);

    private Animator animator;
    private NavMeshAgent agent;
    private float nextFireTime;
    private int patrolIndex;
    private NavMeshPath debugPath;

    private void Start()
    {
        animator = GetComponentInChildren<Animator>(true);
        agent = GetComponent<NavMeshAgent>();
        var capsuleCollider = GetComponent<CapsuleCollider>();
        var visualRoot = animator != null ? animator.transform : transform.childCount > 0 ? transform.GetChild(0) : null;
        debugPath = new NavMeshPath();

        agent.updateRotation = false;
        agent.updateUpAxis = true;
        agent.baseOffset = ResolveGroundOffset(capsuleCollider, visualRoot);

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        // Ensure the enemy is on the NavMesh; warp to nearest valid position if not.
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[EnemyAI] {name} could not find a NavMesh position within 5 units. Make sure the NavMesh is baked and the enemy is placed on walkable ground.");
            }
        }
    }

    private float ResolveGroundOffset(CapsuleCollider capsuleCollider, Transform visualRoot)
    {
        if (groundOffset > 0f)
        {
            return groundOffset;
        }

        if (visualRoot != null)
        {
            var visualOffset = Mathf.Abs(Mathf.Min(0f, visualRoot.localPosition.y));
            if (visualOffset > 0.001f)
            {
                return visualOffset;
            }
        }

        return capsuleCollider != null
            ? Mathf.Max(0f, capsuleCollider.center.y + (capsuleCollider.height * 0.5f))
            : agent.baseOffset;
    }

    private void Update()
    {
        if (agent == null)
        {
            return;
        }

        // If still off the NavMesh, keep trying to warp until we land on it.
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out var hit, 8f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            return;
        }

        UpdateTargeting();
        UpdateAnimation();
        UpdateFacing();
    }

    private void UpdateTargeting()
    {
        if (player == null)
        {
            Patrol();
            return;
        }

        var distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Always chase the player; only stop to shoot when close enough.
        if (distanceToPlayer > shootRange)
        {
            MoveTo(player.position);
            return;
        }

        agent.ResetPath();

        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + 1f / fireRate;
            ShootPlayer();
        }
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            agent.ResetPath();
            return;
        }

        var targetPoint = patrolPoints[patrolIndex];
        if (targetPoint == null)
        {
            AdvancePatrolIndex();
            return;
        }

        MoveTo(targetPoint.position);
        if (!agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, patrolPointTolerance))
        {
            AdvancePatrolIndex();
        }
    }

    private void AdvancePatrolIndex()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        if (loopPatrol)
        {
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
            return;
        }

        patrolIndex = Mathf.Min(patrolIndex + 1, patrolPoints.Length - 1);
    }

    private void MoveTo(Vector3 worldPosition)
    {
        agent.SetDestination(worldPosition);
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(SpeedHash, agent.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    private void UpdateFacing()
    {
        Vector3 lookDirection;

        if (player != null && Vector3.Distance(transform.position, player.position) <= shootRange)
        {
            lookDirection = player.position - transform.position;
        }
        else if (agent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            lookDirection = agent.desiredVelocity;
        }
        else
        {
            return;
        }

        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        var targetRotation = Quaternion.LookRotation(lookDirection.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    private void ShootPlayer()
    {
        var playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        if (playerHealth == null)
        {
            return;
        }

        playerHealth.TakeDamage(5);
        Debug.Log("Enemy Shot Player");
    }

    private void OnDrawGizmosSelected()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (debugPath == null)
        {
            debugPath = new NavMeshPath();
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = patrolColor;
            for (var i = 0; i < patrolPoints.Length; i++)
            {
                var current = patrolPoints[i];
                if (current == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(current.position, 0.25f);

                var nextIndex = i + 1;
                if (nextIndex >= patrolPoints.Length)
                {
                    if (!loopPatrol)
                    {
                        continue;
                    }

                    nextIndex = 0;
                }

                var next = patrolPoints[nextIndex];
                if (next != null)
                {
                    Gizmos.DrawLine(current.position, next.position);
                }
            }
        }

        if (!drawNavigationPath || agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        if (!agent.hasPath && player != null)
        {
            NavMesh.CalculatePath(transform.position, player.position, NavMesh.AllAreas, debugPath);
            DrawPath(debugPath, currentPathColor);
            return;
        }

        DrawPath(agent.path, currentPathColor);
    }

    private static void DrawPath(NavMeshPath path, Color color)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
        {
            return;
        }

        Gizmos.color = color;
        for (var i = 0; i < path.corners.Length - 1; i++)
        {
            Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
        }
    }
}
