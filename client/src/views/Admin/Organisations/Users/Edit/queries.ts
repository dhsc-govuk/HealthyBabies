import axios from 'axios';
import { OrganisationUserRole } from '../../../../../components/Global/Organisations/Users/Common';

interface OrganisationUserDto {
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: OrganisationUserRole;
}

interface OrganisationDto {
  name: string;
}

export const getOrganisationUser = (organisationId: string, userId: string) => axios.get<OrganisationUserDto>(`/organisations/${organisationId}/users/${userId}`);

export const getOrganisation = (organisationId: string) => axios.get<OrganisationDto>(`/organisations/${organisationId}`);
