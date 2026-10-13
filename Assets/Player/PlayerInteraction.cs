using TMPro;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Настройки луча")]
    [SerializeField] private float _rayDistance = 3.0f;
    [SerializeField] private LayerMask _interactLayers = ~0;

    [Header("Точка руки и Камера")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Transform _holdPoint;
    [Tooltip("Позиция удержания предмета относительно камеры, если _holdPoint не назначен вручную")]
    [SerializeField] private Vector3 _defaultHoldOffset = new Vector3(0.25f, -0.18f, 0.5f);

    [Header("Выбрасывание предмета")]
    [SerializeField] private float _throwForce = 4.0f;

    [Header("Слой для предмета в руках")]
    [Tooltip("Имя слоя, на который переключается предмет в руках для рендера поверх стен")]
    [SerializeField] private string _heldItemLayer = "HeldItem";

    [Header("Интерфейс (UI)")]
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private GameObject _promptUI;

    private PickableItem _currentHeldItem;
    private IInteractable _targetedInteractable;

    public bool IsHoldingItem => _currentHeldItem != null;

    private void Start()
    {
        if (_cameraTransform == null)
        {
            Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            if (cam != null) _cameraTransform = cam.transform;
        }

        if (_holdPoint == null && _cameraTransform != null)
        {
            GameObject autoHold = new GameObject("AutoHoldPoint");
            autoHold.transform.SetParent(_cameraTransform);
            autoHold.transform.localPosition = _defaultHoldOffset;
            autoHold.transform.localRotation = Quaternion.identity;
            _holdPoint = autoHold.transform;
        }

        if (_promptText == null && _promptUI != null)
        {
            _promptText = _promptUI.GetComponent<TextMeshProUGUI>();
        }

        HidePrompt();
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        if (_cameraTransform == null) return;

        Vector3 rayOrigin = _cameraTransform.position + _cameraTransform.forward * 0.35f;
        Ray ray = new Ray(rayOrigin, _cameraTransform.forward);

        RaycastHit[] hits = Physics.RaycastAll(ray, _rayDistance, _interactLayers);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        IInteractable foundInteractable = null;
        foreach (var hit in hits)
        {
            // Игнорируем триггеры
            if (hit.collider.isTrigger) continue;

            // Игнорируем самого игрока
            if (hit.collider.transform.root == transform.root) continue;

            // Игнорируем предмет, который мы уже держим в руках
            if (_currentHeldItem != null && hit.collider.transform.IsChildOf(_currentHeldItem.transform)) continue;

            // Ищем IInteractable на самом объекте или его родителях (например, коллайдер на двери, а скрипт на петле DoorPivot)
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                foundInteractable = interactable;
                break;
            }
            else
            {
                // Луч уперся в стену или другое непрозрачное препятствие
                break;
            }
        }

        _targetedInteractable = foundInteractable;

        if (_targetedInteractable != null)
        {
            ShowPrompt(_targetedInteractable.PromptMessage);
        }
        else
        {
            HidePrompt();
        }
    }

    private float _lastInteractTime = -1f;

    public void Interact()
    {
        // Защита от двойного клика в одном кадре
        if (Time.time - _lastInteractTime < 0.25f) return;
        _lastInteractTime = Time.time;

        if (_targetedInteractable == null) return;

        _targetedInteractable.Interact(this);

        // Сразу обновляем текст подсказки (например: "Открыть дверь" сменится на "Закрыть дверь")
        if (_targetedInteractable != null)
        {
            ShowPrompt(_targetedInteractable.PromptMessage);
        }
        else
        {
            HidePrompt();
        }
    }

    public void TurnOff()
    {
        if (_targetedInteractable is ITurnOffable turnOffable)
        {
            turnOffable.TurnOff();

            if (_targetedInteractable != null)
            {
                ShowPrompt(_targetedInteractable.PromptMessage);
            }
            else
            {
                HidePrompt();
            }
        }
    }

    public void PickUpItem(PickableItem item)
    {
        if (_currentHeldItem != null)
        {
            Drop();
        }

        _currentHeldItem = item;
        _currentHeldItem.PickUp(_holdPoint, _heldItemLayer);

        _targetedInteractable = null;
        HidePrompt();
    }

    public void Drop()
    {
        if (_currentHeldItem != null)
        {
            Vector3 force = _cameraTransform != null ? _cameraTransform.forward * _throwForce : transform.forward * _throwForce;
            _currentHeldItem.Drop(force);
            _currentHeldItem = null;
        }
    }

    private void ShowPrompt(string message)
    {
        if (_promptText != null)
        {
            _promptText.text = message;
            _promptText.gameObject.SetActive(true);
        }
        else if (_promptUI != null)
        {
            _promptUI.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        if (_promptText != null && _promptText.gameObject.activeSelf)
        {
            _promptText.gameObject.SetActive(false);
        }
        else if (_promptUI != null && _promptUI.activeSelf)
        {
            _promptUI.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_cameraTransform == null) return;
        Gizmos.color = Color.green;
        Vector3 rayOrigin = _cameraTransform.position + _cameraTransform.forward * 0.35f;
        Gizmos.DrawRay(rayOrigin, _cameraTransform.forward * _rayDistance);
    }
}