using EShop.Identity.Application.DTOs;
using MediatR;

namespace EShop.Identity.Application.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResponseDto>;
