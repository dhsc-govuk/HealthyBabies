import axios from 'axios';
import { OrganisationUserRole } from '../Common';

export interface CreateOrganisationUserDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

export interface CreateOrganisationUserResponse extends CreateOrganisationUserDto {
  temporaryPassword?: string | null;
}

export const createOrganisationUser = (organisationId: string, user: CreateOrganisationUserDto) =>
  axios.post<CreateOrganisationUserResponse>(`/organisations/${organisationId}/users/create`, user);
