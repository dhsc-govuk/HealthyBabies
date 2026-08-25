import axios from 'axios';
import { FormField } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const getFormFieldById = async (id: string): Promise<FormField> => {
  const response = await axios.get<FormField>(API_ROUTES.dataCollectionFormQuestions.byId(id));
  return response.data;
};
