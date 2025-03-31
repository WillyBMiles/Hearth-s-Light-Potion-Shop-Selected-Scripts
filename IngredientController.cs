using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class IngredientController : SerializedMonoBehaviour
{
    [Tooltip("Each")]
    public List<IngredientSet> ingredientSets = new();

    public static IngredientController instance;


    public List<IngredientInfo> availableIngredients = new();
    public List<IngredientInfo> lastIngredients = new();
    //New ingredients today!
    public List<IngredientInfo> newIngredients = new();

    public GameObject trash;

    public int tries { get { return Mathf.Max( 10, (int) Mathf.Pow(availableIngredients.Count / 2, 2)); } }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }


    int lastDay = -1;
    // Update is called once per frame
    void Update()
    {
        
        if (DayController.Day != lastDay)
        {
            lastIngredients.Clear();
            lastIngredients.AddRange(availableIngredients);
            RecalculateIngredients();
            lastDay = DayController.Day;
            newIngredients.Clear();
            foreach (IngredientInfo info in availableIngredients)
            {
                if (!lastIngredients.Contains(info))
                    newIngredients.Add(info);
            }
            
        }
    }


    public GameObject GetRandomIngredient()
    {

        return PrefabLoader.HoldablePrefab(GetRandomIngredientInfo().prefab);
    }

    public IngredientInfo GetRandomIngredientInfo()
    {
        return GetRandomIngredientInfo(availableIngredients);
    }

    public IngredientInfo GetRandomIngredientInfo(List<IngredientInfo> list)
    {
        if (list.Count == 0)
            return null;
        IngredientInfo info = list[Random.Range(0, list.Count)];
        if (info.rarity == 1 && Random.value < .5f)
            info = list[Random.Range(0, list.Count)];
        if (info.rarity > 1 && Random.value < .8f)
            info = list[Random.Range(0, list.Count)];
        return info;
    }

    List<IngredientInfo> infos = new();
    public IngredientInfo GetRandomIngredientInfo(int minRarity, IngredientInfo.Source? source)
    {
        if (minRarity == 0 && source == null)
            return GetRandomIngredientInfo();
        infos.Clear();
        foreach (IngredientInfo info in availableIngredients)
        {
            if (info.rarity >= minRarity && (source == null || source.Value == info.source))
            {
                infos.Add(info);
            }
        }
        if (infos.Count == 0)
        {
            GetRandomIngredientInfo();
        }

        return GetRandomIngredientInfo(infos);
    }

    public void RecalculateIngredients()
    {
        int day = DayController.Day;
        int currentDay = 0;


        availableIngredients.Clear();
        foreach (IngredientSet set in ingredientSets)
        {
            if (currentDay + set.dayGap > day)
                break;
            currentDay += set.dayGap;
            set.FillInIngredientInfos(availableIngredients);


            if (set.allRemaining)
            {
                availableIngredients.Clear();
                availableIngredients.AddRange(PrefabLoader.Instance.ingredients);
                 

                break;
            }
        }

        for (int i =0; i < availableIngredients.Count; i++)
        {
            IngredientInfo info = availableIngredients[i];
            if (info.dontGenerate)
            {
                availableIngredients.RemoveAt(i);
                i--;
            }
        }
    }

    static List<Property> producers = new();
    static List<Property> consumers = new();

    static List<Property> conditionConsumers = new();
    public static int GetScore(List<IngredientInfo> infos)
    {
        producers.Clear();
        consumers.Clear();
        conditionConsumers.Clear();

        int difference = 0;
        int utility = 0;
        bool gottenPotency = false;
        foreach (IngredientInfo info in infos)
        {
            if (info == null)
                continue;
            producers.AddRange(info.producers);
            consumers.AddRange(info.consumers);
            if (info.classification == IngredientInfo.Classification.Flavor)
                difference++;
            if (info.classification == IngredientInfo.Classification.Potency)
            {
                difference--;
                gottenPotency = true;
            }
            if (info.classification == IngredientInfo.Classification.Utility)
            {
                utility++;
            }
                
        }

        int score = 0;

        foreach (Order order in OrderController.instance.futureOrders)
        {
            if (order.Conditions != null)
            {
                foreach (Condition condition in order.Conditions)
                {
                    conditionConsumers.AddRange(condition.requires);
                    if (condition.requires.Count > 0)
                    {
                        for (int i = 0; i < condition.howManyMissingRequires; i++)
                        {
                            conditionConsumers.Remove(condition.requires[Random.Range(0, condition.requires.Count)]); //remove a few random consumers
                        }
                    }
                }
            }
 
        }

        
        int missedConsumers = 0;
        int filledConsumers = 0;
        int remainingFlavors = 0;
        foreach (Property property in consumers)
        {
            if (property == Property.AnyFlavor)
            {
                remainingFlavors++;

            }
            else if (property == Property.Potency)
            {
                if (difference < 0)
                    difference++; //counts as a negative potency, if there are too many
            }
            else
            {
                if (producers.Contains(property))
                {
                    producers.Remove(property);
                    filledConsumers++;
                }
                else if (producers.Contains(Property.AnyFlavor))
                {
                    producers.Remove(Property.AnyFlavor);
                    filledConsumers++;
                }
                else
                    missedConsumers++;
            }
        }

        
        //Lose points if you can't fill condition consumers
        foreach (Property property in conditionConsumers)
        {
            if (property == Property.AnyFlavor)
            {
                if (producers.Count > 0)
                    producers.RemoveAt(0);
                else
                {
                    score -= infos.Count;
                    break;
                }
            }
            else
            {
                if (producers.Contains(property))
                {
                    producers.Remove(property);
                }
                else
                {
                    score -= infos.Count;
                    break;
                }
            }
        }

        score -= Mathf.Max(0, remainingFlavors - producers.Count); //missing "any flavor" consumers


        if (!gottenPotency) //need at least 1 potency or you lose 7 points
            score -= 7;
        score -= Mathf.Abs(difference); //lose a point for each imbalance between potency and flavor
        score -= Mathf.Max(0, producers.Count - remainingFlavors) / 2; //lose half a point for missing producers
        if (difference > 0)
        {
            score -= 2; //if there are fewer potency than flavor, lose 2 more points
        }
        score += filledConsumers - missedConsumers; //filled consumers gain a point, missed consumers lose a point

        score += utility / 2; //utility counts for 1/2 a point
        return score;
    }


    public static void GetListOfRandomIngredients(int amount, ref List<IngredientInfo> infos)
    {

        for (int i =0; i < amount; i++)
        {
            infos.Add(instance.GetRandomIngredientInfo());
        }
    }

    List<IngredientInfo> tempList = new();
    [Tooltip("Only true up to current day.")]
    public readonly List<IngredientInfo> currentIngredients = new();
    public void GetList(int amount, int tries, ref List<IngredientInfo> outList, bool forceNew = true)
    {
        outList.Clear();
        if (amount == 0)
            return;
        int bestScore = int.MinValue;
        

        currentIngredients.Clear();

        foreach (Ingredient i in Ingredient.allIngredients)
        {
            if (i.ingredientInfo != null)
                currentIngredients.Add(i.ingredientInfo);
        }

        IngredientInfo force = null;
        if (forceNew && newIngredients != null && newIngredients.Count > 0)
        {
            force = newIngredients[Random.Range(0, instance.newIngredients.Count)]; //add one random new ingredient
            amount--;
        }
        else if (forceNew)
        {
            force = availableIngredients[Random.Range(0, instance.availableIngredients.Count)]; //add one completely random ingredient instead
            amount--;
        }

        for (int i =0; i < tries; i++)
        {
            tempList.Clear();

            //add all available ingredients
            tempList.AddRange(currentIngredients);
            if (force != null)
            {
                tempList.Add(force);
            }

            GetListOfRandomIngredients(amount, ref tempList);
            int current = GetScore(tempList);

            if (current > bestScore) {
                bestScore = current;
                outList.Clear();
                outList.AddRange(tempList);
            }
        }

        foreach (IngredientInfo info in currentIngredients)
        {
            outList.Remove(info);
        }

    }


    public void GetList(int amount, ref List<IngredientInfo> outList)
    {
        GetList(amount, tries, ref outList);
    }

    List<IngredientInfo> singleTemp = new();
    public IngredientInfo GetOne(int tries)
    {
        GetList(1, tries, ref singleTemp, false);
        return singleTemp[0];
    }

    public IngredientInfo GetOne()
    {
        return GetOne(tries);
    }

}

public class IngredientSet {
    [HideIf(nameof(allRemaining))]
    public List<GameObject> ingredientPrefabs = new List<GameObject>();

    public int dayGap = 0;

   
    public bool allRemaining = false;

    public void FillInIngredientInfos(List<IngredientInfo> ingredients)
    {
        foreach (GameObject prefab in ingredientPrefabs)
        {
            if (prefab.TryGetComponent<Ingredient>(out Ingredient ingredient))
            {
                if (ingredient.ingredientInfo != null)
                {
                    ingredients.Add(ingredient.ingredientInfo);
                }
            }
        }
    }
}
