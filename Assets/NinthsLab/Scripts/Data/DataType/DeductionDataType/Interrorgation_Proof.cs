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

        public string PostTopicDialogue;

        public void Init(string proof)
        {
            PostTopicDialogue = $"{proof}_to_{Topic.DeductionID}_PostTopicDialogue";
        }
    }
    public string PreTopicDialogue;
    public List<TopicDialoguePair> PostTopicDialogues = new List<TopicDialoguePair>();

    public override void Init()
    {
        base.Init();
        PreTopicDialogue = $"{DeductionID}_PreTopicDialogue";
    }

    public string GetPostTopicDialogue(string topicID)
    {
        return PostTopicDialogues.Find(x => x.Topic.DeductionID == topicID).PostTopicDialogue;
    }
}




