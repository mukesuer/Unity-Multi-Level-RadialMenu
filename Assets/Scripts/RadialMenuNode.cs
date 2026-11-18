using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RadialMenuNode
{
    public string menuName;

    public List<RadialMenuNode> children = new List<RadialMenuNode>();

    public bool HasChildren
    {
        get { return children != null && children.Count > 0; }
    }
}