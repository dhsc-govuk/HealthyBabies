using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Dtos;
using Application.DataCollections.Exceptions;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record UpdateFormFieldCommand : IRequest<Either<FormFieldException, FormField>>
{
    public Guid Id { get; init; }
    public Guid? FormSectionId { get; init; }
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public int DisplayOrder { get; init; }
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public string? Placeholder { get; init; }
    public string? HelpText { get; init; }
    public string? DefaultValue { get; init; }
    public string? ValidationRules { get; init; }
    public string? ConditionalRules { get; init; }
    public string? Configuration { get; init; }
    public IReadOnlyList<FormFieldOptionInputDto> Options { get; init; } = [];
}

public class UpdateFormFieldCommandHandler(IFormFieldRepository repository)
    : IRequestHandler<UpdateFormFieldCommand, Either<FormFieldException, FormField>>
{
    public async Task<Either<FormFieldException, FormField>> Handle(
        UpdateFormFieldCommand request,
        CancellationToken cancellationToken)
    {
        var fieldId = FormFieldId.From(request.Id);
        var field = await repository.GetById(fieldId, cancellationToken);

        return await field.MatchAsync(
            f => UpdateField(request, f, cancellationToken),
            () => new FormFieldNotFoundException(fieldId));
    }

    private async Task<Either<FormFieldException, FormField>> UpdateField(
        UpdateFormFieldCommand request,
        FormField field,
        CancellationToken cancellationToken)
    {
        try
        {
            field.UpdateDetails(
                request.Label,
                request.DisplayOrder,
                request.IsRequired,
                request.Placeholder,
                request.HelpText,
                request.DefaultValue);

            field.SetFieldType(FieldType.From(request.FieldType));
            field.SetSection(request.FormSectionId.HasValue ? FormSectionId.From(request.FormSectionId.Value) : null);
            field.SetValidationRules(request.ValidationRules);
            field.SetConditionalRules(request.ConditionalRules);
            field.SetConfiguration(request.Configuration);

            if (request.IsActive)
            {
                field.Activate();
            }
            else
            {
                field.Deactivate();
            }

            await repository.ClearOptionsAsync(field.Id, cancellationToken);

            foreach (var option in request.Options)
            {
                field.AddOption(option.Value, option.Label, option.DisplayOrder, option.IsDefault);
            }

            return await repository.UpdateAsync(field, cancellationToken);
        }
        catch (Exception exception)
        {
            return new FormFieldUnknownException(field.Id, exception);
        }
    }
}