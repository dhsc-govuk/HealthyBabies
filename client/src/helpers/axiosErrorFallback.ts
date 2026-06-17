import axios from 'axios';

interface ValidationProblemDetails {
  title?: string;
  errors?: Record<string, string[]>;
}

export const processError = (error: unknown, fallback: (message: string | undefined) => void) => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;

    if (typeof data === 'string') {
      fallback(data);
      return;
    }

    if (data && typeof data === 'object') {
      const problemDetails = data as ValidationProblemDetails;

      if (problemDetails.errors) {
        const messages = Object.values(problemDetails.errors).flat();
        fallback(messages.join('. '));
        return;
      }

      if (problemDetails.title) {
        fallback(problemDetails.title);
        return;
      }
    }

    fallback(error.message || 'An unexpected error occurred');
  }
};
