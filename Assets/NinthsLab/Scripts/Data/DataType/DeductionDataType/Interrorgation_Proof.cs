using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;



[CreateAssetMenu(fileName = "DebugProof", menuName = "Interrorgation/CreateNewProof", order = 1)]
[Serializable]
public class Interrorgation_Proof : Interrorgation_Deduction
{
    [Serializable]
    public class TopicDialoguePair
    {
        public Interrorgation_Topic Topic;

        [TextArea(3, 10)]
        public string PostTopicDialogue;
    }
    [TextArea(3, 10)]
    public string PreTopicDialogue;
    public List<TopicDialoguePair> PostTopicDialogues = new List<TopicDialoguePair>(); 
}




