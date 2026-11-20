using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class RadialMenuController : MonoBehaviour
{
    [Header("Menu Data")]
    [SerializeField] private RadialMenuData menuData;
    private Stack<RadialMenuNode> menuStack = new Stack<RadialMenuNode>();
    [Header("Menu Settings")]
    [SerializeField] private RectTransform itemPrefab;
    [SerializeField] private float radius = 220f;
    [SerializeField] private float spacing = 20f;
    [SerializeField] private Button centerButton;
    [SerializeField] private TMP_Text centerButtonLabel;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.35f;
    [SerializeField] private Color disabledColor =
        new Color(0.35f, 0.35f, 0.35f, 0.7f);
    [SerializeField] private GameObject menuTooltip;
    [SerializeField] private TMP_Text menuTooltipText;
    [SerializeField] private CanvasGroup tooltipCanvasGroup;

    [Header("Tooltip Settings")]
    [SerializeField] private float tooltipShowDelay = 0.3f;
    [SerializeField] private float tooltipFadeDuration = 0.2f;
    [SerializeField] private Vector2 tooltipOffset =
        new Vector2(16f, -16f);
    [SerializeField] private Color tooltipBackgroundColor =
        new Color(0.15f, 0.15f, 0.15f, 0.95f);
    [SerializeField] private Color tooltipTextColor =
        Color.white;
    private Coroutine tooltipCoroutine;

    [Header("Selection Settings")]

    [SerializeField] private float selectedScale = 1.15f;

    [SerializeField] private float selectionAnimationDuration = 0.15f;

    [SerializeField] private Color selectedColor =
        new Color(1f, 0.85f, 0.3f, 1f);

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioClip navigateSound;

    [SerializeField, Range(0f, 1f)]
    private float audioVolume = 1f;

    [Header("Empty Menu Settings")]
    [SerializeField] private string emptyMenuMessage = "No items available";
    private bool isTransitioning = false;
    private int selectedIndex = 0;

    private readonly Dictionary<int, Coroutine>
    selectionAnimations = new Dictionary<int, Coroutine>();
    private List<GameObject> generatedItems =
        new List<GameObject>();

    private readonly List<Coroutine> itemAnimations =
        new List<Coroutine>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow) ||
        Input.GetKeyDown(KeyCode.DownArrow))
        {
            RefreshSelection(1);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow) ||
        Input.GetKeyDown(KeyCode.UpArrow))
        {
            RefreshSelection(-1);
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
        if (menuTooltip != null && menuTooltip.activeSelf)
        {
            RectTransform tooltipRect =
                menuTooltip.GetComponent<RectTransform>();

            if (tooltipRect != null)
            {
                Vector3 mousePosition = Input.mousePosition;

                Vector3 tooltipPosition =
                    mousePosition + new Vector3(
                    tooltipOffset.x,
                    tooltipOffset.y,
                    0f
                );

                float width = tooltipRect.rect.width;
                float height = tooltipRect.rect.height;

                float minX = width * tooltipRect.pivot.x;
                float maxX = Screen.width -
                width * (1f - tooltipRect.pivot.x);

                float minY = height * tooltipRect.pivot.y;
                float maxY = Screen.height -
                height * (1f - tooltipRect.pivot.y);

                tooltipPosition.x = Mathf.Clamp(tooltipPosition.x, minX, maxX);
                tooltipPosition.y = Mathf.Clamp(tooltipPosition.y, minY, maxY);
                tooltipRect.position = tooltipPosition;
            }
        }
    }
    private void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        return;

        audioSource.PlayOneShot(clip, audioVolume);
    }
    private void RefreshSelection(int direction)
    {
        if (isTransitioning || menuStack == null || menuStack.Count == 0)
            return;

        RadialMenuNode currentNode = menuStack.Peek();

        if (currentNode == null || currentNode.children == null)
            return;

        int count = Mathf.Min(
            generatedItems.Count,
            currentNode.children.Count
        );

        if (count == 0)
            return;

        bool foundEnabledItem = false;

        for (int attempts = 0; attempts < count; attempts++)
        {
            selectedIndex = (selectedIndex + direction + count) % count;

            RadialMenuNode node = currentNode.children[selectedIndex];

            if (node != null && !node.isDisabled)
            {
                foundEnabledItem = true;
                break;
            }
        }

        if (!foundEnabledItem)
            return;

        PlaySound(navigateSound);

        for (int i = 0; i < count; i++)
        {
            UpdateSelectionVisual(i);
        }
    }

    public void RefreshCurrentMenu()
    {
        if (isTransitioning)
            return;

        if (menuStack == null || menuStack.Count == 0)
            return;

        RadialMenuNode currentNode = menuStack.Peek();

        if (currentNode == null)
            return;

        if (currentNode.children == null)
            currentNode.children = new List<RadialMenuNode>();

        int firstEnabledIndex = FindFirstEnabledIndex(currentNode);

        selectedIndex = firstEnabledIndex >= 0
            ? firstEnabledIndex
            : 0;

        ShowCurrentMenu();
    }

    private bool CanEnterNode(RadialMenuNode node)
    {
        if (node == null || node.isDisabled)
        return false;

        if (menuStack.Count == 0)
        return false;

        if (menuStack.Peek() == node)
        return false;

        RadialMenuNode currentNode = menuStack.Peek();

        if (currentNode.children == null)
        return false;

        return currentNode.children.Contains(node);
    }

    public bool AddMenuItem(
        RadialMenuNode parent,
        RadialMenuNode newItem)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Cannot modify menu during transition.");
            return false;
        }

        if (menuData == null ||
        menuData.rootNode == null ||
        parent == null ||
        newItem == null)
        {
            Debug.LogWarning("Invalid menu data.");
            return false;
        }

        if (!IsValidMenuName(newItem.menuName))
        {
            Debug.LogWarning("Menu item name cannot be empty.");
            return false;
        }

        if (!ContainsNode(menuData.rootNode, parent))
        {
            Debug.LogWarning("Parent does not belong to this menu.");
            return false;
        }

        if (newItem.itemSize <= 0f ||
        newItem.iconSize < 0f ||
        float.IsNaN(newItem.itemSize) ||
        float.IsInfinity(newItem.itemSize) ||
        float.IsNaN(newItem.iconSize) ||
        float.IsInfinity(newItem.iconSize))
        {
            Debug.LogWarning("Invalid menu item size.");
            return false;
        }

        // Prevent adding a node that already exists in the menu tree.
        if (ContainsNode(menuData.rootNode, newItem))
        {
            Debug.LogWarning("This menu item already exists.");
            return false;
        }

        // Prevent circular references.
        if (ContainsNode(newItem, parent))
        {
            Debug.LogWarning("Adding this item would create a cycle.");
            return false;
        }

        if (parent.children == null)
        parent.children = new List<RadialMenuNode>();

        foreach (RadialMenuNode child in parent.children)
        {
            if (child != null &&
            string.Equals(
                child.menuName,
                newItem.menuName,
                System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning(
                    "A menu item with this name already exists."
                );
                return false;
            }
        }

        parent.children.Add(newItem);

        if (menuStack.Count > 0 && menuStack.Peek() == parent)
        RefreshCurrentMenu();

        return true;
    }

    public bool RemoveMenuItem(
        RadialMenuNode parent,
        RadialMenuNode item)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("Cannot modify menu during transition.");
            return false;
        }

        if (menuData == null ||
        menuData.rootNode == null ||
        parent == null ||
        item == null ||
        parent.children == null)
        {
            return false;
        }

        if (!ContainsNode(menuData.rootNode, parent))
        return false;

        // Ensure the item belongs directly to this parent.
        if (!parent.children.Contains(item))
        return false;

        // Protect the current menu path.
        if (IsNodeInMenuStack(item))
        {
            Debug.LogWarning(
                "Cannot remove a menu item in the current menu path."
            );
            return false;
        }

        bool removed = parent.children.Remove(item);

        if (!removed)
        return false;

        if (menuStack.Count > 0 && menuStack.Peek() == parent)
        RefreshCurrentMenu();

        return true;
    }

    public bool UpdateMenuItem(
        RadialMenuNode parent,
        RadialMenuNode target,
        RadialMenuNode updatedData)
    {
        if (isTransitioning)
            return false;

        if (menuData == null ||
            menuData.rootNode == null ||
            parent == null ||
            target == null ||
            updatedData == null ||
            parent.children == null)
        {
            return false;
        }

        if (!ContainsNode(menuData.rootNode, parent))
            return false;

        if (!parent.children.Contains(target))
            return false;

        if (!IsValidMenuName(updatedData.menuName))
        {
            Debug.LogWarning("Updated menu name cannot be empty.");
            return false;
        }

        if (!IsValidSize(updatedData.itemSize, false) ||
            !IsValidSize(updatedData.iconSize, true))
        {
            Debug.LogWarning("Invalid updated menu item size.");
            return false;
        }

        foreach (RadialMenuNode child in parent.children)
        {
            if (child != null &&
                child != target &&
                string.Equals(
                    child.menuName,
                    updatedData.menuName,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("Another item already uses this name.");
                return false;
            }
        }

        target.menuName = updatedData.menuName;
        target.description = updatedData.description;
        target.menuIcon = updatedData.menuIcon;
        target.iconSize = updatedData.iconSize;
        target.itemColor = updatedData.itemColor;
        target.isDisabled = updatedData.isDisabled;
        target.itemSize = updatedData.itemSize;

        if (menuStack.Count > 0 && menuStack.Peek() == parent)
            RefreshCurrentMenu();

        return true;
    }

    private void UpdateSelectionVisual(int index)
    {
        if (menuStack == null || menuStack.Count == 0)
            return;

        if (index < 0 || index >= generatedItems.Count)
            return;

        RadialMenuNode currentNode = menuStack.Peek();

        if (currentNode == null ||
            currentNode.children == null ||
            index >= currentNode.children.Count)
        {
            return;
        }

        GameObject itemObject = generatedItems[index];

        if (itemObject == null)
            return;

        if (selectionAnimations.TryGetValue(index, out Coroutine running))
        {
            if (running != null)
                StopCoroutine(running);

            selectionAnimations.Remove(index);
        }

        RadialMenuNode node = currentNode.children[index];

        bool isSelected =
            node != null &&
            !node.isDisabled &&
            index == selectedIndex;

        RectTransform item = itemObject.GetComponent<RectTransform>();

        if (item == null)
            return;

        Image background = item.GetComponent<Image>();

        if (background != null)
        {
            Outline outline = background.GetComponent<Outline>();

            if (outline == null)
                outline = background.gameObject.AddComponent<Outline>();

            outline.effectColor = selectedColor;
            outline.effectDistance = new Vector2(4f, -4f);
            outline.enabled = isSelected;
        }

        selectionAnimations[index] = StartCoroutine(
            AnimateSelection(
                item,
                isSelected ? selectedScale : 1f,
                index
            )
        );
    }

    private IEnumerator AnimateSelection(
        RectTransform item,
        float targetScale,
        int index)
    {
        float elapsed = 0f;
        Vector3 startScale = item.localScale;

        while (elapsed < selectionAnimationDuration)
        {
            if (item == null)
            yield break;

            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / selectionAnimationDuration
            );

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            item.localScale = Vector3.Lerp(
                startScale,
                Vector3.one * targetScale,
                smoothT
            );

            yield return null;
        }

        if (item != null)
        item.localScale = Vector3.one * targetScale;

        selectionAnimations.Remove(index);
    }
    private void Start()
    {
        if (!ValidateMenuData())
        {
            Debug.LogError("Radial Menu initialization failed.");
            enabled = false;
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogError("Item Prefab is missing!");
            enabled = false;
            return;
        }

        menuStack.Clear();
        menuStack.Push(menuData.rootNode);

        selectedIndex = 0;

        if (centerButton != null)
        {
            centerButton.onClick.RemoveListener(GoBack);
            centerButton.onClick.AddListener(GoBack);
        }

        ShowCurrentMenu();
    }

    private void ShowCurrentMenu()
    {
        ClearItems();
        HideTooltipImmediate();

        if (menuStack == null || menuStack.Count == 0)
        {
            Debug.LogWarning("Cannot display menu: menu stack is empty.");
            return;
        }

        RadialMenuNode currentNode = menuStack.Peek();

        if (currentNode == null)
        {
            Debug.LogError("Current menu node is null.");
            return;
        }

        if (centerButtonLabel != null)
        {
            centerButtonLabel.text =
                menuStack.Count > 1 ? "BACK" : "HOME";
        }

        if (currentNode.children == null)
            currentNode.children = new List<RadialMenuNode>();

        int count = currentNode.children.Count;

        if (count == 0)
        {
            Debug.Log(emptyMenuMessage);
            selectedIndex = 0;
            return;
        }

        // Validate children before generating UI.
        foreach (RadialMenuNode child in currentNode.children)
        {
            if (child == null ||
                !IsValidMenuName(child.menuName) ||
                !IsValidSize(child.itemSize, false) ||
                !IsValidSize(child.iconSize, true))
            {
                Debug.LogError(
                    "Invalid child node in menu: " + currentNode.menuName
                );
                selectedIndex = 0;
                return;
            }
        }

        int firstEnabledIndex = FindFirstEnabledIndex(currentNode);

        selectedIndex = firstEnabledIndex >= 0
            ? firstEnabledIndex
            : 0;

        float maxItemSize = 0f;

        foreach (RadialMenuNode child in currentNode.children)
        {
            maxItemSize = Mathf.Max(maxItemSize, child.itemSize);
        }

        float safeRadius = Mathf.Max(0f, radius);
        float safeSpacing = Mathf.Max(0f, spacing);
        float currentRadius = safeRadius;

        if (count > 1)
        {
            float denominator =
                2f * Mathf.Sin(Mathf.PI / count);

            if (denominator > 0f)
            {
                float minRadius =
                    (maxItemSize + safeSpacing) / denominator;

                currentRadius = Mathf.Max(safeRadius, minRadius);
            }
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

            if (item == null)
                continue;

            item.name = "MenuItem_" + node.menuName;
            item.sizeDelta = new Vector2(node.itemSize, node.itemSize);
            item.anchoredPosition = position;
            item.localScale = Vector3.zero;

            Image background = item.GetComponent<Image>();

            if (background != null)
            {
                background.color = node.isDisabled
                    ? disabledColor
                    : node.itemColor;
            }

            // Configure icon safely.
            Transform iconTransform = item.Find("ItemIcon");

            if (iconTransform != null)
            {
                Image icon = iconTransform.GetComponent<Image>();

                if (icon != null)
                {
                    icon.sprite = node.menuIcon;
                    icon.preserveAspect = true;
                    icon.rectTransform.sizeDelta =
                        new Vector2(node.iconSize, node.iconSize);

                    icon.enabled = node.menuIcon != null;
                    icon.color = node.isDisabled
                        ? disabledColor
                        : Color.white;
                }
            }

            // Configure label.
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = node.menuName;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10;
                label.fontSizeMax = 16;
                label.enableWordWrapping = false;
                label.color = node.isDisabled
                    ? disabledColor
                    : Color.white;
            }

            Button button = item.GetComponent<Button>();

            if (button != null)
            {
                button.interactable = !node.isDisabled;
                button.onClick.AddListener(
                    () => OnMenuItemClicked(node)
                );
            }

            SetupTooltipEvents(item.gameObject, node);

            generatedItems.Add(item.gameObject);

            Coroutine animation = StartCoroutine(
                AnimateItem(item, position, i)
            );

            if (animation != null)
                itemAnimations.Add(animation);
        }
    }

    private void HideTooltipImmediate()
    {
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }

        if (tooltipCanvasGroup != null)
        tooltipCanvasGroup.alpha = 0f;

        if (menuTooltip != null)
        menuTooltip.SetActive(false);
    }

    private void ShowTooltip(RadialMenuNode node)
    {
        if (menuTooltip == null ||
        menuTooltipText == null ||
        tooltipCanvasGroup == null)
        return;

        if (node == null || string.IsNullOrEmpty(node.description))
        {
            HideTooltip();
            return;
        }

        if (tooltipCoroutine != null)
        StopCoroutine(tooltipCoroutine);

        tooltipCoroutine =
            StartCoroutine(ShowTooltipRoutine(node));
    }

    private IEnumerator ShowTooltipRoutine(RadialMenuNode node)
    {
        if (node == null ||
            menuTooltip == null ||
            menuTooltipText == null ||
            tooltipCanvasGroup == null)
        {
            tooltipCoroutine = null;
            yield break;
        }

        Image background = menuTooltip.GetComponent<Image>();

        if (background != null)
            background.color = tooltipBackgroundColor;

        menuTooltipText.color = tooltipTextColor;
        menuTooltipText.text = node.description;

        menuTooltip.SetActive(true);
        tooltipCanvasGroup.alpha = 0f;

        yield return new WaitForSecondsRealtime(
            Mathf.Max(0f, tooltipShowDelay)
        );

        if (menuTooltip == null ||
            menuTooltipText == null ||
            tooltipCanvasGroup == null)
        {
            tooltipCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0f, tooltipFadeDuration);

        while (elapsed < duration)
        {
            if (tooltipCanvasGroup == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            tooltipCanvasGroup.alpha = Mathf.Clamp01(
                elapsed / duration
            );

            yield return null;
        }

        if (tooltipCanvasGroup != null)
            tooltipCanvasGroup.alpha = 1f;

        tooltipCoroutine = null;
    }

    private void HideTooltip()
    {
        if (menuTooltip == null || tooltipCanvasGroup == null)
        return;

        if (tooltipCoroutine != null)
        StopCoroutine(tooltipCoroutine);

        tooltipCoroutine =
            StartCoroutine(HideTooltipRoutine());
    }

    private IEnumerator HideTooltipRoutine()
    {
        if (tooltipCanvasGroup == null || menuTooltip == null)
        {
            tooltipCoroutine = null;
            yield break;
        }

        float startAlpha = tooltipCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0f, tooltipFadeDuration);

        while (elapsed < duration)
        {
            if (tooltipCanvasGroup == null || menuTooltip == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            tooltipCanvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                0f,
                Mathf.Clamp01(elapsed / duration)
            );

            yield return null;
        }

        if (tooltipCanvasGroup != null)
            tooltipCanvasGroup.alpha = 0f;

        if (menuTooltip != null)
            menuTooltip.SetActive(false);

        tooltipCoroutine = null;
    }

    private void SetupTooltipEvents(GameObject item, RadialMenuNode node)
    {
        EventTrigger trigger =
            item.GetComponent<EventTrigger>();

        if (trigger == null)
        trigger = item.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry =
            new EventTrigger.Entry();

        enterEntry.eventID = EventTriggerType.PointerEnter;

        enterEntry.callback.AddListener((data) =>
        {
            if (!node.isDisabled)
            {
                PlaySound(hoverSound);
                ShowTooltip(node);
            }
        });

        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry =
            new EventTrigger.Entry();

        exitEntry.eventID = EventTriggerType.PointerExit;

        exitEntry.callback.AddListener((data) =>
        {
            HideTooltip();
        });

        trigger.triggers.Add(exitEntry);
    }

    private void OnMenuItemClicked(RadialMenuNode node)
    {
        if (node.isDisabled)
        return;

        if (isTransitioning)
        return;

        PlaySound(clickSound);

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
        if (isTransitioning)
        return;

        if (menuStack == null || menuStack.Count <= 1)
        return;

        StartCoroutine(TransitionBack());
    }

    private void ClearItems()
    {
        // Stop selection animations.
        foreach (var animation in selectionAnimations.Values)
        {
            if (animation != null)
            StopCoroutine(animation);
        }

        selectionAnimations.Clear();

        // Stop item entrance animations.
        foreach (Coroutine animation in itemAnimations)
        {
            if (animation != null)
            StopCoroutine(animation);
        }

        itemAnimations.Clear();

        // Destroy generated menu items.
        foreach (GameObject item in generatedItems)
        {
            if (item != null)
            Destroy(item);
        }

        generatedItems.Clear();
    }

    private IEnumerator AnimateItem(
        RectTransform item,
        Vector2 targetPosition,
        int index)
    {
        if (item == null)
            yield break;

        float elapsed = 0f;
        Vector2 startPosition = Vector2.zero;
        float startAngle = -90f;

        while (elapsed < animationDuration)
        {
            if (item == null)
                yield break;

            elapsed += Time.deltaTime;

            float t = animationDuration > 0f
                ? Mathf.Clamp01(elapsed / animationDuration)
                : 1f;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float angle = startAngle * (1f - smoothT);

            Vector2 direction =
                Quaternion.Euler(0f, 0f, angle) * targetPosition;

            item.anchoredPosition = Vector2.Lerp(
                startPosition,
                direction,
                smoothT
            );

            item.localScale = Vector3.one * smoothT;

            yield return null;
        }

        if (item == null)
            yield break;

        item.anchoredPosition = targetPosition;
        item.localScale = Vector3.one;

        UpdateSelectionVisual(index);
    }

    private IEnumerator TransitionToNode(RadialMenuNode node)
    {
        if (isTransitioning || !CanEnterNode(node))
        yield break;

        isTransitioning = true;

        HideTooltipImmediate();

        yield return StartCoroutine(AnimateItemsOut());

        if (node == null)
        {
            isTransitioning = false;
            yield break;
        }

        menuStack.Push(node);
        selectedIndex = 0;

        ShowCurrentMenu();

        isTransitioning = false;
    }

    private IEnumerator TransitionBack()
    {
        if (isTransitioning)
        yield break;

        if (menuStack == null || menuStack.Count <= 1)
        yield break;

        isTransitioning = true;

        HideTooltipImmediate();

        yield return StartCoroutine(AnimateItemsOut());

        // Check the stack again after the animation.
        if (menuStack == null || menuStack.Count <= 1)
        {
            isTransitioning = false;
            yield break;
        }

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
            if (obj == null)
                continue;

            RectTransform rect = obj.GetComponent<RectTransform>();

            if (rect == null)
                continue;

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

            float t = animationDuration > 0f
                ? Mathf.Clamp01(elapsed / animationDuration)
                : 1f;

            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;

                items[i].anchoredPosition = Vector2.Lerp(
                    startPositions[i],
                    Vector2.zero,
                    smoothT
                );

                items[i].localScale = Vector3.Lerp(
                    startScales[i],
                    Vector3.zero,
                    smoothT
                );

                items[i].localRotation =
                    Quaternion.Euler(0f, 0f, 360f * smoothT)
                    * startRotations[i];
            }

            yield return null;
        }

        ClearItems();
    }

    private bool ContainsNode(
        RadialMenuNode root,
        RadialMenuNode target)
    {
        if (root == null || target == null)
        return false;

        HashSet<RadialMenuNode> visited =
            new HashSet<RadialMenuNode>();

        Stack<RadialMenuNode> nodes =
            new Stack<RadialMenuNode>();

        nodes.Push(root);

        while (nodes.Count > 0)
        {
            RadialMenuNode current = nodes.Pop();

            if (current == null)
            continue;

            if (current == target)
            return true;

            if (!visited.Add(current))
            continue;

            if (current.children == null)
            continue;

            foreach (RadialMenuNode child in current.children)
            {
                if (child != null)
                nodes.Push(child);
            }
        }

        return false;
    }

    private bool IsNodeInMenuStack(RadialMenuNode node)
    {
        return node != null &&
        menuStack != null &&
        menuStack.Contains(node);
    }

    public bool ValidateMenuData()
    {
        if (menuData == null)
        {
            Debug.LogError("Menu Data is missing.");
            return false;
        }

        if (menuData.rootNode == null)
        {
            Debug.LogError("Root menu node is missing.");
            return false;
        }

        HashSet<RadialMenuNode> visited =
            new HashSet<RadialMenuNode>();

        return ValidateNode(menuData.rootNode, visited);
    }

    private bool ValidateNode(
        RadialMenuNode node,
        HashSet<RadialMenuNode> visited)
    {
        if (node == null)
        {
            Debug.LogError("A null menu node was found.");
            return false;
        }

        if (!visited.Add(node))
        {
            Debug.LogError(
                "Circular reference or duplicate node detected: "
                + node.menuName
            );
            return false;
        }

        if (!IsValidMenuName(node.menuName))
        {
            Debug.LogError("A menu node has an empty name.");
            return false;
        }

        if (!IsValidSize(node.itemSize, false) ||
            !IsValidSize(node.iconSize, true))
        {
            Debug.LogError(
                "Invalid size settings: " + node.menuName
            );
            return false;
        }

        if (node.children == null)
        {
            Debug.LogError(
                "Children list is null: " + node.menuName
            );
            return false;
        }

        HashSet<string> names = new HashSet<string>(
            System.StringComparer.OrdinalIgnoreCase
        );

        foreach (RadialMenuNode child in node.children)
        {
            if (child == null)
            {
                Debug.LogError(
                    "Null child found in: " + node.menuName
                );
                return false;
            }

            if (!IsValidMenuName(child.menuName))
            {
                Debug.LogError(
                    "A child node has an empty name in: "
                    + node.menuName
                );
                return false;
            }

            if (!names.Add(child.menuName))
            {
                Debug.LogError(
                    "Duplicate child name in " + node.menuName
                    + ": " + child.menuName
                );
                return false;
            }

            if (!ValidateNode(child, visited))
                return false;
        }

        return true;
    }

    private bool IsValidMenuName(string menuName)
    {
        return !string.IsNullOrWhiteSpace(menuName);
    }

    private bool IsValidSize(float value, bool allowZero)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return false;

        return allowZero ? value >= 0f : value > 0f;
    }
    private int FindFirstEnabledIndex(RadialMenuNode node)
    {
        if (node == null || node.children == null)
        return -1;

        for (int i = 0; i < node.children.Count; i++)
        {
            RadialMenuNode child = node.children[i];

            if (child != null && !child.isDisabled)
            return i;
        }

        return -1;
    }
}
