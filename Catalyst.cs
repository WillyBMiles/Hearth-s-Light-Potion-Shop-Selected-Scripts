using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Catalyst", fileName = "new Catalyst")]
public class Catalyst : SerializedScriptableObject
{
    public Color color;
    public IngredientInfo ingredientInfo = new IngredientInfo();
    public float payoutMult = 1f;

    public string GetDescription()
    {
        string output = "";
        //output += "<size=140%>" + name + "</size>";
        output += ingredientInfo.GetDescription(false, false);
        return output;
    }
}
