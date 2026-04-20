namespace DialogueSystem
{
    public enum DialogueOwnerType
    {
        Global,
        Node,
        Phase
    }

    public class DialogueSource
    {
        public DialogueOwnerType OwnerType { get; set; }
        public string OwnerId { get; set; }
        public string DialogueKey { get; set; }

        public DialogueSource(DialogueOwnerType ownerType, string ownerId, string dialogueKey)
        {
            OwnerType = ownerType;
            OwnerId = ownerId;
            DialogueKey = dialogueKey;
        }

        public override string ToString()
        {
            return $"[{OwnerType}] {OwnerId ?? "N/A"} : {DialogueKey}";
        }
    }
}
