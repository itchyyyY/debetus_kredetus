using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerPhysics : MonoBehaviour
{
    [Header("Gravity")]
    [SerializeField] private float _gravity = -15f;
    [SerializeField] private LayerMask _groundLayers = ~0;

    private CharacterController _characterController;
    private Vector3 _velocity;

    public bool IsGrounded => _characterController.isGrounded || CheckGrounded();
    public float Gravity => _gravity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        if (IsGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
        }

        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    public void SetVerticalVelocity(float verticalSpeed)
    {
        _velocity.y = verticalSpeed;
    }

    private bool CheckGrounded()
    {
        if (_characterController == null) return false;

        if (_velocity.y > 0.1f) return false;

        float radius = _characterController.radius;
        Vector3 feetPosition = transform.position + _characterController.center - Vector3.up * (_characterController.height / 2f);
        Vector3 origin = feetPosition + Vector3.up * radius;

        if (Physics.SphereCast(origin, radius * 0.9f, Vector3.down, out RaycastHit hit, radius * 0.5f + 0.08f, _groundLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != _characterController)
            {
                return true;
            }
        }

        return false;
    }
}