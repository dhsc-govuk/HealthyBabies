import axios from 'axios';
import { OrganisationUserRole } from '../Common';

export interface OrganisationUserDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
  mfaStatus: string | null;
}

export const getOrganisationUsers = (organisationId: string) => axios.get<OrganisationUserDto[]>(`/organisations/${organisationId}/users`);
