using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using Nebukam.ORCA;
using Nebukam.Common;


//A placeholder class to show how  pathfinding works on GameObjects

public class EnemyAgent : MonoBehaviour
{
    [Header("Pathfinding")]
    [Tooltip("The target the agent will chase. Usually the player.")]
    public Transform target;
    [Tooltip("How often the agent checks if it needs a new path (in seconds).")]
    public float pathUpdateRate = 0.25f;
    [Tooltip("How far the target can move from the path's end before a new path is requested.")]
    public float pathRecalculationThreshold = 1f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    [Tooltip("How close the agent needs to be to a waypoint to move to the next one.")]
    public float waypointReachedThreshold = 0.1f;

    private Vector2[] currentPath;
    private int targetWaypointIndex;
    private Agent orcaAgent;

    void Start()
    {
        if (ORCASimulationManager.Instance != null)
        {
            orcaAgent = ORCASimulationManager.Instance.RegisterAgent(this);
        }
        else
        {
            Debug.LogError("ORCASimulationManager not found in scene! Disabling agent.", this);
            this.enabled = false;
            return;
        }

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
            else
            {
                Debug.LogError("EnemyAgent cannot find a target with tag 'Player'. Disabling script.", this);
                this.enabled = false;
                return;
            }
        }

        StartCoroutine(UpdatePath());
    }

    private void Update()
    {
        UpdateMovement();
        //UpdateSimpleMovement();
    }
    void OnDestroy()
    {
        /*if (ORCASimulationManager.Instance != null && orcaAgent != null)
        {
            ORCASimulationManager.Instance.UnregisterAgent(this);
        }*/
    }
    public void UpdateSimpleMovement()
    {
        if (currentPath == null || currentPath.Length == 0)
        {
            // If there's no path, don't move.
            return;
        }

        // --- This part is the same: Follow the waypoints ---
        Vector2 currentWaypoint = currentPath[targetWaypointIndex];

        // Use squared distance for a minor performance improvement
        if (Vector2.SqrMagnitude((Vector2)transform.position - currentWaypoint) < waypointReachedThreshold * waypointReachedThreshold)
        {
            targetWaypointIndex++;
            if (targetWaypointIndex >= currentPath.Length)
            {
                // Reached the end of the path
                currentPath = null;
                return;
            }
        }

        // --- This part is now simplified ---
        // Get the direction to the *next* waypoint
        currentWaypoint = currentPath[targetWaypointIndex];
        Vector2 desiredDirection = (currentWaypoint - (Vector2)transform.position).normalized;

        // Calculate the movement vector for this frame
        Vector3 movement = desiredDirection * moveSpeed * Time.deltaTime;

        // Apply the movement directly to the transform.
        transform.position += movement;
    }
    public void OnPathFound(Vector2[] newPath, bool pathSuccessful)
    {
        if (pathSuccessful && this.enabled)
        {
            currentPath = newPath;
            targetWaypointIndex = 0;
        }
    }

    public void UpdateMovement()
    {
        if (orcaAgent == null) return;

        float3 desiredVelocity = float3.zero;

        if (currentPath != null && currentPath.Length > 0)
        {
            Vector2 currentWaypoint = currentPath[targetWaypointIndex];

            if (Vector2.Distance(transform.position, currentWaypoint) < waypointReachedThreshold)
            {
                targetWaypointIndex++;
                if (targetWaypointIndex >= currentPath.Length)
                {
                    currentPath = null;
                }
                else
                {
                    currentWaypoint = currentPath[targetWaypointIndex];
                }
            }

            if (currentPath != null)
            {
                Vector2 desiredDirection = (currentWaypoint - (Vector2)transform.position).normalized;
                desiredVelocity = new float3(desiredDirection.x, desiredDirection.y, 0f) * moveSpeed;
            }
        }

        ORCASimulationManager.Instance.SetAgentPreferredVelocity(orcaAgent, desiredVelocity);

        float3 safeVelocity = orcaAgent.velocity;
        transform.position += new Vector3(safeVelocity.x, safeVelocity.y, 0f) * Time.deltaTime;
    }

    IEnumerator UpdatePath()
    {
        yield return new WaitForSeconds(0.1f);

        PathRequestManager.Instance.RequestPath(transform.position, target.position, OnPathFound);

        while (true)
        {
            yield return new WaitForSeconds(pathUpdateRate);
            if (this.enabled && target != null && currentPath != null && currentPath.Length > 0)
            {
                Vector2 endOfCurrentPath = currentPath[currentPath.Length - 1];
                if (Vector2.Distance(target.position, endOfCurrentPath) > pathRecalculationThreshold)
                {
                    PathRequestManager.Instance.RequestPath(transform.position, target.position, OnPathFound);
                }
            }
        }
    }

    /*
    public void OnDrawGizmos()
    {
        if (currentPath != null)
        {
            for (int i = targetWaypointIndex; i < currentPath.Length; i++)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(currentPath[i], Vector3.one * 0.2f);

                if (i == targetWaypointIndex)
                {
                    Gizmos.DrawLine(transform.position, currentPath[i]);
                }
                else
                {
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                }
            }
        }
    }
    */
}