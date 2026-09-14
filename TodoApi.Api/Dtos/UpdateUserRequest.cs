using TodoApi.Api.Models;

namespace TodoApi.Api.Dtos;

/// <summary>Email is the account identity and cannot be changed. Password is only replaced when supplied.</summary>
public record UpdateUserRequest(string DisplayName, UserRole Role, string? Password);
