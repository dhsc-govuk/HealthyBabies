import axios from 'axios';

export interface CreateAdminDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
}

export interface CreateOrganisationUserDto {
  id?: string;
  firstName: string;
  lastName: string;
  email: string;
  organisationId: string;
  role: string;
  isActive: boolean;
}

export interface CreateOrganisationUserResponse extends CreateOrganisationUserDto {
  temporaryPassword?: string | null;
}

export const createAdmin = (user: CreateAdminDto) => axios.post<CreateAdminDto>('admin/users/create', user);

export const createOrganisationUser = (user: CreateOrganisationUserDto) =>
  axios.post<CreateOrganisationUserResponse>('admin/users/organisation-users', user);
