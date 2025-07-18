using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;


//Bridge between data oriented design and GameObjects
public class PathRequestManager : MonoBehaviour
{
    public static PathRequestManager Instance { get; private set; }

    // Parameter to control agent spreading to fix conga lines
    [Header("Pathfinding Parameters")]
    [Tooltip("The max radius around the target to find an alternative pathing spot. Prevents agent clumping.")]
    [SerializeField] private float pathingSpreadRadius = 1.0f;
    [Tooltip("How many times to try finding a walkable random spot before giving up.")]
    [SerializeField] private int pathingSpreadMaxTries = 10;

    // Safety limit for the nearest node search 
    [Tooltip("The max number of nodes to check when searching for the nearest walkable node.")]
    [SerializeField] private int nearestNodeSearchLimit = 100;

    [Header("Performance")]
    [Tooltip("The maximum number of pathfinding jobs that can be scheduled in a single frame.")]
    [SerializeField] private int maxJobsPerFrame = 10;

    private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    private PathRequest currentPathRequest;

    // This list holds all jobs that are currently running.
    private List<ProcessingJob> activeJobs = new List<ProcessingJob>();

    private GridManager gridManager;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); }
        else { Instance = this; }

        gridManager = GridManager.Instance;

    }
    private void Start()
    {
        //StartCoroutine(WaitForGridAndProcess());
    }
    // This coroutine will wait until the grid is ready, and then start
    // the main processing loop of the manager.
    private IEnumerator WaitForGridAndProcess()
    {
        // Wait until the GridManager signals that it's done.
        yield return new WaitUntil(() => gridManager.IsGridReady);

        // Now that the grid is ready, we can start our own processing loop.
        // We use LateUpdate as before for the main job management.
        this.enabled = true; // Or set a specific flag to start processing in LateUpdate.
    }

    // This is the core processing loop, running at the end of the frame.
    private void LateUpdate()
    {
         if (!gridManager.IsGridReady) return; // Don't do anything until grid is built

            // 1. Process completed jobs from the previous frame(s).
            // We iterate backwards so we can safely remove items from the list.
            for (int i = activeJobs.Count - 1; i >= 0; i--)
            {
                var job = activeJobs[i];
                if (job.handle.IsCompleted)
                {
                    // The job is done, so we must complete it to access its data.
                    job.handle.Complete();

                    // Process the results now that we have them.
                    ProcessFinishedJob(job);

                    // Remove from the active list.
                    activeJobs.RemoveAt(i);
                }
            }

            // 2. Schedule new jobs from the queue, up to our per-frame limit.
            int jobsToSchedule = Mathf.Min(maxJobsPerFrame, pathRequestQueue.Count);
            for (int i = 0; i < jobsToSchedule; i++)
            {
                PathRequest request = pathRequestQueue.Dequeue();
                ScheduleNewJob(request);
            }
        
    }
    private void ScheduleNewJob(PathRequest request)
    {
        // This logic runs on the main thread to prepare the job's data.
        Node startNode = gridManager.NodeFromWorldPoint(request.pathStart);
        Node endNode = gridManager.NodeFromWorldPoint(request.pathEnd);

        // Find a valid endpoint if the requested one is unwalkable.
        bool isEndpointValid = true;
        if (!endNode.isWalkable)
        {
            endNode = FindNearestWalkableNode(endNode);
            if (endNode == null)
            {
                isEndpointValid = false;
            }
        }

        // If the endpoint is valid, try to spread agents out.
        if (isEndpointValid && endNode.isWalkable)
        {
            for (int i = 0; i < pathingSpreadMaxTries; i++)
            {
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * pathingSpreadRadius;
                Node potentialEndNode = gridManager.NodeFromWorldPoint(request.pathEnd + randomOffset);
                if (potentialEndNode.isWalkable)
                {
                    endNode = potentialEndNode;
                    break;
                }
            }
        }

        // If we couldn't find ANY valid end node, fail the path immediately.
        if (!isEndpointValid || endNode == null)
        {
            request.callback(new Vector2[0], false);
            return;
        }

        // Create the native list for the job's output. IMPORTANT: Allocator.TempJob
        var pathResultList = new NativeList<int2>(Allocator.TempJob);

        var astarJob = new AStarJob
        {
            startPosition = new int2(startNode.gridX, startNode.gridY),
            endPosition = new int2(endNode.gridX, endNode.gridY),
            gridSize = new int2(gridManager.gridSizeX, gridManager.gridSizeY),
            nodeGrid = gridManager.PathNodeGrid,
            path = pathResultList
        };

        // Schedule the job and get a handle to it.
        JobHandle handle = astarJob.Schedule();

        // Store all the data for this job so we can process it later.
        activeJobs.Add(new ProcessingJob
        {
            handle = handle,
            pathOutput = pathResultList,
            callback = request.callback,
            originalStartPos = request.pathStart
        });
    }

    private void ProcessFinishedJob(ProcessingJob job)
    {
        Vector2[] waypoints;
        bool pathSuccess = false;

        if (job.pathOutput.Length > 0)
        {
            pathSuccess = true;
            // The path smoothing now needs the original start position.
            waypoints = SmoothPath(job.pathOutput, job.originalStartPos);
        }
        else
        {
            waypoints = new Vector2[0];
        }

        // Invoke the original requester's callback with the final path.
        job.callback(waypoints, pathSuccess);

        // Dispose the native list now that we are done with it.
        job.pathOutput.Dispose();
    }
    // This is the public method other scripts will call to request a path.
    // GameObjects will call this one
    public void RequestPath(Vector2 pathStart, Vector2 pathEnd, Action<Vector2[], bool> callback)
    {
            PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
            pathRequestQueue.Enqueue(newRequest);
    }
    private void OnDestroy()
    {
        // This is a safety net. If the manager is destroyed, we must complete
        // any jobs that are still running to prevent memory leaks and errors.
            foreach (var job in activeJobs)
            {
                job.handle.Complete();
                job.pathOutput.Dispose();
            }
        
    }
    //  Path Smoothing (String Pulling) Algorithm
    private Vector2[] SmoothPath(NativeList<int2> rawPath, Vector2 requesterPosition)
    {
        if (rawPath.Length == 0)
        {
            return new Vector2[0];
        }

        // Convert raw grid coordinates to world positions first
        List<Vector2> worldPath = new List<Vector2>();
        foreach (var gridPos in rawPath)
        {
            worldPath.Add(gridManager.GetNode(gridPos.x, gridPos.y).worldPosition);
        }

        List<Vector2> smoothedPath = new List<Vector2>();
        // The first point is always the agent's actual current position, not the center of its node.
        // This prevents the agent from turning backwards to get to its first waypoint.
        smoothedPath.Add(requesterPosition);

        int pathIndex = 0;

        while (pathIndex < worldPath.Count)
        {
            // Start from the last point we added to our smoothed path
            Vector2 lastSmoothedPoint = smoothedPath[smoothedPath.Count - 1];

            // Look ahead as far as possible
            for (int i = worldPath.Count - 1; i > pathIndex; i--)
            {
                // Is there a direct, un-obstructed line of sight?
                if (!Physics2D.Linecast(lastSmoothedPoint, worldPath[i], gridManager.UnwalkableMask))
                {
                    smoothedPath.Add(worldPath[i]);
                    pathIndex = i;
                    goto next_path_segment;
                }
            }

            // If we get here, no direct line was found to any further point.
            // We must add the very next point in the path and continue from there.
            if (pathIndex < worldPath.Count) // Ensure we don't go out of bounds
            {
                smoothedPath.Add(worldPath[pathIndex]);
            }
            pathIndex++;

        next_path_segment:;
        }

        return smoothedPath.ToArray();
    }
    //  Method to find the nearest walkable node
    private Node FindNearestWalkableNode(Node originNode)
    {
        Queue<Node> queue = new Queue<Node>();
        HashSet<Node> visited = new HashSet<Node>();

        queue.Enqueue(originNode);
        visited.Add(originNode);

        int nodesSearched = 0;

        while (queue.Count > 0)
        {
            Node currentNode = queue.Dequeue();

            // Safety break to prevent infinite loops in fully blocked areas
            nodesSearched++;
            if (nodesSearched > nearestNodeSearchLimit)
            {
                Debug.LogWarning("Nearest walkable node search exceeded limit. Path request may fail.");
                return null;
            }

            foreach (Node neighbor in gridManager.GetNeighbours(currentNode))
            {
                if (visited.Contains(neighbor)) continue;
                visited.Add(neighbor);

                if (neighbor.isWalkable)
                {
                    // Found the first (and therefore one of the nearest) walkable nodes.
                    return neighbor;
                }
                else
                {
                    // It's not walkable, so add it to the queue to check its neighbors later.
                    queue.Enqueue(neighbor);
                }
            }
        }
        // If the loop completes, no walkable node was found within the search area.
        return null;
    }
    // A simple struct to hold all the information for a single path request
    struct PathRequest
    {
        public Vector2 pathStart;
        public Vector2 pathEnd;
        public Action<Vector2[], bool> callback;

        public PathRequest(Vector2 _start, Vector2 _end, Action<Vector2[], bool> _callback)
        {
            pathStart = _start;
            pathEnd = _end;
            callback = _callback;
        }
    }
    private struct ProcessingJob
    {
        public JobHandle handle;
        public NativeList<int2> pathOutput;
        public Action<Vector2[], bool> callback;
        public Vector2 originalStartPos; // Needed for path smoothing
    }
}