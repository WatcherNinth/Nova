using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


[CreateAssetMenu(fileName = "DebugTopic", menuName = "Interrorgation/CreateNewTopic", order = 1)]
[Serializable]
public class Interrorgation_Topic : Interrorgation_Deduction
{

    // 嵌套关联型类
    [Serializable]
    public class ProofDialoguePair
    {
        public Interrorgation_Proof Proof;
        public string PostProofDialogue;
        public string AlreadyProvedDialogue;
        public bool IsFalseProof = false;

        public void Init(string phase)
        {
            PostProofDialogue = $"{phase}_for_{Proof.DeductionID}_PostProofDialogue";
            AlreadyProvedDialogue = $"{phase}_for_{Proof.DeductionID}_AlreadyProvedDialogue";
            IsFalseProof = false;
        }
    }

    // 嵌套核心类
    [Serializable]
    public class ProofPhase
    {
        [Header("基本设置")]
        [Tooltip("阶段类型")]
        public int RequiredProofCount = 1;
        public string PreProofDialogue;
        public string DefaultProofFailedDialogue;

        [Header("论据列表")]
        [Tooltip("论据ID与后续对话的映射")]
        public List<ProofDialoguePair> ProofDialogues = new List<ProofDialoguePair>();
        public bool IsSingleProof => RequiredProofCount <= 1; // 是否为单论据阶段

        public void Init(string deductionID)
        {
            RequiredProofCount = 1;
            PreProofDialogue = $"{deductionID}_PreProofDialogue";
            DefaultProofFailedDialogue = $"{deductionID}_DefaultProofFailedDialogue";
        }
    }

    [Tooltip("成功对话")]
    public string TopicSuccessfulDialogue;

    [Tooltip("论据阶段列表")]
    public List<ProofPhase> ProofPhases = new List<ProofPhase>();

    public override void Init()
    {
        base.Init();
        TopicSuccessfulDialogue = $"{DeductionID}_TopicSuccessfulDialogue";
    }
}