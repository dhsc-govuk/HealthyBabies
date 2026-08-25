import axios from 'axios';
import { OrganisationUserRole } from '../Common';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface EditOrganisationUserDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

export const updateOrganisationUser = (organisationId: string, user: EditOrganisationUserDto) =>
  axios.put<EditOrganisationUserDto>(API_ROUTES.organisations.users.edit(organisationId), user);
