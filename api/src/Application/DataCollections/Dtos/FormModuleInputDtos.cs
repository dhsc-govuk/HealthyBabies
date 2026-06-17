namespace Application.DataCollections.Dtos;

public record CreateFormModuleDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record UpdateFormModuleDto
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
}

public record CreateFormSectionDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? HelpText { get; init; }
    public string? HelpUrl { get; init; }
}

public record UpdateFormSectionDto
{
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? HelpText { get; init; }
    public string? HelpUrl { get; init; }
}