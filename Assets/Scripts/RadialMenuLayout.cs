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

    private void Start()
    {
        GenerateItems();
    }

    private void GenerateItems()
    {
        for (int i = 0; i < itemCount; i++)
        {
            float angle = 90f - i * 360f / itemCount;
            float radians = angle * Mathf.Deg2Rad;

            Vector2 position = new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            ) * radius;

            RectTransform item = Instantiate(itemPrefab, transform);

            item.name = $"MenuItem_{i + 1:00}";
            item.anchoredPosition = position;

            Image image = item.GetComponent<Image>();

            if (image != null)
            {
                float hue = (float)i / itemCount;
                image.color = Color.HSVToRGB(hue, 0.65f, 1f);
            }

            TMP_Text label = item.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = i < itemNames.Length
                    ? itemNames[i]
                    : $"Menu Item {i + 1}";
            }

            Button button = item.GetComponent<Button>();

            if (button != null)
            {
                string buttonName = itemNames[i];

                button.onClick.AddListener(() =>
                {
                    Debug.Log("Clicked: " + buttonName);
                });
            }
        }
    }
}