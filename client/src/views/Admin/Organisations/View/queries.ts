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

export interface FinancialDataEntryDto {
  module: string;
  yearRange: string;
  amount: number;
}

export const getOrganisationFinancialData = (id: string) =>
  axios.get<FinancialDataEntryDto[]>(`/organisations/${id}/financial-data`);

export interface SpendFigureDto {
  amount: number;
  percentage: number;
}

export interface StrandForecastDto {
  module: string;
  displayName: string;
  forecastedSpend: number;
  allocation: number;
}

export interface RolloverSummaryDto {
  totalRolloverSpend: number;
  forecast: number;
  actual: number;
  committed: number;
  planned: number;
  unplanned: number;
}

export interface FinancialSummaryDto {
  yearRange: string;
  totalGrantAllocation: number;
  totalAnnualAllocation: number;
  forecastedSpend: SpendFigureDto;
  committedSpend: SpendFigureDto;
  actualSpend: SpendFigureDto;
  actualAndCommittedSpend: SpendFigureDto;
  plannedSpend: SpendFigureDto;
  unplannedSpend: SpendFigureDto;
  rollover: RolloverSummaryDto;
  forecastByStrand: StrandForecastDto[];
}

export interface DeliveryStatusGroupDto {
  activityCount: number;
  totalActualSpend: number;
}

export interface OffTrackActivityDto {
  module: string;
  strandDisplayName: string;
  activityName: string;
  actualSpend: number;
}

export interface DeliverySummaryDto {
  strand: string;
  onTrack: DeliveryStatusGroupDto;
  offTrack: DeliveryStatusGroupDto;
  notStarted: number;
  started: number;
  delayed: number;
  completed: number;
  offTrackActivities: OffTrackActivityDto[];
}

export const getOrganisationFinancialSummary = (id: string, yearRange: string) =>
  axios.get<FinancialSummaryDto>(`/organisations/${id}/financial-summary`, {
    params: { yearRange },
  });

export const getOrganisationDeliverySummary = (id: string, strand: string) =>
  axios.get<DeliverySummaryDto>(`/organisations/${id}/delivery-summary`, {
    params: { strand },
  });

export const upsertOrganisationFinancialData = (id: string, entries: FinancialDataEntryDto[]) =>
  axios.put<FinancialDataEntryDto[]>(`/organisations/${id}/financial-data`, { entries });

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
