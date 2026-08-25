import axios from 'axios';
import { FormModule, FormModuleWithFields } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export const getAllModules = async (): Promise<FormModule[]> => {
  const response = await axios.get<FormModule[]>(API_ROUTES.dataCollectionFormQuestions.modules.list);
  return response.data;
};

export const getModuleWithFields = async (moduleId: string): Promise<FormModuleWithFields> => {
  const response = await axios.get<FormModuleWithFields>(API_ROUTES.dataCollectionFormQuestions.modules.byId(moduleId));
  return response.data;
};
