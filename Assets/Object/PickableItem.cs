using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PickableItem : MonoBehaviour, IInteractable
{
    [Header("Настройки подсказки")]
    [SerializeField] private string _promptMessage = "Подобрать (E)";

    [Header("Слой при удержании")]
    [SerializeField] private string _heldLayerName = "HeldItem";

    private Rigidbody _rb;
    private Collider _col;
    private readonly Dictionary<Transform, int> _originalLayers = new Dictionary<Transform, int>();

    public string PromptMessage => _promptMessage;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    public void Interact(PlayerInteraction player)
    {
        player.PickUpItem(this);
    }

    public void PickUp(Transform holdPoint, string customHeldLayer = null)
    {
        _rb.isKinematic = true;     // Отключаем гравитацию и физику
        _col.enabled = false;        // Отключаем коллайдер, чтобы не толкать игрока изнутри

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
        _rb.isKinematic = false;     // Включаем физику обратно
        _col.enabled = true;         // Включаем коллайдер

        _rb.linearVelocity = Vector3.zero;
        _rb.AddForce(throwForce, ForceMode.Impulse); // Бросаем вперед
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