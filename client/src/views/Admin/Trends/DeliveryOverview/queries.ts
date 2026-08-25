import axios from 'axios';
import { API_ROUTES } from '../../../../constants/apiRoutes';
import { DeliveryOverviewSummaryDto, DeliveryOverviewDeliveryStatusDto, DeliveryOverviewActivityStatusDto } from './types';

export const getDeliveryOverviewSummary = (yearRange: string, strand: string) =>
  axios.get<DeliveryOverviewSummaryDto>(API_ROUTES.admin.trends.deliveryOverviewSummary, {
    params: { yearRange, strand },
  });

export const getDeliveryOverviewDeliveryStatus = (yearRange: string, strand: string) =>
  axios.get<DeliveryOverviewDeliveryStatusDto>(API_ROUTES.admin.trends.deliveryOverviewDeliveryStatus, {
    params: { yearRange, strand },
  });

export const getDeliveryOverviewActivityStatus = (yearRange: string, strand: string) =>
  axios.get<DeliveryOverviewActivityStatusDto>(API_ROUTES.admin.trends.deliveryOverviewActivityStatus, {
    params: { yearRange, strand },
  });
