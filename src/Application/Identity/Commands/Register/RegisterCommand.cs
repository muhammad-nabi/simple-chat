using MediatR;

namespace SimpleChat.Application.Identity.Commands.Register;

public record RegisterCommand(string DisplayName, string Email, string Password)
    : IRequest<RegisterResponse>;
