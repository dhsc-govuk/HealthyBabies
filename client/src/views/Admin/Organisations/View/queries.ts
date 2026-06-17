import axios from 'axios';

export interface OrganisationContactDetailDto {
  fullName: string;
  role: string;
  roleTitle?: string;
  email: string;
}

export interface OrganisationHomeDto {
  name: string;
  onsCode?: string;
  isActive: boolean;
  admins: number;
  locations: number;
  contactDetails: OrganisationContactDetailDto[];
  createdBy?: string;
  createdAt?: string;
  lastChangedBy?: string;
  lastChangedAt?: string;
}

export interface OrganisationUserDto {
  id: string;
  name: string;
  role: string;
  isActive: boolean;
}

export const getOrganisationHome = (id: string) => axios.get<OrganisationHomeDto>(`/organisations/${id}/totals`);

interface OrganisationUserApiDto {
  id: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
}

export const getOrganisationUsers = async (id: string) => {
  const response = await axios.get<OrganisationUserApiDto[]>(`/organisations/${id}/users`);
  return {
    ...response,
    data: response.data.map((user) => ({
      id: user.id,
      name: `${user.firstName} ${user.lastName}`,
      role: user.role,
      isActive: user.isActive,
    })),
  };
};
