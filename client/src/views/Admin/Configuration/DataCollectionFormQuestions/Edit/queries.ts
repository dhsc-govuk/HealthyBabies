import axios from 'axios';
import { FormField } from '../Common/types';

export const getFormFieldById = async (id: string): Promise<FormField> => {
  const response = await axios.get<FormField>(`/data-collection-form-questions/${id}`);
  return response.data;
};
