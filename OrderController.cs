using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Sirenix.OdinInspector;

public class OrderController : SerializedMonoBehaviour
{
    public static OrderController instance { get { if (_instance == null) _instance = FindObjectOfType<OrderController>(); return _instance; }
        private set { _instance = value; } }
    static OrderController _instance;

    public List<Catalyst> currentCatalysts = new();
    public List<Condition> AvailableConditions = new();

    public List<Character> AvailableCharacters = new();

    [HideInInspector]
    public List<Order> currentOrders = new List<Order>();
    [HideInInspector]
    public List<Order> futureOrders { get; set; } = new List<Order>();
    [HideInInspector]
    public List<Order> successfulOrders = new List<Order>();
    [HideInInspector]
    public List<Order> unsuccessfulOrders = new List<Order>();

    public Animator doorAnimator;

    int _money = 10;
    int _reputation = 0;

    public static int money { get { return instance._money; } set { instance._money = value; } }
    public static int reputation { get { return instance._reputation; } set { instance._reputation = value; } }


    private void Start()
    {
        instance = this;

        //Invoke(nameof(NewVisitor), 10f); //Test
    }

    public int lastMoney = 10;
    public int lastReputation = 0;
    public void SetStarting(int money, int reputation)
    {
        OrderController.money = money;
        lastMoney = money;
        OrderController.reputation = reputation;
        lastReputation = reputation;
    }

    public void GetRevisiting(int Day, ref List<Character> characters)
    {
        foreach (Order order in futureOrders)
        {
            if (!order.specialOrder && order.StartingDay + order.Time <= Day)
            {
                characters.Add(Character.GetCharacter(order.CharacterID));
            }
        }
    }

    public Order GenerateOrder(Character character, int numberOfConditions, int difficulty, int Time, float priceMult, bool addPreference, Color ribbonColor, Catalyst catalyst)
    {
        if (DayController.instance.tutorialOrders.Count > 0)
        {
            Order order = DayController.instance.tutorialOrders[0];
            order.CharacterID = Character.GetID(character);
            DayController.instance.tutorialOrders.RemoveAt(0);
            character.currentOrder = order;
            order.ribbonColor = ribbonColor;
            currentOrders.Add(order);
            order.StartingDay = DayController.Day;
            return order;
            
        }

        if (character.relationshipContainer.relationship < -3 && Time == 0)
        {
            difficulty++;
        }
        if (character.relationshipContainer.relationship < -6 && Time > 0)
        {
            difficulty++;
        }
        Order newOrder = new()
        {
            CatalystID = PrefabLoader.GetCatalystID(catalyst),
            CharacterID = Character.GetID(character),

            MinPotency = difficulty,
            Time = Time,
            StartingDay = DayController.Day,
        };
        float Payout =  difficulty * Random.Range(5, 7) + Random.Range(4, 10);
        float PayoutPerPotency = Random.Range(4, 6);


        for (int i = 0; i < numberOfConditions; i++)
        {
            Condition condition = FindCondition(AvailableConditions, newOrder.Conditions.ToArray());
            newOrder.Conditions.Add(condition);
            PayoutPerPotency = condition.difficulty * PayoutPerPotency;
        }
        if (addPreference)
        {
            newOrder.Conditions.Add(character.Preference);
            PayoutPerPotency = character.Preference.difficulty * PayoutPerPotency;
            Payout *= 1 + character.tip;
        }

        Payout = priceMult * Mathf.Max(1, (int)(Payout * catalyst.payoutMult));
        PayoutPerPotency = priceMult * Mathf.Max(1, (int)(PayoutPerPotency * catalyst.payoutMult));

        newOrder.Payout = (int)Payout;
        newOrder.PayoutPerPotency = (int)PayoutPerPotency;

        character.currentOrder = newOrder;

        newOrder.ribbonColor = ribbonColor;

        currentOrders.Add(newOrder);
        return newOrder;
    }


    public Order GenerateDefaultOrder(Character character, int addDifficulty, int Time, float priceMult, Color ribbonColor, Catalyst catalyst)
    {
        int conditions = Time ==0 ? DifficultyScaling.instance.difficulty.GetNumber(Difficulty.Key.ImmediateNumberOfConditions) : DifficultyScaling.instance.difficulty.GetNumber(Difficulty.Key.FutureNumberOfConditions);
        int minPotency = Time == 0 ? DifficultyScaling.instance.difficulty.GetNumber(Difficulty.Key.MinPotencyImmediate) : DifficultyScaling.instance.difficulty.GetNumber(Difficulty.Key.MinPotencyFuture);
        return GenerateOrder(character, conditions, minPotency + addDifficulty, Time, priceMult,
            false, ribbonColor, catalyst);
    }

    readonly List<Condition> tempConditions = new();
    public Condition FindCondition(List<Condition> possibleConditions, params Condition[] otherConditions)
    {
        //This ensures the condition has some chance of working
        if (possibleConditions.Count == 0 )
            return null;

        tempConditions.Clear();
        tempConditions.AddRange(possibleConditions);
        foreach (Condition c in otherConditions)
        {
            tempConditions.Remove(c);
        }
        for (int i =0; i < tempConditions.Count; i++)
        {
            if (!tempConditions[i].CanDoWithAvailable())
            {
                tempConditions.RemoveAt(i);
                i--;
            }
            else {
                foreach (Condition con in otherConditions)
                {
                    if (!con.CanOverlap(tempConditions[i]))
                    {
                        tempConditions.RemoveAt(i);
                        i--;
                    }
                        
                }
            }
        }
        if (tempConditions.Count == 0)
            return possibleConditions[0];
        
        Condition condition;
        condition = tempConditions[Random.Range(0, tempConditions.Count)];
        return condition;

    }


    private void Update()
    {
        //Order management
        for (int i = 0; i < futureOrders.Count; i++)  
        {
            if (futureOrders.Count < i + 1)
            {
                break;
            }
            Order o = futureOrders[i];
            Character character = Character.GetCharacter(o.CharacterID);
            if (character.currentOrder != o) //if (o.Time + o.StartingDay < DayController.Day || character.currentOrder != o)
            {
                ArchiveOrder(o, false);
                i--;
                //if (character.currentOrder == o)
                //    character.currentOrder = null;
            }
        }

        if (money != lastMoney)
        {
            PopupManager.Coins(money - lastMoney);
            lastMoney = money;
        }
        if (reputation != lastReputation)
        {
            PopupManager.Reputation(reputation - lastReputation);
            lastReputation = reputation;
        }
    }
    
    public Character GetRandomCharacter(bool doesntHaveOrder, List<Character> DayCharacters)
    {
        if (DayCharacters.Count >= AvailableCharacters.Count || (currentOrders.Count >= AvailableCharacters.Count && doesntHaveOrder))
        {
            return AvailableCharacters[Random.Range(0, AvailableCharacters.Count)];
        }

        Character character;
        do
        {
            character = AvailableCharacters[Random.Range(0, AvailableCharacters.Count)];
        } while (DayCharacters.Contains(character) || (doesntHaveOrder && CheckHasAnOrder(character)));
        return character;
        
    }

    bool CheckHasAnOrder(Character character)
    {
        foreach (Order order in currentOrders)
        {
            Character c = Character.GetCharacter(order.CharacterID);
            if (c == character)
                return true;
        }
        return false;
    }

    public void ArchiveOrder(Order order, bool successful)
    {
        currentOrders.Remove(order);
        futureOrders.Remove(order);
        if (successful)
        {
            successfulOrders.Add(order);
            DifficultyScaling.instance.AddSuccess();
        }
        else
        {
            unsuccessfulOrders.Add(order);
            DifficultyScaling.instance.AddFailed();
        }
    }

    #region Animation and Sound

    public void PlayDoorAnimation()
    {
        doorAnimator.SetTrigger("Open");
        Invoke(nameof(PlayBellSound), .25f);

    }
    public void PlayBellSound()
    {
        SoundManager.PlaySound("bell", .5f, doorAnimator.transform.position);
    }
    #endregion

    #region Sunsetted system
    /*   
     *   
     *   void StartCharacter(Character character, DayController.VisitorType visitorType)
    {
        Invoke(nameof(ShowCharacter), .25f);
        doorAnimator.SetTrigger("Open");

    }
     *   void StartCharacter(Character character, DayController.VisitorType visitorType)
    {
        Invoke(nameof(ShowCharacter), .25f);
        doorAnimator.SetTrigger("Open");

    }
     *   
     *       void ShowCharacter()
    {
        currentCharacter.Enter();
        SoundManager.PlaySound("bell", .5f, doorAnimator.transform.position);
    }
    void LeaveCharacter()
    {
        currentCharacter.Leave();
        currentCharacter = null;
        leaving = false;
        
    }
     *       public void NewImmediateOrder(Character character)
    {
        Order order = GenerateOrder(character, 0, 1, 0);
        order.ribbonColor = Color.blue;
        currentOrders.Add(order);
        StartCharacter(character, DayController.VisitorType.ImmediateOrder);
        character.showOrder = true;
    }

    
    public void NewFutureOrder(Character character)
    {
        Order order = GenerateOrder(character, 1, Random.Range(2,4),1);
        order.ribbonColor = Color.red;
        currentOrders.Add(order);
        StartCharacter(character, DayController.VisitorType.FutureOrder);
        character.showOrder = true;
    }
     *   
     *   public void RevisitOrder(Character character)
    {
        StartCharacter(character, DayController.VisitorType.Revisit);
        character.showOrder = true;
    }
     * public void Extra(Character character, DayController.VisitorType visitorType)
    {
        StartCharacter(character, visitorType);
        character.ExtraSignal?.Invoke();
        character.showOrder = false;
    }
     * */
    #endregion

}
