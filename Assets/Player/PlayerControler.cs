using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    public float currentSpeed;
    public float walkSpeed = 4f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2f;

    [Header("Crouch Settings")]
    [SerializeField] private float _standingHeight = 1.8f;
    [SerializeField] private float _crouchHeight = 0.65f;
    [SerializeField] private float _standingCameraY = 0.75f;
    [SerializeField] private float _crouchCameraY = -0.35f;
    [SerializeField] private float _colliderTransitionSpeed = 25f;
    [SerializeField] private float _cameraTransitionSpeed = 12f;
    [SerializeField] private LayerMask _obstacleLayers = ~0;

    [Header("Jump Settings")]
    [SerializeField] private float _jumpHeight = 0.8f;
    [SerializeField] private float _jumpBufferTime = 0.2f;

    [Header("References")]
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private CinemachineCamera _cinCam;
    [SerializeField] private Transform _cameraTarget;
    [SerializeField] private PlayerPhysics _playerPhysics;
    [SerializeField] private PlayerInteraction _playerInteraction;

    private Vector2 _move;
    private float _jumpBufferCounter;
    private bool _isCrouchPressed;
    private bool _isCrouching;
    private bool _isSprinting;

    public bool IsCrouching => _isCrouching;

    private void Awake()
    {
        if (_characterController == null) _characterController = GetComponent<CharacterController>();
        if (_playerPhysics == null) _playerPhysics = GetComponent<PlayerPhysics>();
        if (_playerInteraction == null) _playerInteraction = GetComponent<PlayerInteraction>();

        _characterController.height = _standingHeight;
        _characterController.center = Vector3.zero;
    }

    private void Start()
    {
        currentSpeed = walkSpeed;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (_cinCam == null) _cinCam = FindAnyObjectByType<CinemachineCamera>();

        if (_cameraTarget == null && _cinCam != null)
        {
            _cameraTarget = _cinCam.transform;
        }

        if (_cameraTarget != null)
        {
            Vector3 camPos = _cameraTarget.localPosition;
            camPos.y = _standingCameraY;
            _cameraTarget.localPosition = camPos;
        }
    }

    public void OnMove(InputValue val)
    {
        _move = val.Get<Vector2>();
    }

    public void OnSprint(InputValue val)
    {
        _isSprinting = val.isPressed;
    }

    public void OnCrouch(InputValue val)
    {
        _isCrouchPressed = val.isPressed;
    }

    public void OnJump(InputValue val)
    {
        if (val.isPressed && !_isCrouching)
        {
            _jumpBufferCounter = _jumpBufferTime;
        }
    }

    private void Update()
    {
        HandleCrouch();
        HandleJump();

        Vector3 moveDirection = (GetForward() * _move.y + GetRight() * _move.x) * currentSpeed;
        _characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (_isCrouchPressed)
        {
            _isCrouching = true;
        }
        else if (CanStandUp())
        {
            _isCrouching = false;
        }

        float targetHeight = _isCrouching ? _crouchHeight : _standingHeight;
        float targetCamY = _isCrouching ? _crouchCameraY : _standingCameraY;
        float targetSpeed = _isCrouching ? crouchSpeed : (_isSprinting ? sprintSpeed : walkSpeed);

        float newHeight = Mathf.Lerp(_characterController.height, targetHeight, Time.deltaTime * _colliderTransitionSpeed);
        _characterController.height = newHeight;

        float centerY = -(_standingHeight / 2f) + (newHeight / 2f);
        _characterController.center = new Vector3(0f, centerY, 0f);

        if (_cameraTarget != null)
        {
            Vector3 camPos = _cameraTarget.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, Time.deltaTime * _cameraTransitionSpeed);
            _cameraTarget.localPosition = camPos;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * _cameraTransitionSpeed);
    }

    private void HandleJump()
    {
        if (_jumpBufferCounter > 0)
        {
            _jumpBufferCounter -= Time.deltaTime;
        }

        if (_jumpBufferCounter > 0 && _playerPhysics != null && _playerPhysics.IsGrounded && !_isCrouching)
        {
            float jumpVelocity = Mathf.Sqrt(_jumpHeight * -2f * _playerPhysics.Gravity);
            _playerPhysics.SetVerticalVelocity(jumpVelocity);
            _jumpBufferCounter = 0;
        }
    }

    public bool CanStandUp()
    {
        float feetY = -(_standingHeight / 2f);
        Vector3 origin = transform.position + Vector3.up * (feetY + _crouchHeight - _characterController.radius);
        float radius = _characterController.radius - _characterController.skinWidth;
        float distance = (_standingHeight - _crouchHeight) + _characterController.skinWidth * 2f;

        return !Physics.SphereCast(origin, radius, Vector3.up, out _, distance, _obstacleLayers);
    }

    private Vector3 GetForward()
    {
        if (_cinCam == null) return transform.forward;

        Vector3 forward = _cinCam.transform.forward;
        forward.y = 0;
        return forward.normalized;
    }

    private Vector3 GetRight()
    {
        if (_cinCam == null) return transform.right;

        Vector3 right = _cinCam.transform.right;
        right.y = 0;
        return right.normalized;
    }

    public void OnInteract(InputValue val)
    {
        if (val.isPressed && _playerInteraction != null)
        {
            _playerInteraction.Interact();
        }
    }

    public void OnDrop(InputValue val)
    {
        if (val.isPressed && _playerInteraction != null)
        {
            _playerInteraction.Drop();
        }
    }

    public void OnTurnOff(InputValue val)
    {
        if (val.isPressed && _playerInteraction != null)
        {
            _playerInteraction.TurnOff();
        }
    }
}