using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_", menuName = "Interrorgation/CreateNewLevel")]
public class Interrorgation_Level : ScriptableObject
{
    public string LevelID;
    public string LevelStartDialogue;
    public List<Interrorgation_Deduction> Deductions;
    public string LevelFinishDialogue;
    [SerializeField] private TextAsset _textFile;  // 使用Unity的TextAsset类型接收文本文件
                                                   // 属性用于安全访问
    public string TextContent => _textFile ? _textFile.text : string.Empty;
    [NonSerialized]
    public List<InspirationDataType> RootInspirations = new List<InspirationDataType>();
    public void Init()
    {
        RootInspirations = InspirationDataType.ParseInspirations(TextContent);
    }
}