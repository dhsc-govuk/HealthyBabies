import axios from 'axios';

export interface ContactPersonDto {
  fullName: string;
  role: string;
  roleTitle?: string;
  email: string;
}

export interface CreateOrganisationDto {
  id?: string;
  name: string;
  onsCode: string;
  isActive: boolean;
  contacts?: ContactPersonDto[];
}

export const createOrganisation = (organisation: CreateOrganisationDto) => axios.post<CreateOrganisationDto>('/organisations/create', organisation);
