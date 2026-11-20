using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadialMenuLayout : MonoBehaviour
{
    [Header("Menu Settings")]
    [SerializeField] private RectTransform itemPrefab;
    [SerializeField] private int itemCount = 8;
    [SerializeField] private float radius = 220f;

    [Header("Menu Labels")]
    [SerializeField] private string[] itemNames =
    {
        "Inventory",
        "Skills",
        "Map",
        "Equipment",
        "Quests",
        "Shop",
        "Settings",
        "Exit"
    };

    private bool hasGenerated;

    private void Start()
    {
        GenerateItems();
    }

    private void GenerateItems()
    {
        if (hasGenerated)
            return;

        if (itemPrefab == null)
        {
            Debug.LogError("Item Prefab is missing.", this);
            return;
        }

        if (itemCount <= 0)
        {
            Debug.LogWarning("Item Count must be greater than zero.", this);
            return;
        }

        if (float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0f)
        {
            Debug.LogWarning("Invalid menu radius.", this);
            return;
        }

        hasGenerated = true;

        for (int i = 0; i < itemCount; i++)
        {
            float angle = 90f - i * 360f / itemCount;
            float radians = angle * Mathf.Deg2Rad;

            Vector2 position = new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            ) * radius;

            RectTransform item = Instantiate(itemPrefab, transform);

            if (item == null)
                continue;

            item.name = $"MenuItem_{i + 1:00}";
            item.anchoredPosition = position;

            Image image = item.GetComponent<Image>();

            if (image != null)
            {
                float hue = (float)i / itemCount;
                image.color = Color.HSVToRGB(hue, 0.65f, 1f);
            }

            TMP_Text label = item.GetComponentInChildren<TMP_Text>();

            string buttonName =
                itemNames != null && i < itemNames.Length &&
                !string.IsNullOrWhiteSpace(itemNames[i])
                    ? itemNames[i]
                    : $"Menu Item {i + 1}";

            if (label != null)
                label.text = buttonName;

            Button button = item.GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    Debug.Log("Clicked: " + buttonName);
                });
            }
        }
    }
}