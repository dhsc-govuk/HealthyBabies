import axios from 'axios';
import { FormModule, FormModuleWithFields } from '../Common/types';

export const getAllModules = async (): Promise<FormModule[]> => {
  const response = await axios.get<FormModule[]>('/data-collection-form-questions/modules');
  return response.data;
};

export const getModuleWithFields = async (moduleId: string): Promise<FormModuleWithFields> => {
  const response = await axios.get<FormModuleWithFields>(`/data-collection-form-questions/modules/${moduleId}`);
  return response.data;
};
