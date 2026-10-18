using UnityEngine;

/// <summary>
/// A prefab plus a clock. Every `interval` seconds it puts one coin somewhere in the arena,
/// until there are `maxCoins` of them out. The coins are children of this object, so transform.childCount counts them.
/// </summary>
public class Spawner : MonoBehaviour
{
    [SerializeField] GameObject coinPrefab;
    [SerializeField] Transform player;
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
}
