using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RadialMenuNode
{
    public string menuName;
    public Sprite menuIcon;
    public float iconSize = 48f;//default
    public Color itemColor = Color.white;
    public bool isDisabled = false;
    public float itemSize = 90f;
    public List<RadialMenuNode> children = new List<RadialMenuNode>();

    public bool HasChildren
    {
        get { return children != null && children.Count > 0; }
    }
}