import axios from 'axios';

interface UpdateOrganisationDto {
  id: string;
  name: string;
  onsCode: string;
  isActive: boolean;
}

export const updateOrganisation = (organisation: UpdateOrganisationDto) => axios.put<UpdateOrganisationDto>('/organisations/edit', organisation);
