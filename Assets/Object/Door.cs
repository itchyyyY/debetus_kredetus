using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    public enum HingeSide
    {
        Right,  // Правая грань
        Left    // Левая грань
    }

    [Header("Петля (Ось вращения)")]
    [Tooltip("Какую грань использовать как ось вращения")]
    [SerializeField] private HingeSide _hingeSide = HingeSide.Right;

    [Header("Настройки открывания")]
    [Tooltip("Угол открытия двери (например, 90 или -90)")]
    [SerializeField] private float _openAngle = 90f;

    [Tooltip("Скорость открывания/закрывания")]
    [SerializeField] private float _speed = 2.5f;

    [Header("Подсказки для экрана")]
    [SerializeField] private string _openPrompt = "Открыть дверь [E]";
    [SerializeField] private string _closePrompt = "Закрыть дверь [E]";

    private bool _isOpen = false;
    private float _currentProgress = 0f; // 0 = закрыта, 1 = открыта

    private Vector3 _closedPos;
    private Quaternion _closedRot;
    private Vector3 _worldHingePoint;
    private Vector3 _worldHingeAxis;
    private Vector3 _localHingePoint;

    public bool IsOpen => _isOpen;
    public string PromptMessage => _isOpen ? _closePrompt : _openPrompt;

    private void Start()
    {
        CalculateHingeAndStates();
    }

    public void CalculateHingeAndStates()
    {
        _closedPos = transform.position;
        _closedRot = transform.rotation;
        _worldHingeAxis = transform.up;

        Vector3 size = Vector3.one;
        Vector3 center = Vector3.zero;

        if (TryGetComponent<BoxCollider>(out var box))
        {
            size = box.size;
            center = box.center;
        }
        else if (TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
        {
            size = mf.sharedMesh.bounds.size;
            center = mf.sharedMesh.bounds.center;
        }

        // Автоматически определяем, вдоль какой оси дверь шире (X или Z)
        float worldWidthX = size.x * Mathf.Abs(transform.lossyScale.x);
        float worldWidthZ = size.z * Mathf.Abs(transform.lossyScale.z);

        if (worldWidthX >= worldWidthZ)
        {
            // Дверь широкая по оси X
            float xOffset = (_hingeSide == HingeSide.Right) 
                ? (center.x + size.x * 0.5f) 
                : (center.x - size.x * 0.5f);
            _localHingePoint = new Vector3(xOffset, center.y, center.z);
        }
        else
        {
            // Дверь широкая по оси Z
            float zOffset = (_hingeSide == HingeSide.Right) 
                ? (center.z + size.z * 0.5f) 
                : (center.z - size.z * 0.5f);
            _localHingePoint = new Vector3(center.x, center.y, zOffset);
        }

        _worldHingePoint = transform.TransformPoint(_localHingePoint);
    }

    private void Update()
    {
        float targetProgress = _isOpen ? 1f : 0f;

        // Плавно меняем прогресс открытия от 0 до 1
        _currentProgress = Mathf.MoveTowards(_currentProgress, targetProgress, Time.deltaTime * _speed);

        // SmoothStep дает мягкое ускорение в начале и замедление в конце
        float smoothT = Mathf.SmoothStep(0f, 1f, _currentProgress);
        float currentAngle = smoothT * _openAngle;

        // Чистое вращение вокруг оси петли по идеальной круговой дуге (без срезания и съезжания)
        Quaternion rotationDelta = Quaternion.AngleAxis(currentAngle, _worldHingeAxis);
        transform.rotation = rotationDelta * _closedRot;
        transform.position = _worldHingePoint + rotationDelta * (_closedPos - _worldHingePoint);
    }

    public void Interact(PlayerInteraction player)
    {
        ToggleDoor();
    }

    public void ToggleDoor()
    {
        _isOpen = !_isOpen;
    }

    // Визуализация оси петли в окне Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 localHinge = _localHingePoint;

        if (!Application.isPlaying)
        {
            CalculateHingeAndStates();
            localHinge = _localHingePoint;
        }

        Vector3 worldHinge = transform.TransformPoint(localHinge);
        Gizmos.DrawWireSphere(worldHinge, 0.08f);
        Gizmos.DrawLine(worldHinge - transform.up * 1f, worldHinge + transform.up * 1f);
    }
}
