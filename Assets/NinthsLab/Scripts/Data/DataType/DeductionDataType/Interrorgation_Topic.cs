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
        public List<string> DefaultProofContinueDialogue;
        public bool DisarrayedContinueDialogue = false;


        [Header("论据列表")]
        [Tooltip("论据ID与后续对话的映射")]
        public List<ProofDialoguePair> ProofDialogues = new List<ProofDialoguePair>();
        public bool IsSingleProof => RequiredProofCount <= 1; // 是否为单论据阶段

        [NonSerialized]
        public int SubmittedProofCount = 0; // 已提交的论据数量

        public void Init(string deductionID, int index)
        {
            RequiredProofCount = 1;
            PreProofDialogue = $"{deductionID}_Phase{index}_PreProofDialogue";
            DefaultProofFailedDialogue = $"{deductionID}_Phase{index}_DefaultProofFailedDialogue";
            DefaultProofContinueDialogue = new List<string>();
        }

        public string GetContinueDialogue()
        {
            if (DisarrayedContinueDialogue)
            {
                // 随机选择一个对话
                return DefaultProofContinueDialogue[UnityEngine.Random.Range(0, DefaultProofContinueDialogue.Count)];
            }
            else
            {
                return DefaultProofContinueDialogue[SubmittedProofCount - 1];
            }
        }

        public bool isValidProof(string proofID) => ProofDialogues.Exists(pair => pair.Proof.DeductionID == proofID);
        public string GetProofDialogue(string proofID, bool IsAlreadyProved)
        {
            var pair = ProofDialogues.Find(pair => pair.Proof.DeductionID == proofID);
            if (pair != null)
            {
                return IsAlreadyProved ? pair.AlreadyProvedDialogue : pair.PostProofDialogue;
            }
            return string.Empty;
        }
        public bool IsProved => SubmittedProofCount >= RequiredProofCount;

    }

    [Tooltip("成功对话")]
    public string TopicSuccessfulDialogue;

    [Tooltip("论据阶段列表")]
    public List<ProofPhase> ProofPhases = new List<ProofPhase>();

    [NonSerialized]
    public int CurrentProofPhaseIndex = 0; // 当前论据阶段索引

    public override void Init()
    {
        base.Init();
        TopicSuccessfulDialogue = $"{DeductionID}_TopicSuccessfulDialogue";
    }

    public ProofPhase getCurrentProofPhase()
    {
        if (CurrentProofPhaseIndex >= 0 && CurrentProofPhaseIndex < ProofPhases.Count)
        {
            return ProofPhases[CurrentProofPhaseIndex];
        }
        return null;
    }

    public bool IsProved => CurrentProofPhaseIndex >= ProofPhases.Count - 1;
}