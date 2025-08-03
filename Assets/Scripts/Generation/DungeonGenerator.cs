using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class SpawnMapping
{
    public string marker;
    public SpawnTableSO spawnTable;
}
[System.Serializable]
public class TileMapping
{
    public string marker;
    public GenerationType generationType;
    public TileTableSO tileTable;
    public PrefabTableSO prefabTable;
}
public enum GenerationType { Tile, Prefab }
public class DungeonGenerator : MonoBehaviour
{
    [Header("Tilemaps & Tiles")]
    [SerializeField] private Tilemap floorTilemap;
    //[SerializeField] private Tilemap wallTilemap;
    [SerializeField] private TileBase floorTile;
    //[SerializeField] private GameObject wallPrefab;
    //[SerializeField] private TileBase wallTile;
    [SerializeField] private TileBase doorTile;

    [Header("Tile & Object Mappings")]
    [SerializeField] private List<TileMapping> tileMappings;

    [Header("Room Templates")]
    [SerializeField] private RoomTemplateSO startRoomTemplate;
    [SerializeField] private List<RoomTemplateSO> normalRoomTemplates;
    // [SerializeField] private RoomTemplateSO bossRoomTemplate;
    // [SerializeField] private List<RoomTemplateSO> itemRoomTemplates;

    [Header("Room Dimensions")]
    [SerializeField] private int roomWidth = 27;
    [SerializeField] private int roomHeight = 17;
    [SerializeField] private int doorWidth = 4;
    [SerializeField] private int doorHeight = 3;

    [Header("Generation Settings")]
    [SerializeField] private int targetRoomCount = 15;

    [Header("Spawning")]
    [SerializeField] private List<SpawnMapping> spawnMappings;
    private Dictionary<string, TileMapping> tileMappingTable;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform entityParent; 

    private HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
    private Vector2Int startRoomPosition;
    private Vector2Int bossRoomPosition;
    private Vector2Int[] walkDirections = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    private Dictionary<string, SpawnTableSO> spawnTable; // For fast lookups

    private void Awake()
    {
        spawnTable = new Dictionary<string, SpawnTableSO>();
        foreach (var mapping in spawnMappings)
        {
            spawnTable[mapping.marker] = mapping.spawnTable;
        }
        tileMappingTable = new Dictionary<string, TileMapping>();
        foreach (var mapping in tileMappings)
        {
            tileMappingTable[mapping.marker] = mapping;
        }
    }
    private void Start()
    {
        GenerateDungeon();
    }
    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        for (int i = entityParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(entityParent.GetChild(i).gameObject);
        }
        roomPositions.Clear();
        floorTilemap.ClearAllTiles();
        //wallTilemap.ClearAllTiles();

        GenerateBlueprint();
        DetermineSpecialRooms();
        DrawDungeon();
        SpawnEntities();

        Debug.Log("Dungeon generation complete!");
    }

    private void GenerateBlueprint()
    {
        Vector2Int currentPosition = Vector2Int.zero;
        roomPositions.Add(currentPosition);

        while (roomPositions.Count < targetRoomCount)
        {
            Vector2Int direction = walkDirections[UnityEngine.Random.Range(0, walkDirections.Length)];
            currentPosition += direction;
            roomPositions.Add(currentPosition);
        }
    }

    private void DetermineSpecialRooms()
    {
        startRoomPosition = Vector2Int.zero;
        bossRoomPosition = roomPositions.OrderByDescending(pos => Vector2.Distance(startRoomPosition, pos)).First();
    }

    private void DrawDungeon()
    {
        foreach (Vector2Int roomPos in roomPositions)
        {
            RoomTemplateSO roomTemplate = GetTemplateForRoom(roomPos);
            Vector2Int drawOffset = new Vector2Int(roomPos.x * roomWidth, roomPos.y * roomHeight);
            DrawRoom(roomTemplate, drawOffset);
        }
    }

    private void DrawRoom(RoomTemplateSO template, Vector2Int offset)
    {
        string[] rows = template.layout.Split('\n');
        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y].Trim();
            for (int x = 0; x < row.Length; x++)
            {
                string markerString = row[x].ToString();

                if (tileMappingTable.ContainsKey(markerString))
                {
                    TileMapping mapping = tileMappingTable[markerString];
                    Vector3Int tilePos = new Vector3Int(offset.x + x, offset.y + (rows.Length - 1 - y), 0);

                    bool isDoor = IsDoorwayPosition(tilePos, offset);

                    if (mapping.generationType == GenerationType.Tile)
                    {
                        TileBase tile = mapping.tileTable.GetRandomItem();
                        if (tile != null)
                        {
                            floorTilemap.SetTile(tilePos, tile);
                        }
                    }
                    else if (mapping.generationType == GenerationType.Prefab && !isDoor)
                    {
                        GameObject prefab = mapping.prefabTable.GetRandomItem();
                        if (prefab != null)
                        {
                            Vector2 spawnPos = new Vector2(tilePos.x + 0.5f, tilePos.y + 0.5f);
                            Instantiate(prefab, spawnPos, Quaternion.identity, entityParent);
                        }
                    }
                }
                else 
                {
                    Vector3Int tilePos = new Vector3Int(offset.x + x, offset.y + (rows.Length - 1 - y), 0);
                    TileMapping floorMapping = tileMappingTable["."];
                    if (floorMapping != null && floorMapping.tileTable != null)
                    {
                        floorTilemap.SetTile(tilePos, floorMapping.tileTable.GetRandomItem());
                    }
                }
            }
        }
        PlaceDoors(offset);
    }

    private void PlaceDoors(Vector2Int roomOffset)
    {
        Vector2Int roomPos = new Vector2Int(roomOffset.x / roomWidth, roomOffset.y / roomHeight);

        Action<Vector3Int> carveDoorArea = startPos =>
        {
            for (int x = 0; x < doorWidth; x++)
            {
                for (int y = 0; y < doorHeight; y++)
                {
                    floorTilemap.SetTile(new Vector3Int(startPos.x + x, startPos.y + y, 0), doorTile);
                }
            }
        };

        int doorX_Start = roomOffset.x + (roomWidth / 2) - (doorWidth / 2);
        int doorY_Start = roomOffset.y + (roomHeight / 2) - (doorHeight / 2);

        // Check North
        if (roomPositions.Contains(roomPos + Vector2Int.up))
        {
            Vector3Int doorStartPos = new Vector3Int(doorX_Start, roomOffset.y + roomHeight - doorHeight, 0);
            carveDoorArea(doorStartPos);
        }
        // Check South
        if (roomPositions.Contains(roomPos + Vector2Int.down))
        {
            Vector3Int doorStartPos = new Vector3Int(doorX_Start, roomOffset.y, 0);
            carveDoorArea(doorStartPos);
        }
        // Check East
        if (roomPositions.Contains(roomPos + Vector2Int.right))
        {
            Vector3Int doorStartPos = new Vector3Int(roomOffset.x + roomWidth - doorWidth, doorY_Start, 0);
            for (int x = 0; x < doorWidth; x++)
            {
                for (int y = 0; y < doorHeight; y++)
                {
                    floorTilemap.SetTile(new Vector3Int(doorStartPos.x + x, doorStartPos.y + y, 0), doorTile);
                }
            }
        }
        // Check West
        if (roomPositions.Contains(roomPos + Vector2Int.left))
        {
            Vector3Int doorStartPos = new Vector3Int(roomOffset.x, doorY_Start, 0);
            // Carve the sideways door.
            for (int x = 0; x < doorWidth; x++)
            {
                for (int y = 0; y < doorHeight; y++)
                {
                    floorTilemap.SetTile(new Vector3Int(doorStartPos.x + x, doorStartPos.y + y, 0), doorTile);
                }
            }
        }
    }

    private bool IsDoorwayPosition(Vector3Int tilePos, Vector2Int roomOffset)
    {
        Vector2Int roomPos = new Vector2Int(roomOffset.x / roomWidth, roomOffset.y / roomHeight);

        // Calculate the boundary boxes for each potential door.
        int doorX_Start = roomOffset.x + (roomWidth / 2) - (doorWidth / 2);
        int doorY_Start = roomOffset.y + (roomHeight / 2) - (doorHeight / 2);

        // Check North Door Area
        if (roomPositions.Contains(roomPos + Vector2Int.up))
        {
            RectInt doorRect = new RectInt(doorX_Start, roomOffset.y + roomHeight - doorHeight, doorWidth, doorHeight);
            if (doorRect.Contains(new Vector2Int(tilePos.x, tilePos.y))) return true;
        }
        // Check South Door Area
        if (roomPositions.Contains(roomPos + Vector2Int.down))
        {
            RectInt doorRect = new RectInt(doorX_Start, roomOffset.y, doorWidth, doorHeight);
            if (doorRect.Contains(new Vector2Int(tilePos.x, tilePos.y))) return true;
        }
        // Check East Door Area
        if (roomPositions.Contains(roomPos + Vector2Int.right))
        {
            RectInt doorRect = new RectInt(roomOffset.x + roomWidth - doorWidth, doorY_Start, doorWidth, doorHeight);
            if (doorRect.Contains(new Vector2Int(tilePos.x, tilePos.y))) return true;
        }
        // Check West Door Area
        if (roomPositions.Contains(roomPos + Vector2Int.left))
        {
            RectInt doorRect = new RectInt(roomOffset.x, doorY_Start, doorWidth, doorHeight);
            if (doorRect.Contains(new Vector2Int(tilePos.x, tilePos.y))) return true;
        }

        return false;
    }
    private void SpawnEntities()
    {
        foreach (Vector2Int roomPos in roomPositions)
        {
            RoomTemplateSO roomTemplate = GetTemplateForRoom(roomPos);
            Vector2Int drawOffset = new Vector2Int(roomPos.x * roomWidth, roomPos.y * roomHeight);
            ProcessRoomSpawns(roomTemplate, drawOffset);
        }
    }

    private void ProcessRoomSpawns(RoomTemplateSO template, Vector2Int offset)
    {
        string[] rows = template.layout.Split('\n');
        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y].Trim();
            for (int x = 0; x < row.Length; x++)
            {
                char markerChar = row[x];
                Vector2 spawnPos = new Vector2(offset.x + x + 0.5f, offset.y + (rows.Length - 1 - y) + 0.5f);

                if (markerChar == 'P' && playerPrefab != null)
                {
                    //Instantiate(playerPrefab, spawnPos, Quaternion.identity, entityParent);
                }
                else
                {
                    string markerString = markerChar.ToString();
                    if (spawnTable.ContainsKey(markerString))
                    {
                        SpawnTableSO table = spawnTable[markerString];
                        GameObject prefabToSpawn = table.GetRandomSpawnable();
                        if (prefabToSpawn != null)
                        {
                            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, entityParent);
                        }
                    }
                }
            }
        }
    }

    private RoomTemplateSO GetTemplateForRoom(Vector2Int roomPos)
    {
        if (roomPos == startRoomPosition)
        {
            return startRoomTemplate;
        }
        // else if (roomPos == bossRoomPosition) { return bossRoomTemplate; } // Future expansion
        else
        {
            return normalRoomTemplates[UnityEngine.Random.Range(0, normalRoomTemplates.Count)];
        }
    }

    private void OnDrawGizmos()
    {
        if (roomPositions == null || roomPositions.Count == 0) return;

        Vector3 gizmoSize = new Vector3(roomWidth, roomHeight, 0);
        foreach (Vector2Int position in roomPositions)
        {
            Vector3 worldPos = new Vector3(position.x * roomWidth, position.y * roomHeight, 0);
            if (position == startRoomPosition) Gizmos.color = Color.green;
            else if (position == bossRoomPosition) Gizmos.color = Color.red;
            else Gizmos.color = Color.white;
            Gizmos.DrawWireCube(worldPos, gizmoSize);
        }
    }
}