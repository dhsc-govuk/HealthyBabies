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
  dataCollectionId: string;
  formModuleId: string;
  /** Null for a submission saved against a module that holds no activities list — link to the section instead. */
  activityId: string | null;
}

export interface DeliveryActivitiesLinkDto {
  dataCollectionId: string;
  /** Set only when a single strand is selected; null means link to the submission summary. */
  formModuleId: string | null;
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
  /** Null when the local authority has no submitted delivery plan round to link into. */
  activitiesLink: DeliveryActivitiesLinkDto | null;
}
