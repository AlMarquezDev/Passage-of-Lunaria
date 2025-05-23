using UnityEngine;
using UnityEngine.UI; // Necesario para Image
using TMPro; // Necesario para TextMeshProUGUI

public class ItemEntryUI : MonoBehaviour
{
    [Header("UI References within Prefab")]
    [Tooltip("Asigna aquí la Imagen para el icono del objeto.")]
    public Image itemIcon;
    [Tooltip("Asigna aquí el TextMeshPro para el nombre del objeto.")]
    public TMP_Text itemNameText;
    [Tooltip("Asigna aquí el TextMeshPro para la cantidad del objeto (ej. 'x3').")]
    public TMP_Text itemQuantityText;
    [Tooltip("Opcional: Imagen de fondo de este slot para resaltar.")]
    public Image background; // Opcional, para cambiar color al seleccionar

    [Header("Highlight Colors (Opcional)")]
    public Color normalColor = Color.clear; // Color normal del fondo (transparente?)
    public Color highlightColor = new Color(1f, 1f, 1f, 0.1f); // Color del fondo al resaltar

    private ConsumableItem assignedItem; // Guardar referencia al item

    /// <summary>
    /// Rellena los campos de UI con la información del objeto consumible.
    /// </summary>
    /// <param name="item">El ScriptableObject del objeto.</param>
    /// <param name="quantity">La cantidad actual en el inventario.</param>
    public void SetData(ConsumableItem item, int quantity)
    {
        assignedItem = item;

        if (item != null)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = item.icon; // Asignar icono
                itemIcon.enabled = (item.icon != null); // Habilitar/deshabilitar si hay icono
            }
            if (itemNameText != null)
            {
                itemNameText.text = item.itemName; // Asignar nombre
            }
            if (itemQuantityText != null)
            {
                itemQuantityText.text = $"x{quantity}"; // Asignar cantidad formateada
            }
        }
        else // Si el item es nulo (no debería pasar en la lista de consumibles usables)
        {
            if (itemIcon != null) itemIcon.enabled = false;
            if (itemNameText != null) itemNameText.text = "---";
            if (itemQuantityText != null) itemQuantityText.text = "";
        }

        // Asegurar estado de resaltado inicial normal
        SetHighlight(false);
    }

    /// <summary>
    /// Obtiene el objeto consumible asignado a este slot.
    /// </summary>
    public ConsumableItem GetAssignedItem()
    {
        return assignedItem;
    }

    /// <summary>
    /// Cambia la apariencia visual para indicar si está seleccionado.
    /// (Actualmente cambia el color de fondo si existe).
    /// </summary>
    /// <param name="isSelected">True si está seleccionado, false si no.</param>
    public void SetHighlight(bool isSelected)
    {
        if (background != null)
        {
            background.color = isSelected ? highlightColor : normalColor;
        }
        // Aquí podrías añadir otros efectos (cambiar color de texto, etc.)
    }
}