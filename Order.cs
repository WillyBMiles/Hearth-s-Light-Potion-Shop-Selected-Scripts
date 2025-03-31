using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

[XmlRoot]
public class Order 
{
    public string CatalystID;
    public string CharacterID;
    [Tooltip("In days, 0 is allowed.")]
    public int Time;
    public int StartingDay;
    public int MinPotency;
    public List<Condition> Conditions = new List<Condition>();
    public int Payout;
    public int PayoutPerPotency;

    public Color ribbonColor = Color.red;
    public Color mixColor = Color.black;

    public string payoutOverwrite = "";

    public bool specialOrder = false;

    public string Description(bool sayDueDate = false, bool sayCharacter = false)
    {
        Character character = Character.GetCharacter(CharacterID);
        string r = "";
        if (sayCharacter)
        {
            r += "Order for\n<smallcaps><size=125%>"  + character.Name + "</size></smallcaps>\n\n";
        }
        if (!sayDueDate)
        {
            int RelativeTime = Time - (DayController.Day - StartingDay);
            if (RelativeTime == 0 && Time == 0)
            {
                r += "I need a potion today.";
            }
            else if (RelativeTime == 0)
            {
                r += "I need that order today!";
            }
            else if (RelativeTime == 1 && Time == 1)
            {
                r += "I need a potion by tomorrow.";
            }
            else if (RelativeTime == 1)
            {
                r += "I need that order by tomorrow.";
            }
            else
            {
                r += "I need a potion in " + RelativeTime + " days.";
            }
            r += "\n";
        }

        Catalyst catalyst = PrefabLoader.GetCatalyst(CatalystID);
        if (MinPotency > 0)
            r += catalyst.name + " with at least " + MinPotency + "$E.\n$_\n";
        else
            r += catalyst.name + "\n$_\n";

        foreach (Condition c in Conditions)
        {
            r += c.Description + "\n";
        }

        if (payoutOverwrite != null && payoutOverwrite != "")
        {
            r += payoutOverwrite + "\n$_\n";
        }
        else
        {
            if (Payout > 0f || PayoutPerPotency > 0f)
            {
                r += "Payout: " + Payout + "$G + " + PayoutPerPotency + "$G per $E\n$_\n";
            }
            else
            {
                r += "Free\n$_\n";
            }
        }

        

        if (sayDueDate)
        {
            int relativeDays = (StartingDay + Time - DayController.Day );
            if (relativeDays == -1)
            {
                r += "Due yesterday!";
            }
            else if (relativeDays < 0)
            {
                r += "Due " + Mathf.Abs(relativeDays) + " Days ago!";
            }
            else if (relativeDays == 0)
            {
                r += "Due today!";
            }
            else if (relativeDays == 1)
            {
                r += "Due tomorrow.";
            }
            else
            {
                r += "Due in " + relativeDays + " Days";
            }
            
        }

        return TextController.AddIcons(r);
    }

    public bool FitsOrder(Character character, MixInfo mixinfo)
    {

        if (mixinfo == null)
            return false;

        if (mixinfo.currentCatalyst != CatalystID)
            return false;
        foreach (Condition condition in Conditions)
        {
            if (!condition.Check(mixinfo))
                return false;
        }
        if (SumProperty(mixinfo.properties,Property.Potency) < MinPotency)
        {
            return false;
        }

        return true;
    }

    public int GetPayout(MixInfo potionInfo)
    {
        int payout = Payout + PayoutPerPotency * SumProperty(potionInfo.properties, Property.Potency);
        return payout;
    }

    static int SumProperty(List<Property> list, Property property)
    {
        int sum = 0;
        foreach (Property p in list)
        {
            if (p == property)
                sum++;
        }
        return sum;
    }
}
