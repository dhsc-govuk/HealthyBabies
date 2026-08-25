export interface LocalAuthorityBudgetVarianceDto {
  organisationId: string;
  organisationName: string;
  forecast: number;
  actual: number;
  variancePercentage: number;
  isBudgetOffTrack: boolean;
  isUnderspend: boolean;
  isOverspend: boolean;
  offTrackActivityCount: number;
  totalActivityCount: number;
  offTrackActivityPercentage: number;
}

export interface StrandVarianceDto {
  module: string;
  displayName: string;
  totalForecast: number;
  totalActual: number;
  variancePercentage: number;
}

export interface DeliveryOverviewSummaryDto {
  yearRange: string;
  strand: string;
  offTrackBudgetLaCount: number;
  underspendLaCount: number;
  overspendLaCount: number;
  localAuthorities: LocalAuthorityBudgetVarianceDto[];
  varianceByStrand: StrandVarianceDto[];
}

export interface DeliveryOverviewStatusGroupDto {
  activityCount: number;
  activityPercentage: number;
  totalActualSpend: number;
  spendPercentage: number;
}

export interface DeliveryOverviewDeliveryStatusDto {
  yearRange: string;
  strand: string;
  onTrack: DeliveryOverviewStatusGroupDto;
  offTrack: DeliveryOverviewStatusGroupDto;
}

export interface DeliveryOverviewActivityStatusDto {
  yearRange: string;
  strand: string;
  notStarted: DeliveryOverviewStatusGroupDto;
  started: DeliveryOverviewStatusGroupDto;
  delayed: DeliveryOverviewStatusGroupDto;
  completed: DeliveryOverviewStatusGroupDto;
}
