using UnityEngine;

public class ItemBase
{
    public string itemName;
    public int itemID;
    public int itemNumber;
    public Sprite itemIcon;

    public virtual void UseItem() // 虚方法，允许子类重写
    {
        Debug.Log("Using item: " + itemName);
    }
}