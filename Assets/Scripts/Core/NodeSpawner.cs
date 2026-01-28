using System.Collections.Generic;
using UnityEngine;

public class NodeSpawner : MonoBehaviour
{
    [SerializeField] private GameObject nodePrefab;
    
    [Header("Grid-Based Spawning (no fallbacks)")]
    [Tooltip("Spacing between nodes in the grid")]
    [SerializeField] private float nodeSpacing = 8f;
    
    [Tooltip("Height of nodes above ground")]
    [SerializeField] private float nodeHeight = 0.75f;
    
    private List<Vector3> spawnedPositions = new List<Vector3>();
    private List<Vector3> availablePositions = new List<Vector3>();

    void Start()
    {
        SpawnNodesForWorld(1);
    }

    public void SpawnNodesForWorld(int world)
    {
        spawnedPositions.Clear();
        
        var encounterData = DataCache.GetWorldEncounter(world);
        
        int shopCount = encounterData.ShopNodeCount;
        int combatCount = encounterData.CombatNodeCount;
        int restCount = encounterData.RestNodeCount;
        int eliteCount = encounterData.EliteNodeCount;
        int totalNodes = shopCount + combatCount + restCount + eliteCount;
        
        // Generate grid positions for all nodes needed
        GenerateGridPositions(totalNodes);
        
        // Spawn nodes using pre-calculated positions
        for (int i = 0; i < shopCount; i++)
            SpawnNode<ShopNode>();
        
        for (int i = 0; i < combatCount; i++)
            SpawnNode<CombatNode>();
        
        for (int i = 0; i < restCount; i++)
            SpawnNode<RestNode>();
        
        for (int i = 0; i < eliteCount; i++)
            SpawnNode<MysteryNode>();
            
        Debug.Log($"[NodeSpawner] Spawned {totalNodes} nodes for World {world}: {shopCount} shops, {combatCount} combats, {restCount} rests, {eliteCount} elites");
    }
    
    /// <summary>
    /// Generates a grid of positions using a spiral pattern from center outward.
    /// Guarantees enough positions for all nodes without fallbacks.
    /// </summary>
    private void GenerateGridPositions(int count)
    {
        availablePositions.Clear();
        
        // Spiral out from center to generate positions
        // This ensures nodes are spread out evenly
        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(count)) + 2; // Extra buffer
        int half = gridSize / 2;
        
        // Generate positions in a spiral pattern starting from center
        List<Vector2Int> spiralOrder = GenerateSpiralOrder(gridSize);
        
        foreach (var gridPos in spiralOrder)
        {
            if (availablePositions.Count >= count) break;
            
            // Convert grid position to world position with some randomness
            float x = (gridPos.x - half) * nodeSpacing + Random.Range(-1f, 1f);
            float z = (gridPos.y - half) * nodeSpacing + Random.Range(-1f, 1f);
            
            // Skip positions too close to origin (player spawn)
            if (Mathf.Abs(x) < nodeSpacing * 0.5f && Mathf.Abs(z) < nodeSpacing * 0.5f)
                continue;
                
            availablePositions.Add(new Vector3(x, nodeHeight, z));
        }
        
        // Shuffle positions for variety
        ShufflePositions();
    }
    
    private List<Vector2Int> GenerateSpiralOrder(int size)
    {
        var result = new List<Vector2Int>();
        int x = 0, y = 0;
        int dx = 0, dy = -1;
        int half = size / 2;
        
        for (int i = 0; i < size * size; i++)
        {
            if (-half <= x && x <= half && -half <= y && y <= half)
            {
                result.Add(new Vector2Int(x + half, y + half));
            }
            
            if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
            {
                int temp = dx;
                dx = -dy;
                dy = temp;
            }
            x += dx;
            y += dy;
        }
        
        return result;
    }
    
    private void ShufflePositions()
    {
        for (int i = availablePositions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = availablePositions[i];
            availablePositions[i] = availablePositions[j];
            availablePositions[j] = temp;
        }
    }

    private void SpawnNode<T>() where T : NodeBase
    {
        Vector3 position = GetNextPosition();
        spawnedPositions.Add(position);
        
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nodeObj.transform.position = position;
        nodeObj.transform.localScale = Vector3.one * 1.5f;
        nodeObj.name = typeof(T).Name;
        
        var collider = nodeObj.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        nodeObj.AddComponent<T>();
    }

    /// <summary>
    /// Gets the next available position from the pre-generated grid.
    /// No fallbacks - positions are guaranteed by GenerateGridPositions.
    /// </summary>
    private Vector3 GetNextPosition()
    {
        if (availablePositions.Count > 0)
        {
            Vector3 pos = availablePositions[0];
            availablePositions.RemoveAt(0);
            return pos;
        }
        
        // This should never happen if GenerateGridPositions was called correctly
        Debug.LogError("[NodeSpawner] No available positions - this is a bug, GenerateGridPositions should have created enough");
        return new Vector3(spawnedPositions.Count * nodeSpacing, nodeHeight, 0);
    }
}
