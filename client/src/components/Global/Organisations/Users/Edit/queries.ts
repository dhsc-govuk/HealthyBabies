import axios from 'axios';
import { OrganisationUserRole } from '../Common';

interface OrganisationUserDto {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

export const getOrganisationUser = (organisationId: string, userId: string) => axios.get<OrganisationUserDto>(`/organisations/${organisationId}/users/${userId}`);
