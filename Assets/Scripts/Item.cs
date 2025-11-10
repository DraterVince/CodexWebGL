using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenuAttribute (fileName = "New Item", menuName = "Item/Create New Item")]

public class Item : ScriptableObject
{
    public int id;
    public string cardName;
    public Sprite artwork;
}