import axios from 'axios';

interface OrganisationDto {
  name: string;
}

export const getOrganisation = (organisationId: string) => axios.get<OrganisationDto>(`/organisations/${organisationId}`);
