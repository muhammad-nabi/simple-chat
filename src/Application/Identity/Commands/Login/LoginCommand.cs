using MediatR;

namespace SimpleChat.Application.Identity.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;
