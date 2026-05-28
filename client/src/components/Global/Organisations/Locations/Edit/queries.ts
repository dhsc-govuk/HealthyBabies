import axios from 'axios';

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
  const url = apiBasePath ? `${apiBasePath}/${locationId}` : `/organisations/${organisationId}/locations/${locationId}`;
  return axios.get<GetLocationDto>(url);
};
