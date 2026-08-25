import axios from 'axios';
import { OrganisationUserRole } from '../Common';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface CreateOrganisationUserDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

export interface CreateOrganisationUserResponse extends CreateOrganisationUserDto {}

export const createOrganisationUser = (organisationId: string, user: CreateOrganisationUserDto) =>
  axios.post<CreateOrganisationUserResponse>(API_ROUTES.organisations.users.create(organisationId), user);
