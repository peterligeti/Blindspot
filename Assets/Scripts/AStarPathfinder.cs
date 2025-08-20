using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class AStarPathfinding
{
    private static Tilemap _tilemap;
    private static HashSet<Vector3Int> _walkableTiles;

    /// <summary>
    /// Prepares pathfinding by scanning the tilemap and storing all walkable tile positions.
    /// </summary>
    public static void Initialize(Tilemap tilemap, TileBase[] allowedTiles)
    {
        _tilemap = tilemap;
        _walkableTiles = new HashSet<Vector3Int>();

        BoundsInt bounds = _tilemap.cellBounds;
        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase tile = _tilemap.GetTile(pos);
            if (tile != null && System.Array.Exists(allowedTiles, t => t == tile))
                _walkableTiles.Add(pos);
        }
    }

    /// <summary>
    /// Returns a path from start to goal using A*, or null if no path exists.
    /// </summary>
    public static List<Vector3Int> Find(Vector3Int start, Vector3Int goal)
    {
        if (_tilemap == null)
        {
            Debug.LogError("AStarPathfinding not initialized. Call Initialize() first.");
            return null;
        }

        if (!_walkableTiles.Contains(start) || !_walkableTiles.Contains(goal))
            return null;

        var openSet = new HashSet<Vector3Int> { start };
        var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        var gScore = new Dictionary<Vector3Int, int> { [start] = 0 };
        var fScore = new Dictionary<Vector3Int, int> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Vector3Int current = GetLowestF(openSet, fScore);
            if (current == goal)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!_walkableTiles.Contains(neighbor))
                    continue;

                int moveCost = (neighbor.x != current.x && neighbor.y != current.y) ? 14 : 10; // Diagonal vs straight
                int tentativeG = gScore[current] + moveCost;

                if (gScore.TryGetValue(neighbor, out int existingG) && tentativeG >= existingG)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                openSet.Add(neighbor);
            }
        }
        return null;
    }

    /// <summary>
    /// Octile distance heuristic (good for diagonal movement).
    /// </summary>
    private static int Heuristic(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return 10 * (dx + dy) - 6 * Mathf.Min(dx, dy);
    }

    private static Vector3Int GetLowestF(HashSet<Vector3Int> set, Dictionary<Vector3Int, int> fScore)
    {
        Vector3Int best = default;
        int bestScore = int.MaxValue;

        foreach (var pos in set)
        {
            if (fScore.TryGetValue(pos, out int score) && score < bestScore)
            {
                bestScore = score;
                best = pos;
            }
        }
        return best;
    }

    private static List<Vector3Int> GetNeighbors(Vector3Int pos)
    {
        return new List<Vector3Int>
        {
            pos + Vector3Int.up,
            pos + Vector3Int.down,
            pos + Vector3Int.left,
            pos + Vector3Int.right,
            pos + new Vector3Int(1, 1, 0),
            pos + new Vector3Int(-1, 1, 0),
            pos + new Vector3Int(1, -1, 0),
            pos + new Vector3Int(-1, -1, 0)
        };
    }

    private static List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> path = new List<Vector3Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }
}
