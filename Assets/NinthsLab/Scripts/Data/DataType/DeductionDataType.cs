using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nova
{
    [Serializable]
    public class Interrorgation_DeductionProcedure_Answer
    {
        [Tooltip("如果填写内容，当玩家持有这个情报时，这个回答会自动验证。")]
        public string AutoVerifiedIntelID = "";
        [Tooltip("如果填写内容，这个回答会作为选项出现")]
        public string OptionText = "";
        public string NLPInputAnswer;
        [TextArea(3, 10)]
        public string AnswerScript;
    }

    [Serializable]
    public class Interrorgation_DeductionProcedureEntry
    {
        [TextArea(3, 10)]
        public string QuesitonScript;
        public List<Interrorgation_DeductionProcedure_Answer> Answers;

        public List<string> GetAllAutoIntelIDsFromAnswers()
        {
            List<string> autoIntelIDs = new List<string>();
            foreach (var answer in Answers)
            {
                if (!string.IsNullOrEmpty(answer.AutoVerifiedIntelID))
                {
                    autoIntelIDs.Add(answer.AutoVerifiedIntelID);
                }
            }
            return autoIntelIDs;
        }

        public List<string> GetAllNLPInputAnswers()
        {
            List<string> nlpInputAnswers = new List<string>();
            foreach (var answer in Answers)
            {
                if (!string.IsNullOrEmpty(answer.NLPInputAnswer))
                {
                    nlpInputAnswers.Add(answer.NLPInputAnswer);
                }
            }
            return nlpInputAnswers;
        }

        public string GetAnswerScriptFromIndex(int index)
        {
            if (index < Answers.Count && index >= 0)
            {
                return Answers[index].AnswerScript;
            }
            return "";
        }
    }

    [CreateAssetMenu(fileName = "Deduction_", menuName = "Interrorgation/CreateNewDeduction")]
    [Serializable]
    public class Interrorgation_Deduction : ScriptableObject
    {
        [Tooltip("推理内容")]
        public string DeductionText;
        [Tooltip("这个推理在故事模式下以这个选项形式给出")]
        public string ChoiceText;
        [TextArea(3, 10)]
        public string DeductionStartScript;
        public List<Interrorgation_DeductionProcedureEntry> DeductionProcedure;
        [TextArea(3, 10)]
        public string DeductionSuccessScript;
        [TextArea(3, 10)]
        public string DeductionFailScript;
    }

    [CreateAssetMenu(fileName = "Level_", menuName = "Interrorgation/CreateNewLevel")]
    public class Interrorgation_Level : ScriptableObject
    {
        public string LevelID;
        [TextArea(3, 10)]
        public string LevelStartScript;
        public List<Interrorgation_Deduction> Deductions;
        [TextArea(3, 10)]
        public string LevelFinishScript;
    }
}