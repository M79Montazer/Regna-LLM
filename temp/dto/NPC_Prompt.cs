namespace temp.dto
{
    public class NpcPrompt
    {
        public static string PlayerName = "Ronald";
        public string NpcId { get; set; } = "";
        public string NpcName { get; set; } = "";
        public string NpcProfile { get; set; } = "";
        public string QuestContext { get; set; } = "";
        public string CardsContext { get; set; } = "";

        public List<FunctionsEnum> ThisNPCAvailableFunctions { get; set; } =
            [FunctionsEnum.ContinueConversation, FunctionsEnum.EndConversation];

        public string AvailableFunctionsStrings => "[\r\n" + string.Join(",\r\n",
            AllAvailableFunctions.Where(a => ThisNPCAvailableFunctions.Contains(a.Key))
                .Select(a => a.Value).ToList()) + "\r\n]";

        public string Example { get; set; } = "";
        public string UserPrompt { get; set; } = "";
        public string PlayerCharacterName { get; set; } = PlayerName;
        public string History { get; set; } = "";
        public List<string> Inventory { get; set; } = [];

        private Dictionary<FunctionsEnum, string> allAvailableFunctions;

        public Dictionary<FunctionsEnum, string> AllAvailableFunctions
        {
            set => allAvailableFunctions = value;
            get => new()
            {
                {
                    FunctionsEnum.ContinueConversation, $$"""
                                                          {
                                                            "name": "Continue_Conversation",
                                                            "description": "continue the conversation",
                                                            "parameters": {
                                                            }
                                                          }
                                                          """
                },
                {
                    FunctionsEnum.EndConversation, $$"""
                                                     {
                                                       "name": "End_Conversation",
                                                       "description": "end the conversation",
                                                       "parameters": {
                                                       }
                                                     }
                                                     """
                },
                {
                    FunctionsEnum.GiveItem, $$"""
                                              {
                                                "name": "Give_Item",
                                                "description": "give the specified item to player, item must be one of the valid choices the NPC has access to",
                                                "parameters": {
                                                  "type": "object",
                                                  "properties": {
                                                    "item_name": {
                                                      "type": "string",
                                                      "valid_choices": [{{string.Join(',', Inventory)}}]
                                                    }
                                                  },
                                                  "required": [
                                                    "item_name"
                                                  ]
                                                }
                                              }
                                              """
                },
                {
                    FunctionsEnum.Attack, $$"""
                                            {
                                              "name": "Attack",
                                              "description": "attack the player",
                                              "parameters": {
                                              }
                                            }
                                            """
                },
            };
        }
    }
}
