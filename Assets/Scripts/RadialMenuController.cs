
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class RadialMenuController : MonoBehaviour
{
    [Header("Menu Data")]
    [SerializeField] private RadialMenuData menuData;

    [Header("Menu Settings")]
    [SerializeField] private RectTransform itemPrefab;
    [SerializeField] private float radius = 220f;
    [SerializeField] private float spacing = 20f;
    [SerializeField] private Button centerButton;
    [SerializeField] private TMP_Text centerButtonLabel;
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.35f;
    private Stack<RadialMenuNode> menuStack = new Stack<RadialMenuNode>();
    private bool isTransitioning = false;
    private int selectedIndex = 0;         

    private List<GameObject> generatedItems =
        new List<GameObject>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) ||
        Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedIndex++;
            RefreshSelection();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedIndex--;
            RefreshSelection();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!isTransitioning &&menuStack.Count > 0 &&generatedItems.Count > 0) 
            {
                int count = menuStack.Peek().children.Count;

                if (selectedIndex >= 0 && selectedIndex < count)
                    OnMenuItemClicked(menuStack.Peek().children[selectedIndex]);
            }
        }
    }
    private void RefreshSelection()
    {
        if (isTransitioning)
            return;

        int count = generatedItems.Count;

        if (count == 0)
            return;

        int attempts = 0;

        do
        {
            selectedIndex = (selectedIndex + count) % count;
            attempts++;

            if (!menuStack.Peek().children[selectedIndex].isDisabled)
                break;

        }
        while (attempts < count);

        for (int i = 0; i < count; i++)
        {
            if (generatedItems[i] == null)
                continue;

            RectTransform item =
                generatedItems[i].GetComponent<RectTransform>();

            if (item == null)
                continue;

            item.localScale = Vector3.one *
                (i == selectedIndex ? 1.15f : 1f);
        }
    }
    private void Start()
    {
        if (menuData == null || menuData.rootNode == null)
        {
            Debug.LogError("Menu Data is missing!");
            return;
        }

        menuStack.Push(menuData.rootNode);

        if (centerButton != null)
        {
            centerButton.onClick.AddListener(GoBack);
        }

        ShowCurrentMenu();
    }

    private void ShowCurrentMenu()
    {
        ClearItems();

        RadialMenuNode currentNode = menuStack.Peek();
        if (centerButtonLabel != null)
        {
            centerButtonLabel.text =
            menuStack.Count > 1 ? "BACK" : "HOME";
        }

        int count = currentNode.children.Count;
        float maxItemSize = 0f;

        foreach (RadialMenuNode child in currentNode.children)
        {
            maxItemSize = Mathf.Max(maxItemSize, child.itemSize);
        }

        float currentRadius = radius;

        if (count > 1)
        {
            float minRadius = (maxItemSize + spacing) /
                (2f * Mathf.Sin(Mathf.PI / count));

            currentRadius = Mathf.Max(radius, minRadius);
        }
            
        for (int i = 0; i < count; i++)
        {
            RadialMenuNode node = currentNode.children[i];

            float angle = 90f - i * 360f / count;
            float radians = angle * Mathf.Deg2Rad;

            Vector2 position = new Vector2(
                Mathf.Cos(radians),
                Mathf.Sin(radians)
            ) * currentRadius;

            RectTransform item = Instantiate(itemPrefab, transform);
            item.sizeDelta = new Vector2(node.itemSize,node.itemSize);
            Image background = item.GetComponent<Image>();

            if (background != null)
            {
                background.color = node.itemColor;
            }
            item.name = "MenuItem_" + node.menuName;
            item.anchoredPosition = position;
            item.localScale = Vector3.zero;

            Image icon = item.Find("ItemIcon").GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = node.menuIcon;
                icon.preserveAspect = true;
                icon.rectTransform.sizeDelta =
                    new Vector2(node.iconSize, node.iconSize);
                icon.enabled = node.menuIcon != null;
            }

            TMP_Text label = item.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = node.menuName;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10;
                label.fontSizeMax = 16;
                label.enableWordWrapping = false;
            }

            if (i == selectedIndex)
            {
                item.localScale = Vector3.one * 1.15f;
            }
            Button button = item.GetComponent<Button>();

            if (button != null)
            {
                button.interactable = !node.isDisabled;
                button.onClick.AddListener(() =>
                {
                    OnMenuItemClicked(node);
                });
            }

            StartCoroutine(AnimateItem(item, position, i));
            generatedItems.Add(item.gameObject);
        }
    }

    
    private void OnMenuItemClicked(RadialMenuNode node)
    {
        if (node.isDisabled)
            return;

        if (isTransitioning)
            return;

        if (node.HasChildren)
        {
            StartCoroutine(TransitionToNode(node));
        }
        else
        {
            Debug.Log("Selected: " + node.menuName);
        }
    }

    private void GoBack()
    {
        if (isTransitioning || menuStack.Count <= 1)
            return;

        StartCoroutine(TransitionBack());
    }


    private void ClearItems()
    {
        foreach (GameObject item in generatedItems)
        {
            Destroy(item);
        }

        generatedItems.Clear();
    }

    private IEnumerator AnimateItem(RectTransform item,Vector2 targetPosition,int index)
    {
        float elapsed = 0f;

        Vector2 startPosition = Vector2.zero;

        float startAngle = -90f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float angle = startAngle * (1f - smoothT);

            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * targetPosition;

            item.anchoredPosition = Vector2.Lerp(startPosition,direction, smoothT);
                
            item.localScale = Vector3.one * smoothT;

            yield return null;
        }

        item.anchoredPosition = targetPosition;
        item.localScale = Vector3.one;
    }

private IEnumerator TransitionToNode(RadialMenuNode node)
{
    isTransitioning = true;

    yield return StartCoroutine(AnimateItemsOut());

    menuStack.Push(node);
    selectedIndex = 0;
    ShowCurrentMenu();

    isTransitioning = false;
}

private IEnumerator TransitionBack()
{
    isTransitioning = true;

    yield return StartCoroutine(AnimateItemsOut());

    menuStack.Pop();
    selectedIndex = 0;
    ShowCurrentMenu();

    isTransitioning = false;
}

    private IEnumerator AnimateItemsOut()
    {
        float elapsed = 0f;

        List<RectTransform> items = new List<RectTransform>();
        List<Vector2> startPositions = new List<Vector2>();
        List<Vector3> startScales = new List<Vector3>();
        List<Quaternion> startRotations = new List<Quaternion>();

        foreach (GameObject obj in generatedItems)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            Button button = obj.GetComponent<Button>();

            if (button != null)
                button.interactable = false;

            items.Add(rect);
            startPositions.Add(rect.anchoredPosition);
            startScales.Add(rect.localScale);
            startRotations.Add(rect.localRotation);
        }

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;

                items[i].anchoredPosition = Vector2.Lerp(startPositions[i],Vector2.zero,smoothT);
                
                items[i].localScale = Vector3.Lerp(startScales[i],Vector3.zero,smoothT);
                
                items[i].localRotation = Quaternion.Euler(0f, 0f, 360f * smoothT)* startRotations[i];
                    
            }

            yield return null;
        }

        ClearItems();
    }

}
