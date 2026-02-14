using EShop.Identity.Application.DTOs;
using MediatR;

namespace EShop.Identity.Application.Commands.Register;

public record RegisterCommand(string Email, string Password, string FullName)
    : IRequest<RegisterResponseDto>;
