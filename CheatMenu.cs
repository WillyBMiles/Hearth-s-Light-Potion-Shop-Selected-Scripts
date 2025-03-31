using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheatMenu : MonoBehaviour
{
    public GameObject pool;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F5))
            pool.SetActive(!pool.activeInHierarchy);
#endif
    }

    public void AddGold(int amount)
    {
        OrderController.money += amount;
        if (OrderController.money < 0)
        {
            OrderController.money = 0;
        }
    }

    public void AddReputation(int amount)
    {
        OrderController.reputation += amount;
        if (OrderController.reputation < 0)
        {
            OrderController.reputation = 0;
        }
    }

    public void AddPotency()
    {
        CauldronManager cm = FindObjectOfType<CauldronManager>();
        if (cm != null && cm.mixInfo != null)
        {
            cm.mixInfo.properties.Add(Property.Potency);
        }
    }

    public void RemoveFlavor()
    {
        CauldronManager cm = FindObjectOfType<CauldronManager>();
        if (cm != null && cm.mixInfo != null && cm.mixInfo.properties.Count > 0)
        {
            cm.mixInfo.properties.Sort();
            cm.mixInfo.properties.RemoveAt(cm.mixInfo.properties.Count - 1);
        }
    }
    public void AddProperty(Property p)
    {
        CauldronManager cm = FindObjectOfType<CauldronManager>();
        if (cm != null && cm.mixInfo != null)
        {
            cm.mixInfo.properties.Add(p);
        }
    }

    public void AddRandomFlavor()
    {
        AddProperty(new[] { Property.Bitter, Property.Salty, Property.Savory, Property.Sour, Property.Spicy, Property.Sweet }[Random.Range(0, 6)]);
    }


    public void ChangeDay(int amount)
    {
        DayController.Day += amount;
    }

    public void Relationship(int amount)
    {
        foreach (Character c in Character.characters.Values)
        {
            c.relationshipContainer.ChangeRelationship(amount);
        }
    }
}
