using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ElementIconDatabase", menuName = "RPG/UI/Element Icon Database")]
public class ElementIconDatabase : ScriptableObject
{
    [System.Serializable]
    public struct ElementIconPair
    {
        public Element element;
        public Sprite icon;
    }

    public List<ElementIconPair> elementIcons;

    private Dictionary<Element, Sprite> iconLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void OnValidate()
    {
        BuildLookup();
    }


    private void BuildLookup()
    {
        if (elementIcons == null)
        {
            iconLookup = new Dictionary<Element, Sprite>(); return;
        }

        iconLookup = elementIcons
    .Where(pair => pair.icon != null).GroupBy(pair => pair.element).ToDictionary(group => group.Key, group => group.First().icon);
    }


    public Sprite GetIconForElement(Element element)
    {
        if (iconLookup == null)
        {
            BuildLookup();
        }

        return iconLookup.TryGetValue(element, out var icon) ? icon : null;
    }
}