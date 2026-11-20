using UnityEngine;

[CreateAssetMenu(
    fileName = "NewRadialMenu",
    menuName = "Radial Menu/Menu Data"
)]
public class RadialMenuData : ScriptableObject
{
    public RadialMenuNode rootNode = new RadialMenuNode
    {
        menuName = "Main Menu"
    };
}
