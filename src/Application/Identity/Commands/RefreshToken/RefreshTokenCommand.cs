using MediatR;

namespace SimpleChat.Application.Identity.Commands.RefreshToken;

public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResponse>;
