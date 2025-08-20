using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;
using TMPro;

public class LevelGeneratorLogic : MonoBehaviour
{
    [Header("Tilemap Settings")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase[] floorTiles;
    [SerializeField] private float[] floorTileWeights;
    
    [Header("Wall Tiles")]
    [SerializeField] private TileBase wallTopLeft;
    [SerializeField] private TileBase wallTop;
    [SerializeField] private TileBase wallTopRight;
    [SerializeField] private TileBase wallLeft;
    [SerializeField] private TileBase wallRight;
    [SerializeField] private TileBase wallBottomLeft;
    [SerializeField] private TileBase wallBottom;
    [SerializeField] private TileBase wallBottomRight;
    [Header("Corridor Wall Tiles")]
    [SerializeField] private TileBase corridorWallLeft;
    [SerializeField] private TileBase corridorWallRight;
    [SerializeField] private TileBase corridorWallTop;
    [SerializeField] private TileBase corridorWallBottom;
    
    [Header("Default Level Settings")]
    [SerializeField] int defaultLevelWidth = 25;
    [SerializeField] int defaultLevelHeight = 25;
    [SerializeField] int defaultMaxIterations = 5;
    //[SerializeField] int defaultMinLeafSize = 8;
    [SerializeField] int defaultMinAllowedLeafSize = 8;
    [SerializeField] int minAllowedLeafSize = 6; // Don't go below this as that can brake level generation
    [SerializeField] int defaultCorridorWidth = 1;
    [Header("Progressive Level Settings")]
    [SerializeField] int progressiveLevelWidth = 5;
    [SerializeField] int progressiveLevelHeight = 5;
    [SerializeField] int progressiveMaxIterations = 2;
    [SerializeField] int progressiveMinLeafSize = 8;
    //Tooltip("Minimum allowed size for room leaves. Prevents rooms from becoming too small.")]
    //[SerializeField] int progressiveCorridorWidth = 1;
    
    private int levelWidth;
    private int levelHeight;
    private int maxIterations;
    private int minLeafSize;
    private int corridorWidth;
    
    private int currentLevel = 1; // starts at 1 as it is a player-facing value
    [SerializeField] TextMeshProUGUI levelNumberText;
    [SerializeField] private int maxLevel = 10;
    
    [Header("Debug Options")]
    [SerializeField] bool stepByStepMode = false;
    [SerializeField] float debugStepDelay = 0.5f;
    [SerializeField] bool generateMap = true;

    private HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();
    private List<Leaf> leaves = new List<Leaf>();
    private List<Rect> createdRooms = new List<Rect>();
    
    [Header("Enemy AI Agent Settings")]
    [SerializeField] GameObject enemyPrefab;
    private List<Vector3> enemySpawnPositions = new List<Vector3>();
    [SerializeField] int currentNumberOfEnemies = 1;
    [SerializeField] int defaultEnemiesPerRoom = 1;
    private int currentEnemiesPerRoom = 1;
    [SerializeField] int progressiveEnemiesPerRoom = 1;
    [SerializeField] int maxEnemiesPerRoom = 10;
    [SerializeField] TextMeshProUGUI currentNumberOfEnemiesText;
    [SerializeField] private float minDistanceBetweenEnemies = 1.5f;
    [SerializeField] private Tilemap groundTilemap; // Called ground in case I separate walls to another tilemap
    [SerializeField] private TileBase[] allowedPatrolTiles;
    [SerializeField] private TileBase[] allowedPathfindingTiles;


    [Header("Containers")] // These exist to make it easier to debug the game during runtme
    [SerializeField] Transform spawnedEnemiesContainer;
    [SerializeField] Transform spawnedBulletsContainer;
    [SerializeField] Transform spawnedLootContainer;
    
    
    // events the level generator has
    public event Action OnLevelGenerated;
    public event Action LevelCleared;
    public event Action OnAllEnemiesKilled;
    
    private void OnEnable()
    {
        EnemyHealthLogic.OnOneEnemyKilled += HandleEnemyKill;
    }

    private void OnDisable()
    {
        EnemyHealthLogic.OnOneEnemyKilled -= HandleEnemyKill;
    }

    private void HandleEnemyKill(GameObject deadEnemy)
    {
        Debug.Log("Enemy killed!");
        currentNumberOfEnemies--;
        currentNumberOfEnemiesText.text = $"Enemy count: {currentNumberOfEnemies}";

        if (currentNumberOfEnemies == 0) // Level won
        {
            OnAllEnemiesKilled?.Invoke();
        }
    }

    public void Awake()
    {
        levelWidth = defaultLevelWidth;
        levelHeight = defaultLevelHeight;
        maxIterations = defaultMaxIterations;
        minLeafSize = defaultMinAllowedLeafSize;
        corridorWidth = defaultCorridorWidth;
        
        levelNumberText.text = $"Level {currentLevel}";
    }

    public void GenerateLevel()
    {
        if (floorTileWeights.Length != floorTiles.Length)
        {
            Debug.LogError("floorTileWeights.Length != floorTiles.Length");
            return;
        }
        
        ResetLevelData(); // Clear old data before generating new level
        
        if (stepByStepMode && generateMap)
            StartCoroutine(GenerateDungeonStepByStep());
        else if(generateMap)
            GenerateDungeon();
    }
    
    void GenerateDungeon()
    {
        Leaf root = new Leaf(0, 0, levelWidth, levelHeight);
        leaves.Add(root);

        for (int i = 0; i < maxIterations; i++)
        {
            List<Leaf> newLeaves = new List<Leaf>();
            foreach (Leaf leaf in leaves)
            {
                if (leaf.left == null && leaf.right == null)
                {
                    if (leaf.width >= 2 * minLeafSize || leaf.height >= 2 * minLeafSize)
                    {
                        if (leaf.Split(minLeafSize))
                        {
                            newLeaves.Add(leaf.left);
                            newLeaves.Add(leaf.right);
                        }
                    }
                }
            }
            leaves.AddRange(newLeaves);
        }

        root.CreateRooms(createdRooms);
        root.CreateCorridors(this);

        foreach (Leaf leaf in leaves)
        {
            if (leaf.room != Rect.zero)
            {
                DrawRoom(leaf.room);
                SpawnRoomTiles(leaf.room);
            }
        }

        SpawnWalls();

        AddEnemySpawnPositions();

        OnLevelGenerated?.Invoke();
    }

    private void AddEnemySpawnPositions()
    {
        enemySpawnPositions.Clear();

        for (int i = 1; i < createdRooms.Count; i++) // Skip the first room as player is in it
        {
            Rect room = createdRooms[i];

            // Skip tiny rooms
            if (room.width < 3 || room.height < 3)
            {
                Debug.LogWarning($"Room {i} is too small for enemy placement.");
                continue;
            }

            List<Vector3> roomSpawns = new List<Vector3>();
            int attempts = 0;
            int maxAttempts = 10;

            while (roomSpawns.Count < currentEnemiesPerRoom && attempts < maxAttempts)
            {
                float randX = Random.Range(room.x + 1, room.xMax - 1);
                float randY = Random.Range(room.y + 1, room.yMax - 1);
                Vector3 candidate = new Vector3(randX, randY, 0f);

                bool tooClose = false;
                foreach (Vector3 existing in roomSpawns)
                {
                    if (Vector3.Distance(existing, candidate) < minDistanceBetweenEnemies)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    roomSpawns.Add(candidate);
                    enemySpawnPositions.Add(candidate);
                }

                attempts++;
                
                Vector3 offset = new Vector3(0.3f, 0.3f, 0f);
                Debug.DrawLine(candidate - offset, candidate + offset, Color.yellow, 10f);
                Debug.DrawLine(new Vector3(candidate.x - offset.x, candidate.y + offset.y, candidate.z),
                    new Vector3(candidate.x + offset.x, candidate.y - offset.y, candidate.z),
                    Color.yellow, 10f);

            }

            if (roomSpawns.Count < currentEnemiesPerRoom)
            {
                Debug.LogWarning($"Only placed {roomSpawns.Count}/{currentEnemiesPerRoom} enemies in room {i} after {attempts} attempts.");
            }
        }
    }



    IEnumerator GenerateDungeonStepByStep()
    {
        Leaf root = new Leaf(0, 0, levelWidth, levelHeight);
        leaves.Add(root);

        for (int i = 0; i < maxIterations; i++)
        {
            List<Leaf> newLeaves = new List<Leaf>();
            foreach (Leaf leaf in leaves)
            {
                if (leaf.left == null && leaf.right == null)
                {
                    if (leaf.width >= 2 * minLeafSize || leaf.height >= 2 * minLeafSize)
                    {
                        if (leaf.Split(minLeafSize))
                        {
                            newLeaves.Add(leaf.left);
                            newLeaves.Add(leaf.right);

                            DrawLeafBounds(leaf.left, Color.cyan);
                            DrawLeafBounds(leaf.right, Color.magenta);
                            yield return new WaitForSeconds(debugStepDelay);
                        }
                    }
                }
            }
            leaves.AddRange(newLeaves);
        }

        root.CreateRooms(createdRooms);
        foreach (Leaf leaf in leaves)
        {
            if (leaf.room != Rect.zero)
            {
                DrawRoom(leaf.room);
                yield return new WaitForSeconds(debugStepDelay);
                SpawnRoomTiles(leaf.room);
            }
        }

        root.CreateCorridors(this);
        yield return new WaitForSeconds(debugStepDelay);

        SpawnWalls();
        
        AddEnemySpawnPositions();
        
        OnLevelGenerated?.Invoke();
    }

    void SpawnRoomTiles(Rect room)
    {
        for (int x = (int)room.x; x < (int)room.xMax; x++)
        {
            for (int y = (int)room.y; y < (int)room.yMax; y++)
            {
                Vector3Int tilePos = new Vector3Int(x, y, 0);
                tilemap.SetTile(tilePos, GetRandomFloorTile());
                floorPositions.Add(new Vector2Int(x, y));
            }
        }
    }

    private TileBase GetRandomFloorTile()
    {
        if (floorTiles == null || floorTiles.Length == 0)
        {
            Debug.LogWarning("No floor tiles assigned!");
            return null;
        }

        if (floorTileWeights == null || floorTileWeights.Length != floorTiles.Length)
        {
            Debug.LogWarning("Weights are not properly assigned or mismatched with floorTiles.");
            return floorTiles[Random.Range(0, floorTiles.Length)];
        }

        float totalWeight = 0f;
        for (int i = 0; i < floorTileWeights.Length; i++)
            totalWeight += floorTileWeights[i];

        float randomValue = Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        for (int i = 0; i < floorTileWeights.Length; i++)
        {
            cumulativeWeight += floorTileWeights[i];
            if (randomValue <= cumulativeWeight)
                return floorTiles[i];
        }

        return floorTiles[floorTiles.Length - 1]; // Fallback
    }


    public void SpawnCorridorTiles(Vector2 start, Vector2 end)
    {
        int halfWidth = corridorWidth / 2;
        Vector2Int startInt = Vector2Int.RoundToInt(start);
        Vector2Int endInt = Vector2Int.RoundToInt(end);

        // L-shaped corridor: horizontal then vertical
        Vector2Int corner = new Vector2Int(endInt.x, startInt.y);

        // Horizontal segment
        int xStart = Mathf.Min(startInt.x, corner.x);
        int xEnd = Mathf.Max(startInt.x, corner.x);
        for (int x = xStart; x <= xEnd; x++)
        {
            for (int offset = -halfWidth; offset <= halfWidth; offset++)
            {
                Vector2Int pos = new Vector2Int(x, corner.y + offset);
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), GetRandomFloorTile());
                floorPositions.Add(pos);
            }
        }

        // Vertical segment
        int yStart = Mathf.Min(corner.y, endInt.y);
        int yEnd = Mathf.Max(corner.y, endInt.y);
        for (int y = yStart; y <= yEnd; y++)
        {
            for (int offset = -halfWidth; offset <= halfWidth; offset++)
            {
                Vector2Int pos = new Vector2Int(endInt.x + offset, y);
                tilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), GetRandomFloorTile());
                floorPositions.Add(pos);
            }
        }
    }


    void DrawRoom(Rect room)
    {
        Color color = Color.green;

        if (room == createdRooms[0])
            color = Color.yellow;
        else if (room == createdRooms[createdRooms.Count - 1])
            color = Color.red;

        Vector3 topLeft = new Vector3(room.x, room.yMax, 0);
        Vector3 topRight = new Vector3(room.xMax, room.yMax, 0);
        Vector3 bottomLeft = new Vector3(room.x, room.y, 0);
        Vector3 bottomRight = new Vector3(room.xMax, room.y, 0);

        Debug.DrawLine(topLeft, topRight, color, 1f);
        Debug.DrawLine(topRight, bottomRight, color, 1f);
        Debug.DrawLine(bottomRight, bottomLeft, color, 1f);
        Debug.DrawLine(bottomLeft, topLeft, color, 1f);
    }

    void DrawLeafBounds(Leaf leaf, Color color)
    {
        Vector3 topLeft = new Vector3(leaf.x, leaf.y + leaf.height, 0);
        Vector3 topRight = new Vector3(leaf.x + leaf.width, leaf.y + leaf.height, 0);
        Vector3 bottomLeft = new Vector3(leaf.x, leaf.y, 0);
        Vector3 bottomRight = new Vector3(leaf.x + leaf.width, leaf.y, 0);

        Debug.DrawLine(topLeft, topRight, color, 1f);
        Debug.DrawLine(topRight, bottomRight, color, 1f);
        Debug.DrawLine(bottomRight, bottomLeft, color, 1f);
        Debug.DrawLine(bottomLeft, topLeft, color, 1f);
    }

    void SpawnWalls()
    {
        foreach (Vector2Int pos in floorPositions)
        {
            Vector2Int left = pos + Vector2Int.left;
            Vector2Int right = pos + Vector2Int.right;
            Vector2Int up = pos + Vector2Int.up;
            Vector2Int down = pos + Vector2Int.down;

            Vector3Int wallPos;

            // Left Wall
            if (!floorPositions.Contains(left) && floorPositions.Contains(right))
            {
                wallPos = new Vector3Int(left.x, left.y, 0);
                if (!wallPositions.Contains(left))
                {
                    tilemap.SetTile(wallPos, wallLeft);
                    wallPositions.Add(left);
                }
            }

            // Right Wall
            if (!floorPositions.Contains(right) && floorPositions.Contains(left))
            {
                wallPos = new Vector3Int(right.x, right.y, 0);
                if (!wallPositions.Contains(right))
                {
                    tilemap.SetTile(wallPos, wallRight);
                    wallPositions.Add(right);
                }
            }

            // Top Wall
            if (!floorPositions.Contains(up) && floorPositions.Contains(down))
            {
                wallPos = new Vector3Int(up.x, up.y, 0);
                if (!wallPositions.Contains(up))
                {
                    tilemap.SetTile(wallPos, wallTop);
                    wallPositions.Add(up);
                }
            }

            // Bottom Wall
            if (!floorPositions.Contains(down) && floorPositions.Contains(up))
            {
                wallPos = new Vector3Int(down.x, down.y, 0);
                if (!wallPositions.Contains(down))
                {
                    tilemap.SetTile(wallPos, wallBottom);
                    wallPositions.Add(down);
                }
            }
            
            // Top Left Corner
            if (!floorPositions.Contains(up) && !floorPositions.Contains(left) &&
                floorPositions.Contains(down) && floorPositions.Contains(right))
            {
                Vector2Int cornerPos = pos + Vector2Int.up + Vector2Int.left;
                if (!wallPositions.Contains(cornerPos))
                {
                    tilemap.SetTile(new Vector3Int(cornerPos.x, cornerPos.y, 0), wallTopLeft);
                    wallPositions.Add(cornerPos);
                }
            }

            // Top Right Corner
            if (!floorPositions.Contains(up) && !floorPositions.Contains(right) &&
                floorPositions.Contains(down) && floorPositions.Contains(left))
            {
                Vector2Int cornerPos = pos + Vector2Int.up + Vector2Int.right;
                if (!wallPositions.Contains(cornerPos))
                {
                    tilemap.SetTile(new Vector3Int(cornerPos.x, cornerPos.y, 0), wallTopRight);
                    wallPositions.Add(cornerPos);
                }
            }

            // Bottom Left Corner
            if (!floorPositions.Contains(down) && !floorPositions.Contains(left) &&
                floorPositions.Contains(up) && floorPositions.Contains(right))
            {
                Vector2Int cornerPos = pos + Vector2Int.down + Vector2Int.left;
                if (!wallPositions.Contains(cornerPos))
                {
                    tilemap.SetTile(new Vector3Int(cornerPos.x, cornerPos.y, 0), wallBottomLeft);
                    wallPositions.Add(cornerPos);
                }
            }

            // Bottom Right Corner
            if (!floorPositions.Contains(down) && !floorPositions.Contains(right) &&
                floorPositions.Contains(up) && floorPositions.Contains(left))
            {
                Vector2Int cornerPos = pos + Vector2Int.down + Vector2Int.right;
                if (!wallPositions.Contains(cornerPos))
                {
                    tilemap.SetTile(new Vector3Int(cornerPos.x, cornerPos.y, 0), wallBottomRight);
                    wallPositions.Add(cornerPos);
                }
            }

        }
        
        // Corridor Wall Logic
        foreach (Vector2Int pos in floorPositions)
        {
            Vector2Int left = pos + Vector2Int.left;
            Vector2Int right = pos + Vector2Int.right;
            Vector2Int up = pos + Vector2Int.up;
            Vector2Int down = pos + Vector2Int.down;

            // Horizontal corridor: no floor above or below
            if (!floorPositions.Contains(up) && !floorPositions.Contains(down))
            {
                Vector2Int topWall = up;
                Vector2Int bottomWall = down;

                if (!wallPositions.Contains(topWall))
                {
                    tilemap.SetTile(new Vector3Int(topWall.x, topWall.y, 0), corridorWallTop);
                    wallPositions.Add(topWall);
                }
                if (!wallPositions.Contains(bottomWall))
                {
                    tilemap.SetTile(new Vector3Int(bottomWall.x, bottomWall.y, 0), corridorWallBottom);
                    wallPositions.Add(bottomWall);
                }
            }

            // Vertical corridor: no floor left or right
            if (!floorPositions.Contains(left) && !floorPositions.Contains(right))
            {
                Vector2Int leftWall = left;
                Vector2Int rightWall = right;

                if (!wallPositions.Contains(leftWall))
                {
                    tilemap.SetTile(new Vector3Int(leftWall.x, leftWall.y, 0), corridorWallLeft);
                    wallPositions.Add(leftWall);
                }
                if (!wallPositions.Contains(rightWall))
                {
                    tilemap.SetTile(new Vector3Int(rightWall.x, rightWall.y, 0), corridorWallRight);
                    wallPositions.Add(rightWall);
                }
            }
        }
    }

    public class Leaf
    {
        public int x, y, width, height;
        public Leaf left, right;
        public Rect room = Rect.zero;

        public Leaf(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public bool Split(int minLeafSize)
        {
            if (left != null || right != null)
                return false;

            bool splitH = Random.value > 0.5f;

            if (width > height && (float)width / height >= 1.25f)
                splitH = false;
            else if (height > width && (float)height / width >= 1.25f)
                splitH = true;

            int max = (splitH ? height : width) - minLeafSize;
            if (max <= minLeafSize)
                return false;

            int split = Random.Range(minLeafSize, max);

            if (splitH)
            {
                left = new Leaf(x, y, width, split);
                right = new Leaf(x, y + split, width, height - split);
            }
            else
            {
                left = new Leaf(x, y, split, height);
                right = new Leaf(x + split, y, width - split, height);
            }
            return true;
        }

        public void CreateRooms(List<Rect> createdRooms)
        {
            if (left != null || right != null)
            {
                left?.CreateRooms(createdRooms);
                right?.CreateRooms(createdRooms);
            }
            else
            {
                int roomWidth = Random.Range(width / 2, width - 2);
                int roomHeight = Random.Range(height / 2, height - 2);
                int roomX = x + Random.Range(1, width - roomWidth - 1);
                int roomY = y + Random.Range(1, height - roomHeight - 1);
                room = new Rect(roomX, roomY, roomWidth, roomHeight);
                createdRooms.Add(room);
            }
        }

        public Vector2 GetRoomCenter()
        {
            if (room != Rect.zero)
            {
                return new Vector2(room.x + room.width / 2f, room.y + room.height / 2f);
            }
            else
            {
                return left?.GetRoomCenter() ?? right?.GetRoomCenter() ?? Vector2.zero;
            }
        }

        public void CreateCorridors(LevelGeneratorLogic generator)
        {
            if (left != null && right != null)
            {
                Vector2 leftCenter = left.GetRoomCenter();
                Vector2 rightCenter = right.GetRoomCenter();
                Vector2 intermediate = new Vector2(rightCenter.x, leftCenter.y);

                generator.SpawnCorridorTiles(leftCenter, intermediate);
                generator.SpawnCorridorTiles(intermediate, rightCenter);
            }

            left?.CreateCorridors(generator);
            right?.CreateCorridors(generator);
        }
    }
    
    // HELPER FUNCTIONS
    
    public Vector3 GetFirstRoomCenter() // Player always starts here
    {
        if (createdRooms.Count > 0)
        {
            Vector2 center = createdRooms[0].center;
            return new Vector3(center.x, center.y, 0f);
        }
        return Vector3.zero;
    }
    
    public void SpawnEnemiesInRooms()
    {
        if (!enemyPrefab || !spawnedEnemiesContainer)
        {
            Debug.LogError("Missing enemy prefab or container.");
            return;
        }

        foreach (Vector3 pos in enemySpawnPositions)
        {
            GameObject enemyInstance = Instantiate(enemyPrefab, pos, Quaternion.identity, spawnedEnemiesContainer);
            currentNumberOfEnemies++;

            // Assign patrol tilemap and allowed tiles
            VectorMove vm = enemyInstance.GetComponent<VectorMove>();
            if (vm != null)
            {
                vm.SetPatrolTilemap(groundTilemap, allowedPatrolTiles);
                vm.SetPathfindingTilemap(groundTilemap, allowedPathfindingTiles); // Same as patrol tiles for now, not sure if it will ever change
            }
        }

        currentNumberOfEnemiesText.text = $"Enemy count: {currentNumberOfEnemies}";
    }

    public void ClearLevel()
    {
        ClearLevelTilemap();
        ClearSpawnedObjects();
        StartCoroutine(WaitAndInvokeLevelCleared());
        
        currentNumberOfEnemies = 0;
        currentNumberOfEnemiesText.text = $"Enemy count: {currentNumberOfEnemies}";
    }

    private IEnumerator WaitAndInvokeLevelCleared()
    {
        // Wait until all enemies are destroyed
        while (spawnedEnemiesContainer.childCount > 0)
        {
            yield return null;
        }

        // Wait until tilemap is visually and logically cleared
        while (tilemap.GetUsedTilesCount() > 0)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.5f); // Buffer as I need time to clear the level
        LevelCleared?.Invoke();
    }

    private void ResetLevelData() // Cleanup after level is finished
    {
        floorPositions.Clear();
        wallPositions.Clear();
        leaves.Clear();
        createdRooms.Clear();
    }

    public void ClearLevelTilemap()
    {
        tilemap.ClearAllTiles();
    }
    
    public void ClearSpawnedObjects() 
    {
        foreach (Transform child in spawnedEnemiesContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in spawnedBulletsContainer)
        {
            Destroy(child.gameObject);
        }
        
        foreach (Transform child in spawnedLootContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    public void IncreaseLevelDifficulty() //called if player kills all enemies on current level 
    {
        if (currentLevel < maxLevel)
        {
                   currentLevel++;
                   levelNumberText.text = $"Level {currentLevel}";
            
                   levelWidth += progressiveLevelWidth;
                   levelHeight += progressiveLevelHeight;
                   maxIterations += progressiveMaxIterations;
                   minLeafSize = Mathf.Max(minAllowedLeafSize, minLeafSize - 1);
                   corridorWidth = defaultCorridorWidth; // not a mistake, larger corridors are bad for gameplay 
            
                   currentEnemiesPerRoom = Mathf.Min(currentEnemiesPerRoom + progressiveEnemiesPerRoom, maxEnemiesPerRoom); // Increase the number of enemies spawn in a room
        }
        else
        {
            Debug.LogError("Level " + currentLevel + " is out of level range.");
        }
    }
    
    public void ResetLevelDifficulty() //called if player dies
    {
        currentLevel = 1;
        levelNumberText.text = $"Level {currentLevel}";
        
        levelWidth = defaultLevelWidth;
        levelHeight = defaultLevelHeight;
        maxIterations = defaultMaxIterations;
        minLeafSize = defaultMinAllowedLeafSize;
        corridorWidth = defaultCorridorWidth;

        currentEnemiesPerRoom = defaultEnemiesPerRoom;
    }
    
    // Developer Cheats
    void Update() // FOR TESTING ONLY CAN BRAKE THE GAME
    {
        if (Input.GetKeyDown(KeyCode.K)) // Kill all enemy
        {
            OnAllEnemiesKilled?.Invoke();
        }
    }
}
