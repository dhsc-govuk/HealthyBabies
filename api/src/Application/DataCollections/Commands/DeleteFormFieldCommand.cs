using Application.Common.Interfaces.Repositories;
using Application.DataCollections.Exceptions;
using Domain.DataCollections.Forms;
using LanguageExt;
using MediatR;

namespace Application.DataCollections.Commands;

public record DeleteFormFieldCommand : IRequest<Either<FormFieldException, FormField>>
{
    public Guid Id { get; init; }
}

public class DeleteFormFieldCommandHandler(IFormFieldRepository repository)
    : IRequestHandler<DeleteFormFieldCommand, Either<FormFieldException, FormField>>
{
    public async Task<Either<FormFieldException, FormField>> Handle(
        DeleteFormFieldCommand request,
        CancellationToken cancellationToken)
    {
        var fieldId = FormFieldId.From(request.Id);
        var field = await repository.GetById(fieldId, cancellationToken);

        return await field.MatchAsync(
            f => DeleteField(f, cancellationToken),
            () => new FormFieldNotFoundException(fieldId));
    }

    private async Task<Either<FormFieldException, FormField>> DeleteField(
        FormField field,
        CancellationToken cancellationToken)
    {
        try
        {
            var hasRelatedValues = await repository.HasRelatedValuesAsync(field.Id, cancellationToken);
            if (hasRelatedValues)
            {
                return new FormFieldHasValuesException(field.Id);
            }

            await repository.DeleteAsync(field, cancellationToken);
            return field;
        }
        catch (Exception exception)
        {
            return new FormFieldUnknownException(field.Id, exception);
        }
    }
}