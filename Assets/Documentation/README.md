# Radial Menu Framework

A reusable, data-driven multi-level radial menu framework for Unity.

## Features

- Data-driven menu structure using ScriptableObject
- Support for multiple menu levels
- Circular menu item layout
- Mouse hover and click interactions
- Keyboard navigation
- Disabled menu items
- Center button navigation
- Smooth menu transitions and animations
- Item selection highlighting and scaling
- Tooltip with fade animation
- Audio feedback
- Runtime menu item management
- Menu data validation
- Reusable Prefab-based architecture

## Requirements

- Unity 2022.3 LTS or later
- TextMeshPro
- Unity UI

## Installation

1. Import the framework package into your Unity project.
2. Locate `RadialMenuRoot_Template.prefab`.
3. Drag the Prefab into your Canvas.
4. Create a RadialMenuData asset.
5. Assign the data asset to the Menu Data field.
6. Configure the menu item Prefab and UI references.
7. Enter Play Mode.

## Creating Menu Data

1. Right-click in the Project window.
2. Select Create → Radial Menu → Menu Data.
3. Configure the root node and its children.
4. Assign icons, descriptions, colors, and item sizes.
5. Assign the asset to RadialMenuController.

## Controls

| Input | Action |
|---|---|
| Mouse Hover | Select an item |
| Left Click | Activate an item |
| Arrow Keys | Navigate between items |
| Enter | Confirm selection |
| Escape | Return to the previous menu |

## Runtime API

The framework supports runtime menu operations:

- AddMenuItem
- RemoveMenuItem
- UpdateMenuItem

See `RadialMenuController.cs` for method signatures and usage details.

## Project Structure

```text
Assets/
├── Scripts/
│   ├── RadialMenuController.cs
│   ├── RadialMenuNode.cs
│   └── RadialMenuData.cs
├── Prefabs/
│   ├── RadialMenuRoot.prefab
│   ├── RadialMenuRoot_Template.prefab
│   └── ItemButton_01.prefab
├── Demo/
│   ├── Data/
│   ├── Tests/
│   └── Legacy/
├── Scenes/
├── Art/
└── Documentation/
    └── README.md
```

## License

Choose and include an appropriate license before distributing this framework publicly.