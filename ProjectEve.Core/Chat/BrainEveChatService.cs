namespace ProjectEve.Core.Chat;

using ProjectEve.Characters.Base;
public class BrainEveChatService : IEveChatService
{
    public async Task<string> GetReplyAsync(string sessionId, string userMessage)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Load Eve through Project Eve repository (shared DB path)
                var eve = CharacterRepository.LoadCharacter(1);
                if (eve == null)
                    return "Eve couldn’t load from the world DB.";

                if (eve.Brain == null)
                    return "Eve loaded, but brain is missing.";

                eve.Brain.Owner = eve;

                eve.Brain.Think(userMessage);
                var reply = eve.Brain.Reply(userMessage);

                if (string.IsNullOrWhiteSpace(reply))
                    return "...";

                try
                {
                    ProjectEve.Core.Database.EveDb.SaveMemory("Player: " + userMessage, "phone_chat", 1);
                    ProjectEve.Core.Database.EveDb.SaveMemory("Eve: " + reply, "phone_chat", 1);
                }
                catch
                {
                    // ignore memory write failures for now
                }

                return reply.Trim();
            }
            catch (Exception ex)
            {
                return "BRAIN ERR: " + ex.Message;
            }
        });
    }
}