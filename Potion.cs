using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class Potion : SerializedMonoBehaviour
{
    public MixInfo potionInfo = null;
    public HoldableObject holdableObject;
    public GameObject cork;

    public float fillAmount;
    public bool corked = true;
    public bool test = false;

    public List<Renderer> liquids;

    // Start is called before the first frame update
    void Start()
    {
        holdableObject = GetComponent<HoldableObject>();
        holdableObject.HoverSignal += Hover;
        if (test)
        {
            potionInfo = new MixInfo()
            {
                properties = new List<Property>() { Property.Potency },
                filled = true,
                currentCatalyst = "Remedy"
            };
        }
    }

    // Update is called once per frame
    void Update()
    {
        //if (PickupManager.instance != null && PickupManager.instance.GetCurrentLeft() == holdableObject)
        //{
        //    LeftTooltip.ShowTooltip(GetTooltip());
        //}

        if (corked)
            cork.SetActive(true);
        else
            cork.SetActive(false);

        if (potionInfo == null || potionInfo.currentCatalyst == null || fillAmount == 0f)
        {
            foreach (var r in liquids)
            {
                r.enabled = false;
            }
        }
        else
        {
            foreach (var r in liquids)
            {
                Catalyst currentCatalyst = PrefabLoader.GetCatalyst(potionInfo.currentCatalyst);
                r.material.color = potionInfo.GetColor();
                r.enabled = false;
            }
            for (int i = 0; i < liquids.Count; i++)
            {
                if (1 - fillAmount <= (float) i / liquids.Count)
                {
                    liquids[i].enabled = true;
                    break;
                }
                    
            }

        }
    }

    public void Hover(HoldableObject other)
    {
        PotionLabels.Show(potionInfo);
        /*
        if (PickupManager.instance.GetCurrentLeft() == holdableObject)
        {
            LeftTooltip.ShowTooltip(GetTooltip());
        }
        else
        {
            Tooltip.ShowTooltip(GetTooltip());
        }
        */
        
    }

    public void Fill(MixInfo mixInfo)
    {
        potionInfo = mixInfo;
        fillAmount = 1f;
        holdableObject = GetComponent<HoldableObject>();
        if (mixInfo != null) {
            
            mixInfo.SetPotionColor();
        }
            
    }

}

public enum Property{
    Potency,
    
    Bitter,
    Sweet,
    Savory,
    Spicy,
    Salty,
    Sour,

    AnyFlavor = 63,
}

/*
public class PotionInfo{
    public List<Property> properties = new();
    public Catalyst catalyst;
    public Color color;

    public MixInfo ConvertToMixinfo()
    {
        return new MixInfo()
        {
            properties = properties,
            currentCatalyst = catalyst,
            filled = true,
        };
    }
}*/
