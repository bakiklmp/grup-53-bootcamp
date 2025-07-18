using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    public bool IsGridReady { get; private set; } = false;
    public LayerMask UnwalkableMask { get { return unwalkableMask; } }

    [Tooltip("The layers that are considered unwalkable obstacles.")]
    public LayerMask unwalkableMask;

    [Tooltip("The physical size of the grid in world units.")]
    public Vector2 gridWorldSize;

    [Tooltip("The radius of each node. A smaller radius means a more detailed, but more expensive grid.")]
    public float nodeRadius;

    [Tooltip("Define different terrain types and their movement cost. Order from lowest penalty to highest.")]
    public TerrainType[] walkableRegions;
    LayerMask walkableMask; 

    private Node[,] grid;
    private float nodeDiameter;
    public int gridSizeX { get; private set; }
    public int gridSizeY { get; private set; }

    private NativeArray<PathNode> pathNodeGrid;
    public NativeArray<PathNode> PathNodeGrid { get { return pathNodeGrid; } }

    // A dictionary to quickly look up layer penalties
    private Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    [SerializeField]private bool sceneGridGizmo;

    [Header("Performance")]
    [Tooltip("How many nodes to process per frame during initial grid creation. A higher number is faster but may cause more stutter.")]
    public int nodesPerFrame = 500;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);

        // Populate the dictionary and build a combined layer mask for walkable regions
        foreach (TerrainType region in walkableRegions)
        {
            walkableMask.value |= region.terrainMask.value; // Add layer to the combined mask
            // The key is the layer number (0-31), not the mask's value
            int layer = (int)Mathf.Log(region.terrainMask.value, 2);
            if (!walkableRegionsDictionary.ContainsKey(layer))
            {
                walkableRegionsDictionary.Add(layer, region.terrainPenalty);
            }
        }
        StartCoroutine(CreateGridAsync());
        //CreateGrid();
    }

    private void OnDestroy()
    {
        if (pathNodeGrid.IsCreated)
        {
            pathNodeGrid.Dispose();
        }
    }
    // This method can be called to force a grid update if walls are destroyed, etc.
    /*public void CreateGrid()
    {
        grid = new Node[gridSizeX, gridSizeY];
        Vector2 worldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2 - Vector2.up * gridWorldSize.y / 2;

        //Handle disposal and recreation of the native grid
        if (pathNodeGrid.IsCreated)
        {
            pathNodeGrid.Dispose();
        }
        pathNodeGrid = new NativeArray<PathNode>(gridSizeX * gridSizeY, Allocator.Persistent);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius) + Vector2.up * (y * nodeDiameter + nodeRadius);

                // Check if the node is on an unwalkable layer using Physics2D
                bool isWalkable = !(Physics2D.OverlapCircle(worldPoint, nodeRadius, unwalkableMask));

                int movementPenalty = 0;

                // If it's walkable, check for terrain cost
                if (isWalkable)
                {
                    // Check which walkable region this node falls into.
                    // This checks for any colliders on our walkable layers at the node's position.
                    Collider2D[] colliders = Physics2D.OverlapPointAll(worldPoint, walkableMask);

                    // We iterate through all colliders at this point and apply the highest penalty found.
                    // This handles overlapping zones correctly (e.g., a "blessed" zone inside "mud").
                    foreach (Collider2D c in colliders)
                    {
                        if (walkableRegionsDictionary.ContainsKey(c.gameObject.layer))
                        {
                            movementPenalty = Mathf.Max(movementPenalty, walkableRegionsDictionary[c.gameObject.layer]);
                        }
                    }
                }

                grid[x, y] = new Node(isWalkable, worldPoint, x, y, movementPenalty);

                // Populate the persistent native grid at the same time 
                int index = x + y * gridSizeX;
                pathNodeGrid[index] = new PathNode
                {
                    x = x,
                    y = y,
                    index = index,
                    isWalkable = isWalkable,
                    movementPenalty = movementPenalty,
                    gCost = int.MaxValue,
                    hCost = 0,
                    parentNodeIndex = -1
                };
            }
        }
    }
    */

    // This is the new asynchronous version of CreateGrid.
    private IEnumerator CreateGridAsync()
    {
        IsGridReady = false;

        grid = new Node[gridSizeX, gridSizeY];
        Vector2 worldBottomLeft = (Vector2)transform.position - Vector2.right * gridWorldSize.x / 2 - Vector2.up * gridWorldSize.y / 2;

        if (pathNodeGrid.IsCreated) { pathNodeGrid.Dispose(); }
        pathNodeGrid = new NativeArray<PathNode>(gridSizeX * gridSizeY, Allocator.Persistent);

        int nodesProcessedThisFrame = 0;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector2 worldPoint = worldBottomLeft + Vector2.right * (x * nodeDiameter + nodeRadius) + Vector2.up * (y * nodeDiameter + nodeRadius);
                bool isWalkable = !(Physics2D.OverlapCircle(worldPoint, nodeRadius, unwalkableMask));
                int movementPenalty = 0;
                if (isWalkable)
                {
                    Collider2D[] colliders = Physics2D.OverlapPointAll(worldPoint, walkableMask);
                    foreach (Collider2D c in colliders)
                    {
                        if (walkableRegionsDictionary.ContainsKey(c.gameObject.layer))
                        {
                            movementPenalty = Mathf.Max(movementPenalty, walkableRegionsDictionary[c.gameObject.layer]);
                        }
                    }
                }
                grid[x, y] = new Node(isWalkable, worldPoint, x, y, movementPenalty);
                int index = x + y * gridSizeX;
                pathNodeGrid[index] = new PathNode
                {
                    x = x,
                    y = y,
                    index = index,
                    isWalkable = isWalkable,
                    movementPenalty = movementPenalty,
                    gCost = int.MaxValue,
                    hCost = 0,
                    parentNodeIndex = -1
                };

                nodesProcessedThisFrame++;
                // Check if we've hit our batch limit for this frame.
                if (nodesProcessedThisFrame >= nodesPerFrame)
                {
                    // Reset the counter and wait for the next frame.
                    nodesProcessedThisFrame = 0;
                    yield return null; // This is what prevents the game from freezing
                }
            }
        }

        // The loops have finished, the grid is fully built.
        Debug.Log("Grid creation complete!");
        IsGridReady = true;
    }

    // Helper function to get a node from a world position
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        // Use Vector2 for calculations
        float percentX = (worldPosition.x - transform.position.x + gridWorldSize.x / 2) / gridWorldSize.x;
        float percentY = (worldPosition.y - transform.position.y + gridWorldSize.y / 2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return grid[x, y];
    }

    // Helper function to get all neighbors of a given node (no changes needed here)
    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbours;
    }
    // This provides controlled, read-only access to our grid data.
    public Node GetNode(int x, int y)
    {
        // Safety check to prevent errors
        if (x >= 0 && x < gridSizeX && y >= 0 && y < gridSizeY)
        {
            return grid[x, y];
        }
        Debug.LogWarning($"Request for node outside of grid bounds: ({x},{y})");
        return null;
    }
    /// Updates the walkability information for a specific region of the grid.
    /// <param name="worldBounds">The world-space bounds of the area to update.</param>
    public void UpdateGridRegion(Bounds worldBounds)
    {
        if (!IsGridReady)
        {
            Debug.LogWarning("Grid not ready, cannot update region yet.");
            return;
        }

        // Convert the world bounds into grid coordinates
        Node bottomLeft = NodeFromWorldPoint(worldBounds.min);
        Node topRight = NodeFromWorldPoint(worldBounds.max);

        // Loop through only the nodes within the specified bounds
        for (int x = bottomLeft.gridX; x <= topRight.gridX; x++)
        {
            for (int y = bottomLeft.gridY; y <= topRight.gridY; y++)
            {
                // --- This is the same core logic from CreateGrid ---
                Vector2 worldPoint = GetNode(x, y).worldPosition;
                bool isWalkable = !(Physics2D.OverlapCircle(worldPoint, nodeRadius, unwalkableMask));
                int movementPenalty = 0;
                if (isWalkable)
                {
                    Collider2D[] colliders = Physics2D.OverlapPointAll(worldPoint, walkableMask);
                    foreach (Collider2D c in colliders)
                    {
                        if (walkableRegionsDictionary.ContainsKey(c.gameObject.layer))
                        {
                            movementPenalty = Mathf.Max(movementPenalty, walkableRegionsDictionary[c.gameObject.layer]);
                        }
                    }
                }

                // Update BOTH grid representations
                // 1. Update the managed grid
                grid[x, y].isWalkable = isWalkable;
                grid[x, y].movementPenalty = movementPenalty;

                // 2. Update the native grid for the Job System
                int index = x + y * gridSizeX;
                PathNode updatedNode = pathNodeGrid[index];
                updatedNode.isWalkable = isWalkable;
                updatedNode.movementPenalty = movementPenalty;
                pathNodeGrid[index] = updatedNode;
            }
        }
    }

    // Draw a visual representation of the grid in the Scene view
    void OnDrawGizmos()
    {

        // Draw the boundary of the grid
        Gizmos.DrawWireCube(transform.position, new Vector2(gridWorldSize.x, gridWorldSize.y));
        if (sceneGridGizmo) 
        {
            if (grid != null)
            {
                foreach (Node n in grid)
                {
                    if (!n.isWalkable) { Gizmos.color = Color.red; }
                    else
                    {
                        // Walkable nodes are colored based on movement penalty
                        Gizmos.color = Color.Lerp(Color.white, Color.black, n.movementPenalty / 10f); // Adjust 10f to your max penalty
                    }

                    // Draw a small cube at each node's position. We use Vector3 for Gizmos.
                    Gizmos.DrawCube(new Vector3(n.worldPosition.x, n.worldPosition.y, 0), Vector3.one * (nodeDiameter - 0.1f));
                }
            }
        }

    }
}

// Helper class definition
[System.Serializable]
public class TerrainType
{
    public LayerMask terrainMask;
    public int terrainPenalty;
}