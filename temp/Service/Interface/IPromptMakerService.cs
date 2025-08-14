namespace temp.Service.Interface
{
    public interface IPromptMakerService
    {
        string MakePrompt(string npcId, string rawPrompt);
    }
}
