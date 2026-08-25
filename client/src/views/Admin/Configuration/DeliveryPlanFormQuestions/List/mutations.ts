import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const deleteFormField = async (id: string): Promise<void> => {
  await axios.delete(API_ROUTES.dataCollectionFormQuestions.byId(id));
};

export interface ReorderRequest {
  formModuleId: string;
  fields: { id: string; displayOrder: number }[];
}

export const reorderFormFields = async (request: ReorderRequest): Promise<void> => {
  await axios.post(API_ROUTES.dataCollectionFormQuestions.reorder, request);
};
