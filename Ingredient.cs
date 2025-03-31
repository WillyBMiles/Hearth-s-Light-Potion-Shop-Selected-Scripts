using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Xml.Serialization;

public class Ingredient : SerializedMonoBehaviour
{
    public IngredientInfo ingredientInfo = new();
    public HoldableObject holdable;

    public static List<Ingredient> allIngredients = new List<Ingredient>();

    

    // Start is called before the first frame update
    void Start()
    {
        holdable = GetComponent<HoldableObject>();
        holdable.HoverSignal += Hover;
        ingredientInfo.Initialize();
        ingredientInfo.CreationDay = DayController.Day;
        allIngredients.Add(this);
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PickupManager.instance != null && ingredientInfo != null)
        {
            if (PickupManager.instance.GetCurrentLeft() == holdable)
            {
                ingredientInfo.ShowTooltip(true);
            }
        }

       
    }

    private void OnDestroy()
    {
        allIngredients.Remove(this);
    }

    public void ConsumeIngredient()
    {
        ingredientInfo = null;
        Invoke(nameof(Consume),.2f);
    }

    void Consume()
    {
        PickupManager pm = PickupManager.instance;
        if (pm != null && pm.leftHeld.Contains(holdable))
        {
            pm.RemoveIngredientShift(pm.leftHeld.IndexOf(holdable) != pm.leftHeld.Count - 1);
            pm.CleanObject(holdable);
        }
        
        
        Destroy(gameObject);
        
    }

    void Hover(HoldableObject held)
    {
        if (ingredientInfo != null)
        {
            ingredientInfo.ShowTooltip(false);
            /*if (PickupManager.instance.GetCurrentLeft() == holdable){
                LeftTooltip.ShowTooltip(tooltip);
            }
            else
            {
                Tooltip.ShowTooltip(tooltip);
            }*/
            
        }
        

    }

    void EndDay()
    {
        if (ingredientInfo != null && holdable != null && ingredientInfo.IsPerishable(holdable.currentSpot))
        {
            if (holdable.currentSpot != null)
            {
                GameObject trash = Instantiate(IngredientController.instance.trash);
                HoldableObject ho = trash.GetComponent<HoldableObject>();
                holdable.currentSpot.PlaceObject(ho);
            }
            
            
            Destroy(gameObject);
            allIngredients.Remove(this);
        }
    }

    static List<Ingredient> tempIngredients = new List<Ingredient>();
    public static void EndDayAll()
    {
        tempIngredients.Clear();
        tempIngredients.AddRange(allIngredients);
        foreach (Ingredient ingredient in tempIngredients)
        {
            if (ingredient != null)
            {
                ingredient.EndDay();
            }
            
        }

    }
    
    public static bool AnyPerishables()
    {
        while (allIngredients.Contains(null))
            allIngredients.Remove(null);
        foreach (Ingredient ingredient in allIngredients)
        {
            if (ingredient.ingredientInfo.IsPerishable(ingredient.holdable.currentSpot))
                return true;
        }
        
        return false;
    }


}

[XmlRoot]
public class IngredientInfo {
    public string name;
    [Tooltip("Rarity 0 are basic ingredients.")]
    public int rarity = 0;
    [TextArea(5,20)]
    [Tooltip("Use this to describe base results")]
    public string description = "";

    public bool Perishable = true;

    [HideInInspector]
    public int CreationDay = 0;

    public enum TypeOfIngredient
    {
        Ingredient,
        Catalyst,
    }

    public enum Source { 
        Plant,
        Animal, 
        Mineral,
        Liquid,
        Fungus,
        Other = 999
    }
    public TypeOfIngredient type;
    public Source source;
    public bool dontGenerate;

    public List<Property> baseProperties = new();

    public List<Result> baseResults = new();

    public List<Effect> addEffects = new();

    public List<Effect> cauldronEffects = new();

    public List<Effect> fireEffects = new();

    public List<Effect> brewEffects = new();

    public List<Effect> stirEffects = new();

    public List<Effect> timeEffects = new();

    public float labelRotation { get { if (_labelRotation == float.PositiveInfinity) Initialize(); return _labelRotation; } }
    public Color labelColor { get { if (_labelColor == Color.white) Initialize(); return _labelColor; } }
    public float _labelRotation = float.PositiveInfinity;
    public Color _labelColor = Color.white;

     public void Initialize()
    {
        _labelRotation = UnityEngine.Random.Range(-6f, 6f);
        _labelColor = Color.Lerp(UnityEngine.Random.ColorHSV(), Color.black, .85f);
    }

    public string GetDescription(bool abridge, bool hideName = false)
    {
        string output = "";
        if (!hideName)
        {
            output = "<size=130%><smallcaps><font-weight=\"800\">" + name + "</font-weight></smallcaps>";
        }
        if (type != TypeOfIngredient.Catalyst)
        {
            output += TextController.IngredientSourceIcon(source);
        }
        switch (type)
        {
            case TypeOfIngredient.Ingredient:
                //output += "$INGREDIENT";

                output += rarity switch
                {
                    0 => "$BASIC",
                    1 => "$RARE",
                    _ => "$VERYRARE",
                };
                break;
            case TypeOfIngredient.Catalyst:
                output += "$CATALYST";
                break;
        }


        output += "</size><size=150%>"
            + StringifyProperties(baseProperties) + "</size>";
        if (description.Length > 0f)
        {
            output += "\n" + description + "\n";
        }


        if (addEffects.Count > 0)
        {
            output += "\n$_\n<size=120%>$A</size>";
        }
        if (!abridge)
            output += Effect.ListEffects(addEffects, true);
        else
            output += "...";

        if (stirEffects.Count > 0)
        {
            output += "\n$_\n<size=120%>$S</size>";
        }
        if (!abridge)
            output += Effect.ListEffects(stirEffects, true);
        else
            output += "...";

        if (cauldronEffects.Count > 0)
        {
            output += "\n$_\n<size=120%>$C</size>";
        }
        if (!abridge)
            output += Effect.ListEffects(cauldronEffects, true);
        else
            output += "...";

        if (fireEffects.Count > 0)
        {
            output += "\n$_\n<size=120%>$F</size>";
        }
        if (!abridge)
            output += Effect.ListEffects(fireEffects, true);
        else
            output += "...";

        if (brewEffects.Count > 0)
        {
            output += "\n$_\n<size=120%>$R</size>";
        }
        if (!abridge)
            output += Effect.ListEffects(brewEffects, true);
        else
            output += "...";

        if (timeEffects.Count > 0)
        {
            output += "\n$_\n<size=120%>$T</size>";
        }
        if (!abridge)
            output += Effect.ListEffects(timeEffects, true);
        else
            output += "...";

        if (Perishable)
        {
            output += "\n$_\nPerishable";
        }

        return output;
    }

    public bool IsPerishable(PlacementSpot spot)
    {
        if (!Perishable)
            return false;
        if (spot == null || !spot.dontPerish)
            return true;
        return false;
    }

    public static string StringifyProperties(List<Property> properties)
    {
        properties.Sort();
        string potencyString = "";
        string propertyString = "";

        int flavors = 0;
        foreach (Property p in properties)
        {
            if (p == Property.Potency)
            {
                potencyString += TextController.PropertyIcon(Property.Potency);
            }
            else
            {
                if (flavors > 7)
                {
                    propertyString += "<br>";
                    flavors = 0;
                }
                propertyString += TextController.PropertyIcon(p);
                flavors += 1;

            }
        }
        string output = "";
        if (potencyString.Length > 0)
        {
            output += "\n" + potencyString;
        }
        if (propertyString.Length > 0)
        {
            output += "\n" + propertyString;
        }
       
        return output;
    }

    public void ShowTooltip(bool corner)
    {
        IngredientLabel.Show(GetDescription(false), labelColor, labelRotation, corner);
    }
    public void AllEffects(ref List<Effect> effects)
    {
        effects.Clear();

        effects.AddRange(addEffects);
        effects.AddRange(cauldronEffects);
        effects.AddRange(fireEffects);
        effects.AddRange(brewEffects);
        effects.AddRange(stirEffects);
        effects.AddRange(timeEffects);
    }

    [HideInInspector]
    public string prefab;

    [Space(24f)]
    public List<Property> producers = new();
    public List<Property> consumers = new();

    public enum Classification
    {
        Flavor,
        Potency,
        Utility
    }
    public Classification classification;
}