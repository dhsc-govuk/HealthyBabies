import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';

export interface CreateAdminDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

export interface CreateOrganisationUserDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  organisationId: string;
  role: string;
  isActive: boolean;
}

export interface CreateOrganisationUserResponse extends CreateOrganisationUserDto {}

export const createAdmin = (user: CreateAdminDto) => axios.post<CreateAdminDto>(API_ROUTES.admin.users.create, user);

export const createOrganisationUser = (user: CreateOrganisationUserDto) =>
  axios.post<CreateOrganisationUserResponse>(API_ROUTES.admin.users.organisationUsers.create, user);
