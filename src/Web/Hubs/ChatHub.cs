using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SimpleChat.Application.Common.Interfaces;
using SimpleChat.Application.Messaging.Commands.SendMessage;

namespace SimpleChat.Web.Hubs;

[Authorize]
public class ChatHub(ISender sender, IApplicationDbContext db) : Hub
{
    public override async Task OnConnectedAsync()
    {
        string userId = Context.UserIdentifier
            ?? throw new HubException("User not authenticated.");

        List<long> conversationIds = await db.ConversationParticipants
            .Where(cp => cp.UserId == userId)
            .Select(cp => cp.ConversationId)
            .ToListAsync(Context.ConnectionAborted);

        foreach (long conversationId in conversationIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
        }

        await base.OnConnectedAsync();
    }

    public async Task<long> SendMessage(long conversationId, string content)
    {
        try
        {
            return await sender.Send(new SendMessageCommand(conversationId, content), Context.ConnectionAborted);
        }
        catch (Exception ex) when (ex is not HubException and not OperationCanceledException)
        {
            throw new HubException("Failed to send message.");
        }
    }

    public async Task JoinConversation(long conversationId)
    {
        string userId = Context.UserIdentifier
            ?? throw new HubException("User not authenticated.");

        bool isParticipant = await db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId, Context.ConnectionAborted);

        if (!isParticipant)
        {
            throw new HubException("Not a participant in this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
    }

    public async Task LeaveConversation(long conversationId)
    {
        string userId = Context.UserIdentifier
            ?? throw new HubException("User not authenticated.");

        bool isParticipant = await db.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId, Context.ConnectionAborted);

        if (isParticipant)
        {
            throw new HubException("Still a participant in this conversation.");
        }

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId.ToString());
    }
}
