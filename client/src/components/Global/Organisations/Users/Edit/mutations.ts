import axios from 'axios';
import { OrganisationUserRole } from '../Common';

export interface EditOrganisationUserDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

export const updateOrganisationUser = (organisationId: string, user: EditOrganisationUserDto) =>
  axios.put<EditOrganisationUserDto>(`/organisations/${organisationId}/users/edit`, user);
