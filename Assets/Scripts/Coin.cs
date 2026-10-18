using UnityEngine;

public enum CoinKind { Normal, Red, Blue }

/// <summary>
/// A collectible. It spins, and when the player touches it, it removes itself.
/// The collider on the prefab has Is Trigger ticked, so the ball passes through it and OnTriggerEnter fires.
/// </summary>
public class Coin : MonoBehaviour
{
    [SerializeField] CoinKind kind = CoinKind.Normal;
    [SerializeField] int value = 1;           // what it adds to the score
    [SerializeField] float spinSpeed = 90f;   // degrees per second

    void Update() => transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f);

    void OnTriggerEnter(Collider other)
    {
        // logged before any if, so when a pickup does nothing the Console shows whether the trigger fired at all
        Debug.Log($"Coin touched by {other.name}");
        if (other.CompareTag("Player"))
        {
            // the coin never touches the score itself: it reports to the GameManager
            switch (kind)
            {
                case CoinKind.Normal: GameManager.Instance.AddScore(value); break;
                case CoinKind.Red: GameManager.Instance.CollectRedCoin(value); break;
                case CoinKind.Blue: GameManager.Instance.CollectBonusCoin(); break;
            }
            Destroy(gameObject);                    // gameObject, not this: Destroy(this) would remove only the script
        }
    }
}
