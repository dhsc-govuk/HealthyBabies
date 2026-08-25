import axios from 'axios';
import { FormModule, FormModuleType, FormSection } from '../Common/types';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface CreateModuleRequest {
  name: string;
  description?: string;
  type: FormModuleType;
}

export interface UpdateModuleRequest {
  name: string;
  description?: string;
  isActive: boolean;
}

export interface ChangeModuleTypeRequest {
  type: FormModuleType;
}

export interface CreateSectionRequest {
  title: string;
  description?: string;
  helpText?: string;
  helpUrl?: string;
}

export interface UpdateSectionRequest {
  title: string;
  description?: string;
  helpText?: string;
  helpUrl?: string;
}

export const createModule = async (request: CreateModuleRequest): Promise<FormModule> => {
  const response = await axios.post<FormModule>(API_ROUTES.dataCollectionFormQuestions.modules.create, request);
  return response.data;
};

export const updateModule = async (moduleId: string, request: UpdateModuleRequest): Promise<FormModule> => {
  const response = await axios.put<FormModule>(API_ROUTES.dataCollectionFormQuestions.modules.byId(moduleId), request);
  return response.data;
};

export const deleteModule = async (moduleId: string): Promise<void> => {
  await axios.delete(API_ROUTES.dataCollectionFormQuestions.modules.byId(moduleId));
};

export const changeModuleType = async (moduleId: string, request: ChangeModuleTypeRequest): Promise<FormModule> => {
  const response = await axios.put<FormModule>(API_ROUTES.dataCollectionFormQuestions.modules.type(moduleId), request);
  return response.data;
};

export const createSection = async (moduleId: string, request: CreateSectionRequest): Promise<FormSection> => {
  const response = await axios.post<FormSection>(API_ROUTES.dataCollectionFormQuestions.modules.sections(moduleId), request);
  return response.data;
};

export const updateSection = async (moduleId: string, sectionId: string, request: UpdateSectionRequest): Promise<FormSection> => {
  const response = await axios.put<FormSection>(API_ROUTES.dataCollectionFormQuestions.modules.section(moduleId, sectionId), request);
  return response.data;
};

export const deleteSection = async (moduleId: string, sectionId: string): Promise<void> => {
  await axios.delete(API_ROUTES.dataCollectionFormQuestions.modules.section(moduleId, sectionId));
};
