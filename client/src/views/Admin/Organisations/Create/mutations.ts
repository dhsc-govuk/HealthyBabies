import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface ContactPersonDto {
  fullName: string;
  role: string;
  roleTitle?: string;
  email: string;
}

export interface CreateOrganisationDto {
  id?: string;
  name: string;
  onsCode: string;
  isActive: boolean;
  contacts?: ContactPersonDto[];
}

export const createOrganisation = (organisation: CreateOrganisationDto) => axios.post<CreateOrganisationDto>(API_ROUTES.organisations.create, organisation);
