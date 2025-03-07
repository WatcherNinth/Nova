using UnityEngine;

namespace Nova
{
    public class DeductionManager : DeductionBaseClass
    {
        void Awake()
        {
            LuaRuntime.Instance.BindObject("deductionManager", this);
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public override void LoadLevel(string levelName)
        {  
            Debug.Log("Loading level: " + levelName);
        }

        public override void DialogueFinished(string nodeID = "")
        {
            Debug.Log("Dialgue Finished: " + (nodeID == "" ? "No node ID" : nodeID));
        }
    }
}