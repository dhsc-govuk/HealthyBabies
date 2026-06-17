import axios from 'axios';

export interface GetAdminsResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  mfaStatus: string | null;
}

export interface OrganisationUserResponse {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  isActive: boolean;
  role: string;
  organisation: string | null;
  organisationId: string | null;
  mfaStatus: string | null;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export const getAdmins = () => axios.get<GetAdminsResponse[]>('admin/users');

export const getAllOrganisationUsers = (pageSize = 100, pageNumber = 1) =>
  axios.get<PaginatedResult<OrganisationUserResponse>>('admin/users/organisation-users', {
    params: { pageSize, pageNumber },
  });
