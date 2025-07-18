using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using System.Collections.Generic;
public struct PathNode
{
    public int x;
    public int y;
    public int index;

    public int gCost;
    public int hCost;
    public int fCost { get { return gCost + hCost; } }

    public bool isWalkable;
    public int movementPenalty;

    public int parentNodeIndex;
}

// A comparer to tell the Priority Queue how to sort the nodes 
// We sort by F-Cost, and if F-Costs are equal, we use H-Cost as a tie-breaker.
public struct PathNodeComparer : IComparer<int>
{
    [ReadOnly] public NativeArray<PathNode> nodeGrid;
    public int Compare(int indexA, int indexB)
    {
        int fCostA = nodeGrid[indexA].fCost;
        int fCostB = nodeGrid[indexB].fCost;

        if (fCostA != fCostB)
        {
            // Lower F-Cost has higher priority
            return fCostA.CompareTo(fCostB);
        }
        // If F-Costs are the same, prioritize the one closer to the goal (lower H-Cost)
        return nodeGrid[indexA].hCost.CompareTo(nodeGrid[indexB].hCost);
    }
}
//A data oriented design for A*Star algorithm with Burst Compiler and C# Jobs


[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct AStarJob : IJob
{
    public int2 startPosition;
    public int2 endPosition;
    public int2 gridSize;

    [ReadOnly] public NativeArray<PathNode> nodeGrid;

    public NativeList<int2> path;

    public void Execute()
    {
        NativeArray<PathNode> pathfindingGrid = new NativeArray<PathNode>(nodeGrid, Allocator.Temp);

        // Provide an initial capacity (e.g., 512) to both the open and closed sets.
        // Custom comparer to sort the queue.
        var comparer = new PathNodeComparer { nodeGrid = pathfindingGrid };

        // Custom NativeMinHeap
        var openSet = new NativeMinHeap<PathNodeComparer>(Allocator.Temp, comparer);

        NativeHashSet<int> closedSet = new NativeHashSet<int>(512, Allocator.Temp);

        int startNodeIndex = CalculateIndex(startPosition.x, startPosition.y, gridSize.x);
        int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, gridSize.x);

        PathNode startNode = pathfindingGrid[startNodeIndex];
        startNode.gCost = 0;
        startNode.hCost = CalculateDistanceCost(startPosition, endPosition);
        pathfindingGrid[startNodeIndex] = startNode;

        openSet.Enqueue(startNodeIndex);

        while (openSet.Count > 0)
        {
            int currentNodeIndex = openSet.Dequeue();

            PathNode currentNode = pathfindingGrid[currentNodeIndex];

            if (currentNodeIndex == endNodeIndex)
            {
                ReconstructPath(pathfindingGrid, currentNode);
                break;
            }           
            closedSet.Add(currentNodeIndex);

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;

                    int2 neighbourPos = new int2(currentNode.x + x, currentNode.y + y);

                    if (!IsPositionInsideGrid(neighbourPos, gridSize)) continue;

                    int neighbourNodeIndex = CalculateIndex(neighbourPos.x, neighbourPos.y, gridSize.x);

                    if (closedSet.Contains(neighbourNodeIndex)) continue;

                    PathNode neighbourNode = pathfindingGrid[neighbourNodeIndex];
                    if (!neighbourNode.isWalkable) continue;

                    // The cost now includes the movement penalty of the TILE WE ARE MOVING TO
                    int tentativeGCost = currentNode.gCost + CalculateDistanceCost(new int2(currentNode.x, currentNode.y), neighbourPos) + neighbourNode.movementPenalty;

                    if (tentativeGCost < neighbourNode.gCost)
                    {
                        neighbourNode.parentNodeIndex = currentNodeIndex;
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.hCost = CalculateDistanceCost(neighbourPos, endPosition);
                        pathfindingGrid[neighbourNodeIndex] = neighbourNode;

                        openSet.Enqueue(neighbourNodeIndex);
                    }
                }
            }
        }

        pathfindingGrid.Dispose();
        openSet.Dispose();
        closedSet.Dispose();
    }

    private void ReconstructPath(NativeArray<PathNode> grid, PathNode endNode)
    {
        if (endNode.parentNodeIndex == -1) return; // Path not found

        PathNode currentNode = endNode;
        while (currentNode.parentNodeIndex != -1)
        {
            path.Add(new int2(currentNode.x, currentNode.y));
            currentNode = grid[currentNode.parentNodeIndex];
        }
        path.Add(new int2(currentNode.x, currentNode.y));

        // Reverse the list
        for (int i = 0; i < path.Length / 2; i++)
        {
            int2 tmp = path[i];
            path[i] = path[path.Length - 1 - i];
            path[path.Length - 1 - i] = tmp;
        }
    }

    private int CalculateDistanceCost(int2 a, int2 b)
    {
        int xDistance = math.abs(a.x - b.x);
        int yDistance = math.abs(a.y - b.y);
        int remaining = math.abs(xDistance - yDistance);
        return 14 * math.min(xDistance, yDistance) + 10 * remaining;
    }

    // This needs to implement if target isnt on a walkable node
    private int GetLowestFCostNodeIndex(NativeList<int> openSet, NativeArray<PathNode> grid)
    {
        PathNode lowestCostNode = grid[openSet[0]];
        int lowestCostNodeIndexInList = 0;

        for (int i = 1; i < openSet.Length; i++)
        {
            PathNode testNode = grid[openSet[i]];
            if (testNode.fCost < lowestCostNode.fCost)
            {
                lowestCostNode = testNode;
                lowestCostNodeIndexInList = i;
            }
        }
        return openSet[lowestCostNodeIndexInList];
    }

    private bool IsPositionInsideGrid(int2 gridPosition, int2 gridSize)
    {
        return gridPosition.x >= 0 && gridPosition.y >= 0 &&
               gridPosition.x < gridSize.x && gridPosition.y < gridSize.y;
    }

    private int CalculateIndex(int x, int y, int gridWidth)
    {
        return x + y * gridWidth;
    }
}