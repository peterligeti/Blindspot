using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class VectorMove : MonoBehaviour
{
    public enum STATE { IDLE, PATROL, PURSUE, ATTACK, FLEE }
    [SerializeField] private STATE currentState = STATE.IDLE;

    private GameObject goal;
    private EnemyHealthLogic healthLogic;

    [Header("General Movement")]
    [SerializeField] float speed = 2.0f;
    [SerializeField] float idealDistanceFromPlayer = 5f;
    [SerializeField] float deadZone = 0.5f;

    [Header("Idle Settings")]
    [SerializeField] float idleMinTime = 1f;
    [SerializeField] float idleMaxTime = 3f;
    private float idleTimer;

    [Header("Pathfinding Settings")]
    Tilemap pathfindTilemap;
    TileBase[] allowedPathfindTiles;

    [Header("Detection Settings")]
    [SerializeField] private float playerDetectionRange = 5f;
    [SerializeField] private float bulletDetectionRange = 3f;

    [Header("Vision Settings")]
    [SerializeField] private bool useVisionCheck = true;
    [SerializeField] private float viewAngle = 60f; // degrees total
    [SerializeField] private float viewDistance = 10f;
    [SerializeField] private int numRays = 5;
    [SerializeField] private LayerMask visionMask;

    [Header("Patrol Settings")]
    [SerializeField] float patrolSpeed = 1.0f;
    [SerializeField] private float patrolRadius = 2f;
    [SerializeField] private float patrolPointTolerance = 0.5f;
    [SerializeField] private Tilemap patrolTilemap;
    [SerializeField] private TileBase[] allowedPatrolTiles;
    private Vector3? patrolTarget = null;

    [Header("Flee Settings")]
    [SerializeField] float fleeSpeed = 3.5f;
    [SerializeField] float fleeRadius = 5f;
    [SerializeField] float fleeDuration = 3f;
    [SerializeField] int fleeHealthThreshold = 1;
    [SerializeField] float fleeChance = 0.5f;
    [SerializeField] float fleeCooldownTime = 5f;
    private float fleeTimer;
    private float fleeCooldownTimer = 0f;
    private Vector3? fleeTarget = null;
    
    [Header("Shooting Settings")]
    [SerializeField] private Shooter shooter; // 🔹 generic shooter
    private float lastShotTime = -Mathf.Infinity;

    private List<Vector3Int> currentPath = null;
    private int currentPathIndex = 0;

    void Start()
    {
        goal = GameObject.FindGameObjectWithTag("Player");
        healthLogic = GetComponent<EnemyHealthLogic>();

        if (shooter != null)
        {
            shooter.shooterTag = "Enemy"; // important so bullets damage the player
        }

        idleTimer = Random.Range(idleMinTime, idleMaxTime);
    }

    public void SetPathfindingTilemap(Tilemap tilemap, TileBase[] allowedTiles)
    {
        pathfindTilemap = tilemap;
        allowedPathfindTiles = allowedTiles;
        if (pathfindTilemap != null && allowedPathfindTiles != null && allowedPathfindTiles.Length > 0)
            AStarPathfinding.Initialize(pathfindTilemap, allowedPathfindTiles);
    }

    public void SetPatrolTilemap(Tilemap tilemap, TileBase[] allowedTiles)
    {
        patrolTilemap = tilemap;
        allowedPatrolTiles = allowedTiles;
    }

    void FixedUpdate()
    {
        if (goal == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, goal.transform.position);
        fleeCooldownTimer -= Time.fixedDeltaTime;

        // Health check for FLEE
        if (currentState != STATE.FLEE && fleeCooldownTimer <= 0 && healthLogic != null && healthLogic.GetCurrentHealth() <= fleeHealthThreshold)
        {
            if (Random.value <= fleeChance)
            {
                StartFlee();
                return;
            }
        }

        // Bullet detection check
        if (BulletNearby())
        {
            currentState = STATE.PURSUE;
        }

        switch (currentState)
        {
            case STATE.IDLE:
                idleTimer -= Time.fixedDeltaTime;
                if (idleTimer <= 0)
                    currentState = STATE.PATROL;

                if (distanceToPlayer <= idealDistanceFromPlayer && CanSeePlayer())
                    currentState = STATE.PURSUE;
                break;

            case STATE.PATROL:
                if (distanceToPlayer <= playerDetectionRange && CanSeePlayer())
                    currentState = STATE.PURSUE;
                else
                    MoveToTarget(FindPatrolTarget(), patrolSpeed);
                break;

            case STATE.PURSUE:
                if (distanceToPlayer <= idealDistanceFromPlayer)
                    currentState = STATE.ATTACK;
                else
                    MoveToTarget(goal.transform.position, speed);
                break;

            case STATE.ATTACK:
                if (distanceToPlayer > idealDistanceFromPlayer + deadZone)
                {
                    currentState = STATE.PURSUE;
                }
                else
                {
                    Vector3 directionToPlayer = (goal.transform.position - transform.position).normalized;
                    float angle = Mathf.Atan2(directionToPlayer.y, directionToPlayer.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);

                    // 🔹 Use Shooter system
                    shooter.Shoot(directionToPlayer);
                }
                break;

            case STATE.FLEE:
                fleeTimer -= Time.fixedDeltaTime;
                if (fleeTimer <= 0)
                {
                    fleeTarget = null;
                    currentState = STATE.IDLE;
                    idleTimer = Random.Range(idleMinTime, idleMaxTime);
                }
                else
                {
                    if (fleeTarget.HasValue)
                    {
                        MoveToTarget(fleeTarget.Value, fleeSpeed);
                    }
                    else
                    {
                        fleeTarget = GenerateFleeTarget();
                    }
                }
                break;
        }
    }

    bool BulletNearby()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, bulletDetectionRange);
        foreach (var hit in hits)
        {
            BulletLogic bullet = hit.GetComponent<BulletLogic>();
            if (bullet != null && bullet.shooterTag == "Player")
            {
                return true;
            }
        }
        return false;
    }

    bool CanSeePlayer()
    {
        if (!useVisionCheck) return true;

        float effectiveRange = Mathf.Min(viewDistance, playerDetectionRange);
        Vector3 forward = -transform.up;

        float step = viewAngle / (numRays - 1);
        for (int i = 0; i < numRays; i++)
        {
            float angle = -viewAngle / 2 + step * i;
            Vector3 rayDir = Quaternion.Euler(0, 0, angle) * forward;

            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDir, effectiveRange, visionMask);

            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                if (hit.collider.CompareTag("Player"))
                {
                    Debug.DrawLine(transform.position, hit.point, Color.green);
                    return true;
                }
                else
                {
                    Debug.DrawLine(transform.position, hit.point, Color.red);
                }
            }
            else
            {
                Debug.DrawLine(transform.position, transform.position + rayDir * effectiveRange, Color.yellow);
            }
        }
        return false;
    }

    void StartFlee()
    {
        currentState = STATE.FLEE;
        fleeTimer = fleeDuration;
        fleeTarget = GenerateFleeTarget();
        fleeCooldownTimer = fleeCooldownTime;
    }

    Vector3 FindPatrolTarget()
    {
        if (patrolTarget == null || Vector3.Distance(transform.position, patrolTarget.Value) < patrolPointTolerance)
        {
            for (int attempts = 0; attempts < 10; attempts++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
                Vector3 candidate = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);
                Vector3Int cellPos = patrolTilemap.WorldToCell(candidate);
                TileBase tileAtPos = patrolTilemap.GetTile(cellPos);
                if (tileAtPos != null && System.Array.Exists(allowedPatrolTiles, t => t == tileAtPos))
                {
                    patrolTarget = patrolTilemap.GetCellCenterWorld(cellPos);
                    break;
                }
            }
        }
        return patrolTarget ?? transform.position;
    }

    Vector3 GenerateFleeTarget()
    {
        Vector3 away = transform.position - goal.transform.position;

        if (away.sqrMagnitude < 0.01f)
        {
            away = Random.insideUnitCircle.normalized;
        }
        else
        {
            away.Normalize();
        }

        for (int i = 0; i < 30; i++)
        {
            Vector3 randomOffset = Random.insideUnitCircle * fleeRadius;
            Vector3 candidate = transform.position + away * fleeRadius + new Vector3(randomOffset.x, randomOffset.y, 0);

            Vector3Int cellPos = patrolTilemap.WorldToCell(candidate);
            TileBase tileAtPos = patrolTilemap.GetTile(cellPos);
            if (tileAtPos != null && System.Array.Exists(allowedPatrolTiles, t => t == tileAtPos))
            {
                return patrolTilemap.GetCellCenterWorld(cellPos);
            }
        }

        Vector3 step = away * 1f;
        Vector3 checkPos = transform.position;

        for (int i = 0; i < Mathf.CeilToInt(fleeRadius); i++)
        {
            checkPos += step;
            Vector3Int cellPos = patrolTilemap.WorldToCell(checkPos);
            TileBase tileAtPos = patrolTilemap.GetTile(cellPos);

            if (tileAtPos != null && System.Array.Exists(allowedPatrolTiles, t => t == tileAtPos))
            {
                return patrolTilemap.GetCellCenterWorld(cellPos);
            }
        }

        Debug.LogWarning("Flee fallback failed – returning current position");
        return transform.position;
    }

    void MoveToTarget(Vector3 targetWorld, float moveSpeed)
    {
        if (pathfindTilemap == null) return;
        Vector3Int startCell = pathfindTilemap.WorldToCell(transform.position);
        Vector3Int targetCell = pathfindTilemap.WorldToCell(targetWorld);

        currentPath = AStarPathfinding.Find(startCell, targetCell);
        currentPathIndex = 0;

        if (currentPath != null && currentPath.Count > 1)
        {
            Vector3 nextPos = pathfindTilemap.GetCellCenterWorld(currentPath[1]);
            Vector3 moveDir = nextPos - transform.position;
            transform.position += moveDir.normalized * moveSpeed * Time.fixedDeltaTime;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg + 90f);
        }
    }

    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 1 && pathfindTilemap != null)
        {
            switch (currentState)
            {
                case STATE.PATROL: Gizmos.color = Color.cyan; break;
                case STATE.PURSUE: Gizmos.color = Color.yellow; break;
                case STATE.ATTACK: Gizmos.color = Color.red; break;
                case STATE.FLEE: Gizmos.color = Color.green; break;
                default: return;
            }

            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Vector3 start = pathfindTilemap.GetCellCenterWorld(currentPath[i]);
                Vector3 end = pathfindTilemap.GetCellCenterWorld(currentPath[i + 1]);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(pathfindTilemap.GetCellCenterWorld(currentPath[i + 1]), 0.1f);
            }
        }

        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, bulletDetectionRange);

        if (currentState == STATE.FLEE)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, fleeRadius);
        }

        if (useVisionCheck)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            float effectiveRange = Mathf.Min(viewDistance, playerDetectionRange);
            Vector3 forward = -transform.up;

            Quaternion leftRot = Quaternion.Euler(0, 0, -viewAngle / 2);
            Quaternion rightRot = Quaternion.Euler(0, 0, viewAngle / 2);

            Vector3 leftDir = leftRot * forward * effectiveRange;
            Vector3 rightDir = rightRot * forward * effectiveRange;

            Gizmos.DrawLine(transform.position, transform.position + forward * effectiveRange);
            Gizmos.DrawLine(transform.position, transform.position + leftDir);
            Gizmos.DrawLine(transform.position, transform.position + rightDir);

            float step = viewAngle / (numRays - 1);
            for (int i = 0; i < numRays; i++)
            {
                float angle = -viewAngle / 2 + step * i;
                Vector3 dir = Quaternion.Euler(0, 0, angle) * forward * effectiveRange;
                Gizmos.DrawLine(transform.position, transform.position + dir);
            }
        }
    }
}
