using Microsoft.AspNetCore.Http.HttpResults;
using SimpleChat.Application.Messaging.Commands.CreateConversation;
using SimpleChat.Application.Messaging.Commands.SendMessage;
using SimpleChat.Application.Messaging.Queries.GetConversations;
using SimpleChat.Application.Messaging.Queries.GetMessageHistory;

namespace SimpleChat.Web.Endpoints;

public class Conversations : IEndpointGroup
{
    public static string? RoutePrefix => "/api/conversations";

    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetConversations)
            .RequireAuthorization();

        groupBuilder.MapPost(CreateConversation)
            .RequireAuthorization();

        groupBuilder.MapPost(SendMessage, "{conversationId:long}/messages")
            .RequireAuthorization();

        groupBuilder.MapGet(GetMessageHistory, "{conversationId:long}/messages")
            .RequireAuthorization();
    }

    public static async Task<Ok<List<ConversationListDto>>> GetConversations(
        ISender sender)
    {
        List<ConversationListDto> conversations = await sender.Send(new GetConversationsQuery());
        return TypedResults.Ok(conversations);
    }

    public static async Task<Ok<CreateConversationResponse>> CreateConversation(
        ISender sender,
        CreateConversationRequest request)
    {
        long id = await sender.Send(new CreateConversationCommand(
            OtherUserId: request.OtherUserId,
            ParticipantIds: request.ParticipantIds,
            GroupName: request.GroupName));
        return TypedResults.Ok(new CreateConversationResponse(id));
    }

    public static async Task<Ok<SendMessageResponse>> SendMessage(
        ISender sender,
        long conversationId,
        SendMessageRequest request)
    {
        long id = await sender.Send(new SendMessageCommand(conversationId, request.Content));
        return TypedResults.Ok(new SendMessageResponse(id));
    }

    public static async Task<Ok<MessageHistoryResponse>> GetMessageHistory(
        ISender sender,
        long conversationId,
        long? before,
        int? limit)
    {
        MessageHistoryResponse response = await sender.Send(
            new GetMessageHistoryQuery(conversationId, before, limit ?? 50));
        return TypedResults.Ok(response);
    }
}

public record CreateConversationRequest(
    string? OtherUserId = null,
    List<string>? ParticipantIds = null,
    string? GroupName = null);
public record CreateConversationResponse(long Id);
public record SendMessageRequest(string Content);
public record SendMessageResponse(long Id);
