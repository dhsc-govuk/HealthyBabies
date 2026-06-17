using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Dtos;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using Domain.DataCollections.Forms.ValueObjects;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record CreateFormFieldCommand : IRequest<Either<FormFieldException, FormField>>
{
    public Guid FormModuleId { get; init; }
    public Guid? FormSectionId { get; init; }
    public string FieldKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string FieldType { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? Placeholder { get; init; }
    public string? HelpText { get; init; }
    public string? DefaultValue { get; init; }
    public string? ValidationRules { get; init; }
    public string? ConditionalRules { get; init; }
    public string? Configuration { get; init; }
    public IReadOnlyList<FormFieldOptionInputDto> Options { get; init; } = [];
}

public class CreateFormFieldCommandHandler(
    IFormFieldRepository fieldRepository,
    IDataCollectionFormModuleRepository moduleRepository)
    : IRequestHandler<CreateFormFieldCommand, Either<FormFieldException, FormField>>
{
    public async Task<Either<FormFieldException, FormField>> Handle(
        CreateFormFieldCommand request,
        CancellationToken cancellationToken)
    {
        var moduleId = DataCollectionFormModuleId.From(request.FormModuleId);
        var module = await moduleRepository.GetById(moduleId, cancellationToken);

        return await module.MatchAsync(
            m => ValidateAndCreateField(request, m, cancellationToken),
            () => new FormFieldFormModuleNotFoundException(request.FormModuleId));
    }

    private async Task<Either<FormFieldException, FormField>> ValidateAndCreateField(
        CreateFormFieldCommand request,
        DataCollectionFormModule module,
        CancellationToken cancellationToken)
    {
        var existingField = await fieldRepository.FindByFieldKeyAsync(
            DataCollectionFormModuleId.From(request.FormModuleId),
            request.FieldKey,
            cancellationToken);

        if (existingField.IsSome)
        {
            return new FormFieldDuplicateKeyException(request.FieldKey, module.Code);
        }

        return await CreateFieldAsync(request, module, cancellationToken);
    }

    private async Task<Either<FormFieldException, FormField>> CreateFieldAsync(
        CreateFormFieldCommand request,
        DataCollectionFormModule module,
        CancellationToken cancellationToken)
    {
        try
        {
            var maxDisplayOrder = await fieldRepository.GetMaxDisplayOrderForModule(
                module.Id,
                cancellationToken);
            var displayOrder = maxDisplayOrder + 1;

            var fieldType = FieldType.From(request.FieldType);

            var field = module.AddField(
                request.FieldKey,
                request.Label,
                fieldType,
                displayOrder,
                request.IsRequired,
                request.Placeholder,
                request.HelpText,
                request.DefaultValue,
                request.ValidationRules);

            if (request.FormSectionId.HasValue)
            {
                field.SetSection(FormSectionId.From(request.FormSectionId.Value));
            }

            if (!string.IsNullOrEmpty(request.ConditionalRules))
            {
                field.SetConditionalRules(request.ConditionalRules);
            }

            if (!string.IsNullOrEmpty(request.Configuration))
            {
                field.SetConfiguration(request.Configuration);
            }

            foreach (var option in request.Options)
            {
                field.AddOption(option.Value, option.Label, option.DisplayOrder, option.IsDefault);
            }

            await moduleRepository.UpdateAsync(module, cancellationToken);

            return field;
        }
        catch (Exception exception)
        {
            return new FormFieldUnknownException(FormFieldId.Empty(), exception);
        }
    }
}