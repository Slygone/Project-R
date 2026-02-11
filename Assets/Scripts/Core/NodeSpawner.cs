using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns world nodes using ring-based placement driven by worldEncounter.json.
/// nodeCount is the authoritative total. Individual type counts are clamped to fit.
/// Nodes are placed in concentric rings around origin with guaranteed minimum spacing.
/// </summary>
public class NodeSpawner : MonoBehaviour
{
    [Header("Ring-Based Placement")]
    [Tooltip("Minimum distance between any two nodes")]
    [SerializeField] private float minNodeSpacing = 12f;
    
    [Tooltip("Minimum distance from player spawn (origin) to nearest node")]
    [SerializeField] private float playerSafeRadius = 10f;
    
    [Tooltip("Distance between concentric rings")]
    [SerializeField] private float ringSpacing = 14f;
    
    [Tooltip("Random offset applied to each node position (jitter)")]
    [SerializeField] private float positionJitter = 3f;
    
    [Tooltip("Height of nodes above ground")]
    [SerializeField] private float nodeHeight = 0.75f;

    void Start()
    {
        SpawnNodesForWorld(1);
    }

    public void SpawnNodesForWorld(int world)
    {
        var encounter = DataCache.GetWorldEncounter(world);
        int totalNodes = encounter.NodeCount;
        
        // Build node type list, clamped to totalNodes
        var nodeTypes = BuildNodeTypeList(encounter, totalNodes);
        
        // Shuffle so node types are randomly distributed across positions
        Shuffle(nodeTypes);
        
        // Generate well-spaced positions using concentric rings
        var positions = GenerateRingPositions(totalNodes);
        
        // Spawn each node at its position
        int spawned = Mathf.Min(totalNodes, positions.Count);
        for (int i = 0; i < spawned; i++)
        {
            SpawnNodeAt(nodeTypes[i], positions[i]);
        }
        
        int combat = 0, rest = 0, shop = 0, elite = 0;
        foreach (var t in nodeTypes)
        {
            if (t == typeof(CombatNode)) combat++;
            else if (t == typeof(RestNode)) rest++;
            else if (t == typeof(ShopNode)) shop++;
            else if (t == typeof(MysteryNode)) elite++;
        }
        
        Debug.Log($"[NodeSpawner] World {world}: spawned {spawned} nodes ({combat} combat, {rest} rest, {shop} shop, {elite} elite)");
    }
    
    /// <summary>
    /// Builds an ordered list of node types from encounter data, clamped to totalNodes.
    /// Priority: combat > elite > rest > shop (combat fills first, shop fills last).
    /// </summary>
    private List<System.Type> BuildNodeTypeList(WorldEncounterData encounter, int totalNodes)
    {
        var list = new List<System.Type>();
        
        int combat = Mathf.Min(encounter.CombatNodeCount, totalNodes);
        for (int i = 0; i < combat; i++) list.Add(typeof(CombatNode));
        
        int elite = Mathf.Min(encounter.EliteNodeCount, totalNodes - list.Count);
        for (int i = 0; i < elite; i++) list.Add(typeof(MysteryNode));
        
        int rest = Mathf.Min(encounter.RestNodeCount, totalNodes - list.Count);
        for (int i = 0; i < rest; i++) list.Add(typeof(RestNode));
        
        int shop = Mathf.Min(encounter.ShopNodeCount, totalNodes - list.Count);
        for (int i = 0; i < shop; i++) list.Add(typeof(ShopNode));
        
        // If data under-specified, fill remaining slots with combat nodes
        while (list.Count < totalNodes)
        {
            list.Add(typeof(CombatNode));
        }
        
        return list;
    }
    
    /// <summary>
    /// Places nodes in concentric rings radiating outward from origin.
    /// Each ring is at radius = playerSafeRadius + (ringIndex * ringSpacing).
    /// Nodes within a ring are evenly spaced angularly with random jitter.
    /// </summary>
    private List<Vector3> GenerateRingPositions(int count)
    {
        var positions = new List<Vector3>();
        int remaining = count;
        int ringIndex = 0;
        
        while (remaining > 0)
        {
            float radius = playerSafeRadius + (ringIndex * ringSpacing);
            
            // How many nodes fit in this ring with minimum spacing between them?
            // Arc length between nodes must be >= minNodeSpacing
            float circumference = 2f * Mathf.PI * radius;
            int capacity = Mathf.Max(1, Mathf.FloorToInt(circumference / minNodeSpacing));
            int nodesInRing = Mathf.Min(capacity, remaining);
            
            // Evenly distribute nodes around the ring with a random rotation offset
            float angleStep = 360f / nodesInRing;
            float angleOffset = Random.Range(0f, 360f);
            
            for (int i = 0; i < nodesInRing; i++)
            {
                float angle = (angleOffset + i * angleStep) * Mathf.Deg2Rad;
                
                // Apply jitter to both radius and angle
                float jitteredRadius = radius + Random.Range(-positionJitter, positionJitter);
                float angleJitter = (positionJitter / radius) * Random.Range(-0.5f, 0.5f);
                float jitteredAngle = angle + angleJitter;
                
                float x = Mathf.Cos(jitteredAngle) * jitteredRadius;
                float z = Mathf.Sin(jitteredAngle) * jitteredRadius;
                
                positions.Add(new Vector3(x, nodeHeight, z));
            }
            
            remaining -= nodesInRing;
            ringIndex++;
        }
        
        // Shuffle final positions so node types don't cluster by ring
        Shuffle(positions);
        
        return positions;
    }
    
    private void SpawnNodeAt(System.Type nodeType, Vector3 position)
    {
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        nodeObj.transform.position = position;
        nodeObj.transform.localScale = Vector3.one * 1.5f;
        nodeObj.name = nodeType.Name;
        
        var collider = nodeObj.GetComponent<SphereCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }
        
        nodeObj.AddComponent(nodeType);
    }
    
    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
