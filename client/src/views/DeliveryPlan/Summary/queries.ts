import axios from 'axios';
import { API_ROUTES } from '../../../constants/apiRoutes';
import { DeliverySummaryDto, FinancialSummaryDto } from '../../../components/Global/DeliveryPlanAnalytics/types';

export const getDeliveryPlanFinancialSummary = (area: string, yearRange: string) =>
  axios.get<FinancialSummaryDto>(API_ROUTES.deliveryPlanSummary.financialSummary(area), {
    params: { yearRange },
  });

export const getDeliveryPlanDeliverySummary = (area: string, strand?: string) =>
  axios.get<DeliverySummaryDto>(API_ROUTES.deliveryPlanSummary.deliverySummary(area), {
    params: { strand },
  });
