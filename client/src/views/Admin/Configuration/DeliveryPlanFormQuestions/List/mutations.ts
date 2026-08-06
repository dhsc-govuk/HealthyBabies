import axios from 'axios';

export const deleteFormField = async (id: string): Promise<void> => {
  await axios.delete(`/data-collection-form-questions/${id}`);
};

export interface ReorderRequest {
  formModuleId: string;
  fields: { id: string; displayOrder: number }[];
}

export const reorderFormFields = async (request: ReorderRequest): Promise<void> => {
  await axios.post('/data-collection-form-questions/reorder', request);
};
