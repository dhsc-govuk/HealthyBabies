import axios from 'axios';
import { API_ROUTES } from '../../../../../constants/apiRoutes';

export interface SiteAnswerDto {
  id: string;
  questionCode: string;
  questionLabel: string;
  questionHint?: string;
  questionType: number;
  displayOrder: number;
  value?: string;
  displayValue?: string;
  optionsSnapshot?: string;
}

export interface GetLocationDto {
  id: string;
  name: string;
  postCode: string;
  referenceNumber: string;
  isActive: boolean;
  answers: SiteAnswerDto[];
}

export const getLocation = (organisationId: string, locationId: string, apiBasePath?: string) => {
  const url = apiBasePath ? `${apiBasePath}/${locationId}` : API_ROUTES.organisations.locations.byId(organisationId, locationId);
  return axios.get<GetLocationDto>(url);
};
