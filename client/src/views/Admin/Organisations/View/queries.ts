import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';
import { DeliverySummaryDto, FinancialSummaryDto } from '../../../../components/Global/DeliveryPlanAnalytics/types';

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

export const getOrganisationHome = (id: string) => axios.get<OrganisationHomeDto>(API_ROUTES.organisations.totals(id));

interface OrganisationUserApiDto {
  id: string;
  firstName: string;
  lastName: string;
  role: string;
  isActive: boolean;
}

export interface FinancialDataEntryDto {
  module: string;
  yearRange: string;
  amount: number;
}

export const getOrganisationFinancialData = (id: string) =>
  axios.get<FinancialDataEntryDto[]>(API_ROUTES.organisations.financialData(id));

export const getOrganisationFinancialSummary = (id: string, yearRange: string) =>
  axios.get<FinancialSummaryDto>(API_ROUTES.organisations.financialSummary(id), {
    params: { yearRange },
  });

export const getOrganisationDeliverySummary = (id: string, strand: string) =>
  axios.get<DeliverySummaryDto>(API_ROUTES.organisations.deliverySummary(id), {
    params: { strand },
  });

export const upsertOrganisationFinancialData = (id: string, entries: FinancialDataEntryDto[]) =>
  axios.put<FinancialDataEntryDto[]>(API_ROUTES.organisations.financialData(id), { entries });

export const getOrganisationUsers = async (id: string) => {
  const response = await axios.get<OrganisationUserApiDto[]>(API_ROUTES.organisations.users.list(id));
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
