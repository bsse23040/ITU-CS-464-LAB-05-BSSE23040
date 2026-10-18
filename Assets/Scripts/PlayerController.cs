using UnityEngine;

/// <summary>
/// Rolls the ball. The keys are read in Update; the push is a force, applied in FixedUpdate.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveForce = 10f;
    [SerializeField] ForceMode mode = ForceMode.Force;   // a rate: it is called every physics step

    Rigidbody rb;
    Vector3 input;

    void Awake() => rb = GetComponent<Rigidbody>();

    void Update()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        input = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);   // diagonals are not stronger
    }

    void FixedUpdate() => rb.AddForce(input * moveForce, mode);
}
