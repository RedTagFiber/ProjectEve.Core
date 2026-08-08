namespace ProjectEve.Core.Chat;

public interface IEveChatService
{
    Task<string> GetReplyAsync(string sessionId, string userMessage);
}