interface IPayload {
  id: { value: string };
}

interface IError {
  type: string;
  message: string;
}

interface IMutationResult<T extends IPayload> {
  errors: IError[];
  payload: T;
}

export function matchMutationResult<T extends IPayload>(
  result: IMutationResult<T> | undefined,
  onFailure: (error: IError) => void,
  onSuccess: (payload: T) => void
) {
  if (result) {
    if (result.errors?.length) {
      result.errors.forEach((error) => {
        onFailure(error);
      });
    } else {
      if (result.payload) {
        onSuccess(result.payload);
      }
    }
  }
}
