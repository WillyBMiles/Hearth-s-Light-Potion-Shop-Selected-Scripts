using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

[XmlInclude(typeof(PropertyCondition))]
[XmlInclude(typeof(OrCondition))]
[XmlInclude(typeof(AndCondition))]
[XmlInclude(typeof(HasMoney))]
[XmlInclude(typeof(DominantCondition))]
[XmlInclude(typeof(NumberOfIngredientsCondition))]
[XmlInclude(typeof(PreviousIngredients))]
[XmlInclude(typeof(SourceComparisonCondition))]
public abstract class Condition
{
    

    public bool showExtra = false;

    [ShowIf(nameof(showExtra))]
    public string Description;

    [ShowIf(nameof(showExtra))]
    public float difficulty = 1f;

    [Space(20)]
    [ShowIf(nameof(showExtra))]
    public List<Property> requires = new();

    [ShowIf(nameof(showExtra))]
    public List<Property> excludes = new();

    [ShowIf(nameof(showExtra))]
    [Tooltip("Requires a specific number of something")]
    public bool requiresNumber = false;

    [ShowIf(nameof(showExtra))]
    public bool requiresUnique = false;

    [ShowIf(nameof(showExtra))]
    [Tooltip("How many missing requires can you have before this is no longer allowed")]
    public int howManyMissingRequires = 0;

    [ShowIf(nameof(showExtra))]
    public int minDay = 0;

    public abstract bool Check(MixInfo mix);

    public bool CanDoWithAvailable()
    {
        if (DayController.Day < minDay)
            return false;
        int missingRequires = 0;
        foreach (Property p in requires)
        {
            bool found = false;
            foreach (IngredientInfo ingredient in IngredientController.instance.availableIngredients)
            {
                if (ingredient.producers.Contains(p))
                {
                    found = true;
                    break;
                }
                    
            }
            if (!found)
                missingRequires++;
        }
        return missingRequires <= howManyMissingRequires;
        
    }

    public bool CanOverlap(Condition condition)
    {
        if (requiresNumber && condition.requiresNumber)
            return false;
        if (requiresUnique && condition.requiresUnique)
            return false;
        foreach (Property property in requires)
        {
            if (condition.excludes.Contains(property))
                return false;
        }
        foreach (Property property in condition.requires)
        {
            if (excludes.Contains(property))
                return false;
        }
        return true;
    }

}

public class PropertyCondition : Condition
{
    public List<Property> properties = new();
    public enum Operation { 
        GreaterThan,
        EqualTo,
        LessThan,
    }
    [Space(20)]
    public Operation operation = Operation.EqualTo;
    public int number;
    public Multiplier multiplier = null;
    public bool checkLastIngredientInstead = false;

    [Tooltip("Use for \"3 different flavors\" type ")]
    public bool OnePerType = false;




    public override bool Check(MixInfo mix)
    {
        int count = 0;
        List<Property> checkProperties = checkLastIngredientInstead ?
            mix.LastAddedIngredient().baseProperties
            : mix.properties;

        foreach (Property p in properties)
        {
            count += OnePerType ? (checkProperties.Contains(p) ? 1 : 0) : checkProperties.Count(p) ;
        }

        int actualNumber = Multiplier.Multiply(multiplier, number, mix);

        return operation switch
        {
            Operation.EqualTo => count == actualNumber,
            Operation.GreaterThan => count > actualNumber,
            Operation.LessThan => count < actualNumber,
            _ => throw new System.NotImplementedException(),
        };
    }
}

public class OrCondition : Condition
{
    public List<Condition> conditions = new List<Condition>();

    public override bool Check(MixInfo mix)
    {
        bool or = false;
        foreach (Condition condition in conditions)
        {
            or = or || condition.Check(mix);
        }
        return or;
    }
}

public class AndCondition : Condition
{
    public List<Condition> conditions = new List<Condition>();

    public override bool Check(MixInfo mix)
    {
        bool and = true;
        foreach (Condition condition in conditions)
        {
            and = and && condition.Check(mix);
        }
        return and;
    }
}

public class HasMoney : Condition
{

    public int amount = 0;
    public override bool Check(MixInfo mix)
    {
        return OrderController.money >= amount;
    }
}

public class DominantCondition : Condition
{
    public Property property;
    public bool invert;

    public override bool Check(MixInfo mix)
    {
        if (mix.GetDominant() == property)
            return !invert;
        return invert;
    }
}

public class NumberOfIngredientsCondition : Condition
{
    public Sign sign;
    public int number;

    public override bool Check(MixInfo mix)
    {
        int ingredientsNumber = mix.containedIngredients.Count - 1;
        switch (sign)
        {
            case Sign.EqualTo:
                return ingredientsNumber == number;
            case Sign.GreaterThan:
                return ingredientsNumber > number;
            case Sign.GreaterThanOrEqualTo:
                return ingredientsNumber >= number;
            case Sign.LessThan:
                return ingredientsNumber < number;
            case Sign.LessThanOrEqualTo:
                return ingredientsNumber <= number;
        }
        throw new System.Exception("Number of ingredients not covering sign cases");
    }
}

public class PreviousIngredients : Condition
{

    public enum CompareType
    {
        SharedFlavor
    }

    public int number = 2;
    public CompareType type;

    static List<IngredientInfo> infos = new();
    static List<Property> properties = new();
    public override bool Check(MixInfo mix)
    {
        infos ??= new();
        infos.Clear();
        if (mix.containedIngredients.Count < number)
            return false;
        for (int i = mix.containedIngredients.Count - 1; i >= mix.containedIngredients.Count - number; i--)
        {
            infos.Add(mix.containedIngredients[i]);
        }

        switch (type)
        {
            case CompareType.SharedFlavor:
                properties ??= new();
                properties.Clear();
                properties.AddRange(infos[0].baseProperties);
                properties.RemoveDuplicates();
                for (int i = 1; i < infos.Count; i++)
                {
                    for (int j = 0; j < properties.Count; j++)
                    {
                        if (!infos[i].baseProperties.Contains(properties[j]))
                        {
                            properties.RemoveAt(j);
                            j--;
                        }
                    }
                }
                return properties.Count > 0;

                

        }

        return false;
    }

}

public class SourceComparisonCondition : Condition
{
    public IngredientInfo.Source source;

    public bool lastIngredient = true;

    public override bool Check(MixInfo mix)
    {
        if (mix.LastAddedIngredient() == null)
            return false;
        return mix.LastAddedIngredient().source == source;
    }
}
