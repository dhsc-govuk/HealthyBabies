namespace Application.Systems.Dtos;

public record GlobalDataDto(
    Guid Id,
    string Entity,
    string Value,
    string? Description);

public record CreateGlobalDataDto(
    string Entity,
    string Value,
    string? Description);

public record UpdateGlobalDataDto(
    string Entity,
    string Value,
    string? Description);

public record GlobalDataEntityTypeDto(
    string Name,
    string Description);