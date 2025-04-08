using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;


[XmlInclude(typeof(PropertyResult))]
[XmlInclude(typeof(ExchagePropertiesResult))]
[XmlInclude(typeof(Loop))]
[XmlInclude(typeof(CopyLastIngredientEffectsResult))]
[XmlInclude(typeof(TriggerEffectsResult))]
[XmlInclude(typeof(DominantResult))]
[XmlInclude(typeof(PreserveStirEffects))]
[XmlInclude(typeof(ExtraBrew))]
[XmlInclude(typeof(MoneyChangeResult))]
[XmlInclude(typeof(CanFireAgain))]
public abstract class Result
{
    public abstract void Apply(MixInfo mix, EffectInfo effectInfo);
}

public class PropertyResult : Result
{
    public List<Property> properties = new List<Property>();

    public Multiplier multiplier = null;

    public bool remove = false;
    public bool choosePropertyAtRandom = false;
    [ShowIf("@" + nameof(choosePropertyAtRandom) + " && " + nameof(remove))]
    public bool onlyIfContained = true;

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        int mult = Multiplier.Multiply(multiplier, 1, mix);

        for (int i = 0; i < mult; i++)
        {
            if (remove)
            {
                if (choosePropertyAtRandom)
                {
                    if (onlyIfContained) //this avoids endless loop
                    {
                        bool found = false;
                        foreach (Property p in properties)
                        {
                            if (mix.properties.Contains(p))
                                found = true;
                        }
                        if (!found)
                            return;
                    }


                    while (true)
                    {
                        Property p = properties[Random.Range(0, properties.Count)];
                        if (mix.properties.Contains(p) || !onlyIfContained)
                        {
                            mix.properties.Remove(p);
                            break;
                        }
                    } 
                    
                }
                else
                {
                    foreach (Property p in properties)
                    {
                        mix.properties.Remove(p);
                    }
                }
                
                
            }
            else
            {
                if (choosePropertyAtRandom)
                {
                    mix.properties.Add(properties[Random.Range(0, properties.Count)]);
                }
                else
                {
                    mix.properties.AddRange(properties);
                }
                
            }
        }
        
    }
}

public class ExchagePropertiesResult : Result
{
    [Tooltip("Removes all in from, and adds all in to. If onlyIfAvailable is checked then don't do this if from doesnt exist")]
    public List<Property> from = new();
    public List<Property> to = new();
    public bool onlyIfAvailable = true;
    [ShowIf(nameof(onlyIfAvailable))]
    public bool allInstances = false;

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        do
        {
            if (!onlyIfAvailable || mix.HasProperties(from))
            {
                mix.RemoveProperties(from);
                mix.AddProperties(to);
            }
            else
            {
                break;
            }

        } while (allInstances && onlyIfAvailable);


    }
}

public class Loop : Result
{
    [HideIf(nameof(condition))]
    public int numberOfTimes = 1;
    [HideIf(nameof(condition))]
    public Multiplier mult = null;
    public Condition condition = null;
    public List<Result> results = new List<Result>();
    

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        int num = numberOfTimes;
        if (mult != null)
        {
            num = mult.Multiply(num, mix);
        }

        if (condition != null)
        {
            while (condition.Check(mix))
            {
                foreach (Result result in results)
                {
                    result.Apply(mix, effectInfo);
                }
            }
        }
        else
        {
            for (int i = 0; i < num; i++)
            {
                foreach (Result result in results)
                {
                    result.Apply(mix, effectInfo);
                }
            }
        }



    }
}

public class CopyLastIngredientEffectsResult : Result
{
    public int numberOfTimes = 1;
    public Multiplier mult = null;

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        mix.AddIngredient(mix.LastAddedIngredient(), false, effectInfo);
    }

}

public class MoneyChangeResult : Result
{
    public int amount = 0;

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        OrderController.money += amount;
        if (OrderController.money < 0)
            OrderController.money = 0;
    }
}

public class TriggerEffectsResult : Result {
    public Effect.Type type;

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        var list = mix.GetEffects(type);
        mix.ApplyEffects(list, list, effectInfo, true);
        
    }
}

public class DominantResult :Result
{
    public enum OutcomeType
    {
        Double,
        Remove,
    }

    public OutcomeType type;

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        switch (type)
        {
            case OutcomeType.Double:
                {
                    Property p = mix.GetDominant();
                    if (p != Property.AnyFlavor)
                    {
                        int max = mix.properties.Count(p);
                        for (int i = 0; i < max; i++)
                        {
                            mix.properties.Add(p);
                        }
                    }
                }
                break;
            case OutcomeType.Remove:
                {
                    Property p = mix.GetDominant();
                    for (int i =0; i < mix.properties.Count; i++)
                    {
                        if (mix.properties[i] == p)
                        {
                            mix.properties.RemoveAt(i);
                            i--;
                        }
                    }
                }
                break;
        } 

    }

}

public class PreserveStirEffects : Result
{

    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        foreach (Effect e in mix.stirEffects)
        {
            e.dontRemove = true;
        }
    }
}

public class ExtraBrew : Result
{
    public int HowMany = 1;
    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        mix.numberOfBrewsRemaining += HowMany;
    }
}

public class CanFireAgain : Result
{
    public override void Apply(MixInfo mix, EffectInfo effectInfo)
    {
        mix.hasFired = false;
    }
}