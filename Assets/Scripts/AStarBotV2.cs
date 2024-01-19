using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AStarBotV2 : MonoBehaviour
{
    public Tilemap tilemap; // Needed for setting up the grid of nodes
    public string targetTileName; // What tile the bot is currently looking for
    public Vector3Int startPos; // Where is the bot currently?
    public Vector3Int targetPos;
    public bool lockCurrentTargetType; // Don't change the tile until it reached its target 

    private Grid grid; // idk
    public Queue<Vector3Int> path;
    private List<Vector3Int> unreachableTiles; // Path to target
    
    // Compress tilemap bounds to actual tiles and get grid component
    public void SetupBot()
    {
        unreachableTiles = new List<Vector3Int>();
        tilemap.CompressBounds();
        grid = tilemap.transform.parent.GetComponent<Grid>();
    }

    // Find Path to closest tile of a certain type
    public Queue<Vector3Int> FindPath()
    {
        Vector3Int targetTilePosition = FindClosestTargetTilePos();
        path = FindPath(startPos, targetTilePosition);
        path.Dequeue();
        return path;
    }

    // Find the closest tile of the needed type to the currentPosition 
    private Vector3Int FindClosestTargetTilePos()
    {
        Vector3Int startPosition = startPos; // Start of the path
        Vector3Int closestTargetTile = Vector3Int.zero;
        float closestActualDistance = float.MaxValue; // Distance to target tile which will be reduced until shortest is found 

        // Check all tiles within tilemap
        foreach (var pos in tilemap.cellBounds.allPositionsWithin)
        {
            if(unreachableTiles.Contains(pos)) continue;
            
            // Check if the current tile has the needed type
            if (tilemap.GetTile(pos).name.Contains(targetTileName) && !tilemap.GetTile(pos).name.Contains("Used"))
            {
                // Get distance from currentPosition to current target
                float distance = Vector3Int.Distance(startPosition, pos);

                // Check if the current distance is longer than the new distance
                if (distance < closestActualDistance)
                {
                    // Update target and distance to closest tile
                    closestActualDistance = distance;
                    closestTargetTile = pos;
                    targetPos = pos;
                }
            }
        }

        return closestTargetTile;
    }
    
    private Queue<Vector3Int> FindPath(Vector3Int startPosition, Vector3Int targetTilePosition)
    {
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        HashSet<Vector3Int> openSet = new HashSet<Vector3Int>() { startPosition };
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float> { { startPosition, 0 } };
        Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float> { { startPosition, HeuristicCostEstimate(startPosition, targetTilePosition) } };

        while (openSet.Count > 0)
        {
            Vector3Int current = GetLowestFScore(openSet, fScore);
            if (current == targetTilePosition)
                return ReconstructPath(cameFrom, targetTilePosition);

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector3Int neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor) || !IsTileWalkable(neighbor, tilemap))
                    continue;
                
                float tentativeGScore = gScore[current] + 1; // Assuming all tile costs are the same (1)
                if (!openSet.Contains(neighbor) || tentativeGScore < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, targetTilePosition);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        unreachableTiles.Add(targetPos);
        return FindPath();
    }
    
    static float HeuristicCostEstimate(Vector3Int start, Vector3Int goal)
    {
        return Vector3Int.Distance(start, goal);
    }

    static Vector3Int GetLowestFScore(HashSet<Vector3Int> openSet, Dictionary<Vector3Int, float> fScore)
    {
        float lowestFScore = float.MaxValue;
        Vector3Int lowestFScoreNode = Vector3Int.zero;

        foreach (Vector3Int node in openSet)
        {
            if (fScore.ContainsKey(node) && fScore[node] < lowestFScore)
            {
                lowestFScore = fScore[node];
                lowestFScoreNode = node;
            }
        }

        return lowestFScoreNode;
    }
    
    static Queue<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        Queue<Vector3Int> path = new Queue<Vector3Int>();
        path.Enqueue(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Enqueue(current);
        }

        return new Queue<Vector3Int>(path.Reverse());
    }
    
    static List<Vector3Int> GetNeighbors(Vector3Int current)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>
        {
            current + new Vector3Int(1, 0, 0), // Right
            current + new Vector3Int(-1, 0, 0), // Left
            current + new Vector3Int(0, 1, 0), // Up
            current + new Vector3Int(0, -1, 0) // Down
        };

        return neighbors;
    }
    
    static bool IsTileWalkable(Vector3Int position, Tilemap tilemap)
    {
        if (tilemap.GetTile(position) == null) return false;
        
        return !tilemap.GetTile(position).name.Contains("Wall") && !tilemap.GetTile(position).name.Contains("Water");
    }
}