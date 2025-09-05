using UnityEngine;

public class TreeSpawner : MonoBehaviour
{
    [Header("Scene refs")]
    public MeshCollider ground;
    public GameObject treePrefab;

    [Header("Spawn")]
    public int count = 1000;
    public float minSpacing = 3f;
    public Vector2 randomScale = new Vector2(0.9f, 1.3f);
    public bool alignToNormal = true;
    [Range(0, 90)] public float maxSlope = 35f;

    [Header("Filters")]
    public float minY = -Mathf.Infinity;
    public float maxY = Mathf.Infinity;
    [Range(0f, 1f)] public float minHeightPercent = 0.29f;
    [Range(0f, 1f)] public float maxHeightPercent = 1f;

    [Header("Raycast")]
    public LayerMask groundMask = ~0;

    [Header("Randomness")]
    public int seed = 12345;

    [ContextMenu("Spawn Trees")]
    public void Spawn()
    {
        if (!Validate()) return;

        Random.InitState(seed);
        Clear();

        var b = ground.bounds;
        int placed = 0, attempts = 0, maxAttempts = count * 10;

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(b.min.x, b.max.x);
            float z = Random.Range(b.min.z, b.max.z);
            float yTop = b.max.y + 100f;

            if (!Physics.Raycast(new Vector3(x, yTop, z), Vector3.down, out var hit, Mathf.Infinity, groundMask))
                continue;
            if (hit.collider != ground) continue;

            if (Vector3.Angle(hit.normal, Vector3.up) > maxSlope) continue;

            if (hit.point.y < minY || hit.point.y > maxY) continue;

            float hp = Mathf.InverseLerp(b.min.y, b.max.y, hit.point.y);
            if (hp < minHeightPercent || hp > maxHeightPercent) continue;

            bool tooClose = false;
            for (int i = 0; i < transform.childCount; i++)
            {
                if ((transform.GetChild(i).position - hit.point).sqrMagnitude < minSpacing * minSpacing)
                { tooClose = true; break; }
            }
            if (tooClose) continue;

            // place
            var t = Instantiate(treePrefab, hit.point, Quaternion.identity, transform).transform;
            t.rotation = (alignToNormal
                ? Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0, Random.Range(0f, 360f), 0)
                : Quaternion.Euler(0, Random.Range(0f, 360f), 0));

            float s = Random.Range(randomScale.x, randomScale.y);
            t.localScale = new Vector3(s, s, s);

            placed++;
        }

        Debug.Log($"TreeSpawner: placed {placed}/{count} trees (attempts {attempts}).");
    }

    [ContextMenu("Clear Trees")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
    }

    public void SpawnOn(MeshCollider newGround)
    {
        ground = newGround;
        Spawn();
    }

    bool Validate()
    {
        if (!ground)
        {
            Debug.LogWarning("TreeSpawner: Assign a MeshCollider to 'ground'.");
            return false;
        }
        if (!treePrefab)
        {
            Debug.LogWarning("TreeSpawner: Assign a tree prefab.");
            return false;
        }
        if (groundMask == 0) groundMask = Physics.DefaultRaycastLayers;
        return true;
    }
}
