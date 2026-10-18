using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A prefab plus a clock. Every `interval` seconds it puts one coin somewhere in the arena,
/// until there are `maxCoins` of them out. The coins are children of this object, so transform.childCount counts them.
/// </summary>
public class Spawner : MonoBehaviour
{
    [SerializeField] GameObject coinPrefab;
    [SerializeField] GameObject redCoinPrefab;   // the special coin: only one ever appears per game
    [SerializeField] Transform player;

    [Header("Bonus coins")]
    [SerializeField] GameObject blueCoinPrefab;
    [SerializeField] float bonusRadius = 8f;         // metres: the coins fall inside this circle around the player
    [SerializeField] float bonusMinDistance = 1.5f;  // but not right on top of the player
    [SerializeField] float bonusSpacing = 0.9f;      // coins keep this far from each other when there is room
    [SerializeField] float arenaHalf = 9.3f;         // the floor is 20 m wide: stay inside the walls

    [Header("Timing")]
    [SerializeField] float interval = 1.5f;   // seconds between coins
    [SerializeField] int maxCoins = 5;
    [SerializeField] float area = 7f;         // coins appear within +/- area on x and z

    void Start() => InvokeRepeating(nameof(SpawnCoin), 1f, interval);

    void SpawnCoin()
    {
        if (transform.childCount >= maxCoins) return;

        float x = Random.Range(-area, area);
        float z = Random.Range(-area, area);
        Vector3 pos = new Vector3(x, 0.5f, z);

        if (Vector3.Distance(pos, player.position) < 2f) return;   // not on top of the player

        Instantiate(coinPrefab, pos, Quaternion.identity, transform);
    }

    // One red coin, somewhere in the arena. It is not a child of the spawner, so it does not use up one of the maxCoins.
    public void SpawnRedCoin()
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            Vector3 pos = new Vector3(Random.Range(-area, area), 0.5f, Random.Range(-area, area));
            if (Vector3.Distance(pos, player.position) < 2f) continue;   // not on top of the player
            Instantiate(redCoinPrefab, pos, Quaternion.identity);
            Debug.Log($"[Red coin] spawned at {pos}");
            return;
        }
    }

    // `count` blue coins in a circle around the player. They are children of their own "Bonus Coins" object,
    // so they do not count towards maxCoins and the Hierarchy stays tidy.
    public void SpawnBonusCoins(int count)
    {
        Transform parent = new GameObject("Bonus Coins").transform;
        Vector3 centre = player.position;
        float minSq = bonusMinDistance * bonusMinDistance, maxSq = bonusRadius * bonusRadius;
        var placed = new List<Vector3>();

        for (int tries = 0; placed.Count < count && tries < 5000; tries++)
        {
            Vector3 pos = PointInRing(centre, minSq, maxSq);
            if (InsideArena(pos) && !TooClose(pos, placed)) placed.Add(pos);
        }
        // a crowded corner: fill up without the spacing rule, so there are always exactly `count` coins
        for (int tries = 0; placed.Count < count && tries < 5000; tries++)
        {
            Vector3 pos = PointInRing(centre, minSq, maxSq);
            if (InsideArena(pos)) placed.Add(pos);
        }

        foreach (Vector3 pos in placed) Instantiate(blueCoinPrefab, pos, Quaternion.identity, parent);
        Debug.Log($"[Bonus coins] {placed.Count} blue coins within {bonusRadius} m of the player");
    }

    // A random point between two radii. Taking the square root of a random squared distance spreads the points evenly over the area.
    static Vector3 PointInRing(Vector3 centre, float minSq, float maxSq)
    {
        float angle = Random.value * Mathf.PI * 2f;
        float distance = Mathf.Sqrt(Random.Range(minSq, maxSq));
        return new Vector3(centre.x + Mathf.Cos(angle) * distance, 0.5f, centre.z + Mathf.Sin(angle) * distance);
    }

    bool InsideArena(Vector3 p) => Mathf.Abs(p.x) <= arenaHalf && Mathf.Abs(p.z) <= arenaHalf;

    bool TooClose(Vector3 p, List<Vector3> others)
    {
        foreach (Vector3 o in others) if ((o - p).sqrMagnitude < bonusSpacing * bonusSpacing) return true;
        return false;
    }
}
