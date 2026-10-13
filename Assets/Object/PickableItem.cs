using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PickableItem : MonoBehaviour, IInteractable
{
    [Header("Настройки подсказки")]
    [SerializeField] private string _promptMessage = "Подобрать [E]";

    [Header("Слой при удержании")]
    [SerializeField] private string _heldLayerName = "HeldItem";

    private Rigidbody _rb;
    private Collider _col;
    private RigidbodyInterpolation _originalInterpolation;
    private readonly Dictionary<Transform, int> _originalLayers = new Dictionary<Transform, int>();

    public string PromptMessage => _promptMessage;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        if (_rb != null)
        {
            _originalInterpolation = _rb.interpolation;
        }
    }

    public void Interact(PlayerInteraction player)
    {
        player.PickUpItem(this);
    }

    public void PickUp(Transform holdPoint, string customHeldLayer = null)
    {
        if (_rb != null)
        {
            _originalInterpolation = _rb.interpolation;
            _rb.interpolation = RigidbodyInterpolation.None; // Отключаем интерполяцию, чтобы кубик следовал за рукой
            _rb.isKinematic = true;     // Отключаем гравитацию и физику
        }

        if (_col != null)
        {
            _col.enabled = false;        // Отключаем коллайдер, чтобы не толкать игрока изнутри
        }

        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        string layerToUse = !string.IsNullOrEmpty(customHeldLayer) ? customHeldLayer : _heldLayerName;
        SetHeldLayer(layerToUse);
    }

    public void Drop(Vector3 throwForce)
    {
        RestoreOriginalLayers();

        transform.SetParent(null);

        if (_rb != null)
        {
            _rb.isKinematic = false;     // Включаем физику обратно
            _rb.interpolation = _originalInterpolation; // Возвращаем интерполяцию для плавного полета
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(throwForce, ForceMode.Impulse); // Бросаем вперед
        }

        if (_col != null)
        {
            _col.enabled = true;         // Включаем коллайдер
        }
    }

    private void SetHeldLayer(string layerName)
    {
        int heldLayer = LayerMask.NameToLayer(layerName);
        if (heldLayer == -1)
        {
            Debug.LogWarning($"[PickableItem] Слой '{layerName}' не найден в Tags & Layers! Создайте его в настройках Unity.");
            return;
        }

        _originalLayers.Clear();
        SaveAndApplyLayerRecursively(transform, heldLayer);
    }

    private void SaveAndApplyLayerRecursively(Transform current, int newLayer)
    {
        _originalLayers[current] = current.gameObject.layer;
        current.gameObject.layer = newLayer;

        foreach (Transform child in current)
        {
            SaveAndApplyLayerRecursively(child, newLayer);
        }
    }

    private void RestoreOriginalLayers()
    {
        foreach (var pair in _originalLayers)
        {
            if (pair.Key != null)
            {
                pair.Key.gameObject.layer = pair.Value;
            }
        }
        _originalLayers.Clear();
    }
}