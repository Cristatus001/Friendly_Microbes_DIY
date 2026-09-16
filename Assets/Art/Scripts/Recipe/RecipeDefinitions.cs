// Assets/Scripts/Recipe/RecipeDefinitions.cs
using UnityEngine;
using System;
using System.Collections.Generic;

public enum ActionType
{
    DragDrop,
    RotateKnob,
    ContinuousSwipe,
    TapOrbit,
    SetTimer,
    SetTemp,
    PlaceInOven,
    PourFromTo
}

[Serializable]
public class RecipeStep
{
    public int stepIndex;
    public string title;
    public ActionType actionType;
    public string payloadID;    // e.g. "MilkJug"
    public string targetID;     // e.g. "SteelVessel"
    public float realDurationSeconds; // human time in seconds (e.g., 7*60)
    public float powerRequired; // for knob/whisk/tap
    [TextArea] public string instructionText;
}

[CreateAssetMenu(menuName = "FriendlyMicrobes/Recipe")]
public class RecipeSO : ScriptableObject
{
    public string recipeName;
    public Sprite icon;
    public string microbeName;
    [TextArea] public string microbeShortFact;
    public List<RecipeStep> steps = new List<RecipeStep>();
}
