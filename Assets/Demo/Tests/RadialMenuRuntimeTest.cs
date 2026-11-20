using UnityEngine;

public class RadialMenuRuntimeTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RadialMenuController menuController;

    [SerializeField]
    private RadialMenuData menuData;

    private RadialMenuNode runtimeItem;

    private void Update()
    {
        if (!ReferencesAreValid())
            return;

        if (Input.GetKeyDown(KeyCode.F1))
            AddTestItem();

        if (Input.GetKeyDown(KeyCode.F2))
            RemoveTestItem();

        if (Input.GetKeyDown(KeyCode.F3))
            UpdateTestItem();

        if (Input.GetKeyDown(KeyCode.F4))
            AddInventoryItem();

        if (Input.GetKeyDown(KeyCode.F5))
            AddWeaponsItem();
    }

    private bool ReferencesAreValid()
    {
        if (menuController == null || menuData == null ||
            menuData.rootNode == null)
        {
            return false;
        }

        return true;
    }

    private void AddTestItem()
    {
        if (runtimeItem != null)
        {
            Debug.Log("Test item already exists.");
            return;
        }

        runtimeItem = new RadialMenuNode
        {
            menuName = "Runtime Item",
            description = "This item was created at runtime.",
            itemSize = 90f,
            iconSize = 48f,
            itemColor = Color.cyan
        };

        bool success = menuController.AddMenuItem(
            menuData.rootNode,
            runtimeItem
        );

        Debug.Log(success
            ? "Runtime item added."
            : "Failed to add runtime item.");

        if (!success)
            runtimeItem = null;
    }

    private void RemoveTestItem()
    {
        if (runtimeItem == null)
            return;

        bool success = menuController.RemoveMenuItem(
            menuData.rootNode,
            runtimeItem
        );

        Debug.Log(success
            ? "Runtime item removed."
            : "Failed to remove runtime item.");

        if (success)
            runtimeItem = null;
    }

    private void UpdateTestItem()
    {
        if (runtimeItem == null)
        {
            Debug.Log("Add the test item first.");
            return;
        }

        RadialMenuNode updatedData = new RadialMenuNode
        {
            menuName = "Updated Item",
            description = "This item has been updated.",
            itemSize = 110f,
            iconSize = 48f,
            itemColor = Color.green,
            isDisabled = false
        };

        bool success = menuController.UpdateMenuItem(
            menuData.rootNode,
            runtimeItem,
            updatedData
        );

        Debug.Log(success
            ? "Runtime item updated."
            : "Failed to update runtime item.");
    }

    private void AddInventoryItem()
    {
        RadialMenuNode inventory =
            menuData.rootNode.children.Find(
                node => node != null &&
                string.Equals(
                    node.menuName,
                    "Inventory",
                    System.StringComparison.OrdinalIgnoreCase
                )
            );

        if (inventory == null)
        {
            Debug.LogWarning("Inventory not found.");
            return;
        }

        RadialMenuNode newItem = new RadialMenuNode
        {
            menuName = "Materials",
            description = "Runtime inventory materials.",
            itemSize = 90f,
            iconSize = 48f,
            itemColor = Color.yellow
        };

        bool success = menuController.AddMenuItem(
            inventory,
            newItem
        );

        Debug.Log(success
            ? "Materials added to Inventory."
            : "Failed to add Materials.");
    }

    private void AddWeaponsItem()
    {
        RadialMenuNode inventory =
            menuData.rootNode.children.Find(
                node => node != null &&
                string.Equals(
                    node.menuName,
                    "Inventory",
                    System.StringComparison.OrdinalIgnoreCase
                )
            );

        if (inventory == null || inventory.children == null)
        {
            Debug.LogWarning("Inventory not found.");
            return;
        }

        RadialMenuNode weapons =
            inventory.children.Find(
                node => node != null &&
                string.Equals(
                    node.menuName,
                    "Weapons",
                    System.StringComparison.OrdinalIgnoreCase
                )
            );

        if (weapons == null)
        {
            Debug.LogWarning("Weapons not found.");
            return;
        }

        RadialMenuNode newItem = new RadialMenuNode
        {
            menuName = "Daggers",
            description = "Runtime weapon daggers.",
            itemSize = 90f,
            iconSize = 48f,
            itemColor = Color.red
        };

        bool success = menuController.AddMenuItem(
            weapons,
            newItem
        );

        Debug.Log(success
            ? "Daggers added to Weapons."
            : "Failed to add Daggers.");
    }
}