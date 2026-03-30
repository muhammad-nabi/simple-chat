using MediatR;

namespace SimpleChat.Application.Identity.Commands.Logout;

public record LogoutCommand(string RefreshToken) : IRequest;
