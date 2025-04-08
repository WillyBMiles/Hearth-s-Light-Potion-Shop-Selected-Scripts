using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using UnityEngine;



[XmlRoot]
public class Effect
{
    public enum Type
    {
        Fire,
        Cauldron,
        Add,
        Brew,
        Stir
    }

    [Tooltip("For cauldron effects, happens before default and add effects. Otherwise happens before all other effects of its type.")]
    public bool early;

    [Tooltip("Specifically for fire and stir effects. Won't be automatically removed.")]
    public bool dontRemove;
    public bool cantBeRepeated;

    public string description;

    public List<Condition> conditions = new();
    public List<Result> results = new();

    [Tooltip("Negative means infinite")]
    public int TimesItCanOccur = -1;
    [Tooltip("Hide times can occur")]
    public bool hideTimesOccured = false;
    [Tooltip("Check times it can occur before conditions, otherwise only iterate")]
    public bool checkBeforeCondition = false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cauldron"></param>
    /// <returns>True if should be removed.</returns>
    public bool Apply(MixInfo mix, EffectInfo effectInfo)
    {
        bool cont = true;
        foreach (Condition condition in conditions)
        {
            cont = cont && condition.Check(mix);
        }

        bool ret = false;
        if (checkBeforeCondition && TimesItCanOccur >= 0)
        {
            TimesItCanOccur--;
            if (TimesItCanOccur <= 0)
            {
                ret = true;
            }
        }

        if (!cont)
            return ret;

        foreach (Result result in results)
        {
            result.Apply(mix, effectInfo);
        }


        if (!checkBeforeCondition && TimesItCanOccur >= 0)
        {
            TimesItCanOccur--;
            if (TimesItCanOccur <= 0)
            {
                ret = true;
            }
        }

        return ret;
    }

    public static void CopyInto(List<Effect> destination, List<Effect> source)
    {
        foreach (Effect e in source)
        {
            destination.Add(e.Duplicate());
        }
    }

    public Effect Duplicate()
    {
        Effect newEffect = (Effect)MemberwiseClone();

        return newEffect;
    }

    public static string ListEffects(List<Effect> effects, bool showCurrentValue)
    {
        string output = "";
        foreach (Effect e in effects)
        {
            output += "\n" + e.description;
            if (showCurrentValue && e.TimesItCanOccur >= 0 && !e.hideTimesOccured)
            {
                if (e.TimesItCanOccur == 1)
                    output += "(" + e.TimesItCanOccur + " use)";
                else 
                    output += "(" + e.TimesItCanOccur + " uses)";
            }
        }
        return output;
        
    }

}


