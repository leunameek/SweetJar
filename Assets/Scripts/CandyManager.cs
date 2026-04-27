using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CandyManager : MonoBehaviour
{
    [Header("References")]
    public GameConfig config;
    public Transform candyRoot;
    public GameObject[] candyPrefabs; // assign Blue, Brown, Green, Orange, Yellow in Inspector

    // Runtime state
    [HideInInspector] public List<Rigidbody> candyBodies = new List<Rigidbody>();
    [HideInInspector] public int escapedCount;
    [HideInInspector] public int totalCandies;

    private float escapeY;

    void FixedUpdate()
    {
        foreach (var rb in candyBodies)
        {
            if (rb == null) continue;
            if (rb.linearVelocity.y > config.maxCandySpeedY)
            {
                Vector3 v = rb.linearVelocity;
                v.y = config.maxCandySpeedY;
                rb.linearVelocity = v;
            }
        }
    }

    public void Spawn(int count, System.Action onComplete = null)
    {
        Clear();
        totalCandies = count;
        escapeY = config.boxHeight + config.escapeMargin;
        StartCoroutine(SpawnWaterfall(count, onComplete));
    }

    private IEnumerator SpawnWaterfall(int count, System.Action onComplete)
    {
        float holeRadius = config.escapeHoleRadius * 0.6f;

        for (int i = 0; i < count; i++)
        {
            Vector2 rand2D = Random.insideUnitCircle * holeRadius;
            Vector3 pos = new Vector3(rand2D.x, config.spawnHeight, rand2D.y);

            GameObject prefab = candyPrefabs[Random.Range(0, candyPrefabs.Length)];
            GameObject candy = Instantiate(prefab, pos, Random.rotation, candyRoot);
            Rigidbody rb = candy.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rb.interpolation = RigidbodyInterpolation.None;
            candyBodies.Add(rb);

            yield return new WaitForSeconds(config.spawnInterval);
        }

        onComplete?.Invoke();
    }

    public void CheckEscaped()
    {
        for (int i = candyBodies.Count - 1; i >= 0; i--)
        {
            if (candyBodies[i] == null) continue;

            Vector3 pos = candyBodies[i].position;

            if (pos.y < 0f)
            {
                Destroy(candyBodies[i].gameObject);
                candyBodies[i] = null;
                continue;
            }

            if (pos.y > escapeY)
            {
                float xzDist = new Vector2(pos.x, pos.z).magnitude;
                if (xzDist < config.escapeHoleRadius)
                {
                    escapedCount++;
                    Destroy(candyBodies[i].gameObject);
                    candyBodies[i] = null;
                }
            }
        }
    }

    public void Clear()
    {
        foreach (var rb in candyBodies)
            if (rb != null) Destroy(rb.gameObject);
        candyBodies.Clear();
        escapedCount = 0;
    }

    public bool IsWon()
    {
        return totalCandies > 0 &&
               escapedCount >= Mathf.FloorToInt(totalCandies * config.winRatio);
    }
}
