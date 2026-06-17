using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Dtos;
using Application.DataCollections.Exceptions;
using Domain.DataCollections;
using Domain.DataCollections.Forms;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record ReorderFormFieldsCommand : IRequest<Either<FormFieldException, IReadOnlyList<FormField>>>
{
    public Guid FormModuleId { get; init; }
    public IReadOnlyList<FormFieldOrderDto> Fields { get; init; } = [];
}

public class ReorderFormFieldsCommandHandler(IFormFieldRepository repository)
    : IRequestHandler<ReorderFormFieldsCommand, Either<FormFieldException, IReadOnlyList<FormField>>>
{
    public async Task<Either<FormFieldException, IReadOnlyList<FormField>>> Handle(
        ReorderFormFieldsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var moduleId = DataCollectionFormModuleId.From(request.FormModuleId);
            var fields = await repository.GetByModuleIdTracking(moduleId, cancellationToken);

            foreach (var fieldOrder in request.Fields)
            {
                var field = fields.FirstOrDefault(f => f.Id.Value == fieldOrder.Id);
                if (field != null)
                {
                    field.UpdateDetails(
                        field.Label,
                        fieldOrder.DisplayOrder,
                        field.IsRequired,
                        field.Placeholder,
                        field.HelpText,
                        field.DefaultValue);
                }
            }

            await repository.SaveChangesAsync(cancellationToken);
            return Either<FormFieldException, IReadOnlyList<FormField>>.Right(fields);
        }
        catch (Exception exception)
        {
            return new FormFieldUnknownException(FormFieldId.Empty(), exception);
        }
    }
}