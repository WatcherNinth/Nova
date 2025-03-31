using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


[Serializable]
public class Interrorgation_Deduction : ScriptableObject
{
    public Interrorgation_Level Level;
    public string DeductionID;
    [Tooltip("推理内容")]
    public string DeductionText;
    [Tooltip("在故事难度中默认解锁")]
    public bool DiscoveredInStoryMode = false;
    [NonSerialized]
    public bool IsDiscovered = false;
}