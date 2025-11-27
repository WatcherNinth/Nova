using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Nova
{
    public class DeductionManager : DeductionBaseClass
    {
        #region 单例模式

        // 静态实例
        private static DeductionManager _instance;

        // 公共访问点
        public static DeductionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 尝试在场景中找到现有的实例
                    _instance = FindAnyObjectByType<DeductionManager>();

                    if (_instance == null)
                    {
                        // 如果没有实例，则动态创建一个新对象
                        GameObject singletonObject = new GameObject("DeductionMng");
                        _instance = singletonObject.AddComponent<DeductionManager>();
                    }
                }
                return _instance;
            }
        }

        void instanceFunctionInAwake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject); // 保持跨场景的持久性
            }
            else if (_instance != this)
            {
                Destroy(gameObject); // 如果已经有实例存在，销毁多余的实例
            }
        }
        #endregion
        [Header("DebugUI专用，有值意味着为Debug模式")]
        [SerializeField, AllowNull]
        private DeductionUIManager DebugUIManager;
        // Manager类列表
        private DeductionUIManager deductionUIManager;

        #region 关卡信息管理
        [Header("当前关卡信息")]
        public Interrorgation_Level currentLevel;
        #endregion
        private GameState gameState;
        private GameViewInput gameViewInput;
        private GameViewController gameViewController;

        private Queue<UnityAction> dialogueActionQueue = new Queue<UnityAction>();

        // 确保实例在场景切换时不会销毁
        private void Awake()
        {
            instanceFunctionInAwake();
            LuaRuntime.Instance.BindObject("deductionManager", this);
            var controller = Utils.FindNovaController();
            gameState = controller.GameState;

        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            getUIManager();
            gameViewInput = FindAnyObjectByType<GameViewInput>();
            gameViewController = FindAnyObjectByType<GameViewController>();
        }

        // Update is called once per frame
        void Update()
        {

        }

        public override void LoadLevel(string levelName)
        {
            Debug.Log("Load Deduction Level: " + levelName);
            // 构建资源路径
            string resourcePath = string.Format("InterrorgationLevels/{0}/level", levelName);

            // 加载关卡资源
            currentLevel = Resources.Load<Interrorgation_Level>(resourcePath);

            // 异常处理
            if (currentLevel == null)
            {
                Debug.LogError($"找不到关卡资源: {resourcePath}");
                return;
            }
            currentLevel.Init();
            getUIManager();
            deductionUIManager.ActivateDeductionMode();
            callDialogue(currentLevel.LevelStartDialogue);
        }

        public void DiscoverDeduction(string deductionID)
        {
            Interrorgation_Deduction discoveredDeduction = currentLevel.Deductions.Find(d => d.DeductionID == deductionID);
            if (discoveredDeduction == null)
            {
                Debug.LogError($"找不到推理ID: {deductionID}");
                return;
            }
            discoveredDeduction.IsDiscovered = true;
            // 激活推理UI
            deductionUIManager.DiscoverDeduction(discoveredDeduction);
        }

        private void getUIManager()
        {
            if (DebugUIManager != null)
            {
                deductionUIManager = DebugUIManager;
                LoadLevel("TestLevel");
                return;
            }
            if (deductionUIManager != null) return;
            deductionUIManager = FindAnyObjectByType<DeductionUIManager>();
        }
        #region 推理流程

        private List<Interrorgation_Deduction> provedDeductions = new List<Interrorgation_Deduction>();
        private Interrorgation_Topic currentTopic;
        public void SubmitDeduction(Interrorgation_Deduction deductionData)
        {
            if (deductionData == null)
            {
                Debug.LogError("提交的推理数据为空");
                return;
            }
            switch (deductionData)
            {
                case Interrorgation_Topic topic:
                    topicHandler(topic);
                    break;
                case Interrorgation_Proof proof:
                    proofHandler(proof);
                    break;
                default:
                    Debug.LogError($"{deductionData.DeductionID}：未知的推理类型");
                    break;
            }
            executeQueue();
        }
        private void topicHandler(Interrorgation_Topic topic)
        {
            if (currentTopic != null)
            {
                callDialogue(currentTopic.getCurrentProofPhase().DefaultProofFailedDialogue);
                return;
            }
            currentTopic = topic;
            callDialogue(topic.getCurrentProofPhase().PreProofDialogue);
        }
        private void proofHandler(Interrorgation_Proof proof)
        {
            if (currentTopic != null)
            {
                var proofPhase = currentTopic.getCurrentProofPhase();
                if (proofPhase.isValidProof(proof.DeductionID))
                {
                    proofPhase.SubmittedProofCount++;
                    dialogueActionQueue.Enqueue(() => callDialogue(proofPhase.GetProofDialogue(proof.DeductionID, false)));
                    if (proofPhase.IsProved)
                    {
                        currentTopic.CurrentProofPhaseIndex++;
                        if (currentTopic.IsProved)
                        {
                            dialogueActionQueue.Enqueue(() =>
                            {
                                callDialogue(currentTopic.TopicSuccessfulDialogue);
                                currentTopic = null;
                            });
                        }
                        else
                        {
                            dialogueActionQueue.Enqueue(() => callDialogue(currentTopic.getCurrentProofPhase().PreProofDialogue));
                        }
                    }
                    else
                    {
                        dialogueActionQueue.Enqueue(() => callDialogue(proofPhase.GetContinueDialogue()));
                    }
                }
                else
                {
                    callDialogue(proofPhase.DefaultProofFailedDialogue);
                }
            }
            else
            {
                var provedDeduction = GetProvedDeduction(proof);
                if (provedDeduction != null)
                {
                    callDialogue(proof.GetPostTopicDialogue(provedDeduction.DeductionID));
                }
                else
                {
                    callDialogue(proof.PreTopicDialogue);
                }
            }
        }

        public Interrorgation_Deduction GetProvedDeduction(Interrorgation_Deduction deductionData)
        {
            if (deductionData == null)
            {
                Debug.LogError("检查的推理数据为空");
                return null;
            }
            return GetProvedDeduction(deductionData.DeductionID);
        }

        public Interrorgation_Deduction GetProvedDeduction(string deductionID)
        {
            return provedDeductions.FindLast(x => x.DeductionID == deductionID);
        }

        public override void EndLevel()
        {
            base.EndLevel();
            Debug.Log("Level Ended");
            // 在这里添加结束关卡的逻辑
            deductionUIManager.DeactivateDeductionMode();
        }

        #endregion

        #region 对话
        private void callDialogue(string label)
        {
            Debug.Log("Dialogue called: " + label);
            hideDeductionUI();
            gameState.MoveToNextNode(label);
        }

        public override void DialogueFinished(string nodeID = "")
        {
            Debug.Log("Dialgue Finished: " + (nodeID == "" ? "No node ID" : nodeID));
            onDialogueFinishedCallback(nodeID);

        }

        private void onDialogueFinishedCallback(string nodeID)
        {
            if (dialogueActionQueue.Count > 0)
            {
                executeQueue();
                return;
            }
            showDeductionUI();
        }

        #endregion

        #region UI
        private void showDeductionUI()
        {
            deductionUIManager.ShowDeductionUI();
            gameState.AcquireActionPause();
            gameViewInput.SetFenceFromDeduction(true);
            gameViewController.HideUI();
        }

        private void hideDeductionUI()
        {
            deductionUIManager.HideDeductionUI();
            gameState.ReleaseActionPause();
            gameViewInput.SetFenceFromDeduction(false);
            gameViewController.ShowUI();
        }

        #endregion
        #region 队列
        private void executeQueue()
        {
            if (dialogueActionQueue.Count > 0)
            {
                var action = dialogueActionQueue.Dequeue();
                action.Invoke();
            }
        }
        #endregion
        #region 测试
        public void Debug_GetAllDeduction()
        {
            if (currentLevel == null) return;
            foreach (var deduction in currentLevel.Deductions)
            {
                Debug.Log(deduction.DeductionText);
                DiscoverDeduction(deduction.DeductionID); // 发现所有推论
            }

        }
        #endregion
    }
}
