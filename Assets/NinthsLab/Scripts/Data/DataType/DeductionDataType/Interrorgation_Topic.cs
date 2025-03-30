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
        [TextArea(3, 10)]
        public string PostProofDialogue;
        [TextArea(3, 10)]
        public string AlreadyProvedDialogue;
        public bool IsFalseProof = false;
    }

    // 嵌套核心类
    [Serializable]
    public class ProofPhase
    {
        [Header("基本设置")]
        [Tooltip("阶段类型")]
        public int RequiredProofCount = 1;
        [TextArea(3, 10)]
        public string PreProofDialogue;
        [TextArea(3, 10)]
        public string DefaultProofFailedDialogue;

        [Header("论据列表")]
        [Tooltip("论据ID与后续对话的映射")]
        public List<ProofDialoguePair> ProofDialogues = new List<ProofDialoguePair>();
        public bool IsSingleProof => RequiredProofCount <= 1; // 是否为单论据阶段
    }

    [Tooltip("成功对话")]
    [TextArea(3, 10)]
    public string TopicSuccessfulDialogue;

    [Tooltip("论据阶段列表")]
    public List<ProofPhase> ProofPhases = new List<ProofPhase>();
}