using Application.DataCollections.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Modules.Errors;

public static class DataCollectionSubmissionErrorFactory
{
    public static ObjectResult ToObjectResult(this DataCollectionSubmissionException error)
    {
        return new ObjectResult(error.Message)
        {
            StatusCode = error switch
            {
                DataCollectionSubmissionNotFoundException => StatusCodes.Status404NotFound,
                DataCollectionSubmissionNotAwaitingReviewException => StatusCodes.Status409Conflict,
                DataCollectionSubmissionWrongTypeException => StatusCodes.Status409Conflict,
                DataCollectionSubmissionUnknownException => StatusCodes.Status500InternalServerError,
                _ => throw new NotImplementedException("DataCollectionSubmission exception handler not implemented")
            }
        };
    }
}