using UnityEngine;

public class Node
{
    public bool isWalkable;
    public Vector2 worldPosition; // Changed to Vector2
    public int gridX;
    public int gridY;

    public int movementPenalty;

    // A* specific values
    public int gCost;
    public int hCost;
    public Node parent;

    public int fCost
    {
        get { return gCost + hCost; }
    }

    public Node(bool _isWalkable, Vector2 _worldPosition, int _gridX, int _gridY, int _penalty)
    {
        isWalkable = _isWalkable;
        worldPosition = _worldPosition;
        gridX = _gridX;
        gridY = _gridY;
        movementPenalty = _penalty;
    }
}