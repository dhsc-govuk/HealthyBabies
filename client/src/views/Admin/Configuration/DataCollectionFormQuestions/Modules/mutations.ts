import axios from 'axios';
import { FormModule, FormSection } from '../Common/types';

export interface CreateModuleRequest {
  code: string;
  name: string;
  description?: string;
}

export interface UpdateModuleRequest {
  name: string;
  description?: string;
  isActive: boolean;
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
  const response = await axios.post<FormModule>('/data-collection-form-questions/modules', request);
  return response.data;
};

export const updateModule = async (moduleId: string, request: UpdateModuleRequest): Promise<FormModule> => {
  const response = await axios.put<FormModule>(`/data-collection-form-questions/modules/${moduleId}`, request);
  return response.data;
};

export const deleteModule = async (moduleId: string): Promise<void> => {
  await axios.delete(`/data-collection-form-questions/modules/${moduleId}`);
};

export const createSection = async (moduleId: string, request: CreateSectionRequest): Promise<FormSection> => {
  const response = await axios.post<FormSection>(`/data-collection-form-questions/modules/${moduleId}/sections`, request);
  return response.data;
};

export const updateSection = async (moduleId: string, sectionId: string, request: UpdateSectionRequest): Promise<FormSection> => {
  const response = await axios.put<FormSection>(`/data-collection-form-questions/modules/${moduleId}/sections/${sectionId}`, request);
  return response.data;
};

export const deleteSection = async (moduleId: string, sectionId: string): Promise<void> => {
  await axios.delete(`/data-collection-form-questions/modules/${moduleId}/sections/${sectionId}`);
};
