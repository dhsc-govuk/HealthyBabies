import { useQuery } from 'react-query';
import axios from 'axios';
import { defaultStaleTime } from '../../../helpers/queriesParams';

export interface GlobalDataDto {
  id: string;
  entity: string;
  value: string;
  description: string | null;
}

export interface GlobalDataEntityTypeDto {
  name: string;
  description: string;
}

export const getEntityTypes = () =>
  axios.get<GlobalDataEntityTypeDto[]>('/global-data/entity-types');

export const useEntityTypesQuery = () =>
  useQuery({
    queryKey: ['global-data-entity-types'],
    queryFn: getEntityTypes,
    staleTime: defaultStaleTime,
  });

export const getGlobalDataByEntity = (entity: string) =>
  axios.get<GlobalDataDto[]>(`/global-data/entity/${entity}`);

export const useGlobalDataByEntityQuery = (entity: string) =>
  useQuery({
    queryKey: ['global-data', entity],
    queryFn: () => getGlobalDataByEntity(entity),
    staleTime: defaultStaleTime,
    enabled: !!entity,
  });

export const validateLookupValue = (value: string, lookupData: GlobalDataDto[]): boolean => {
  if (!value) return true;
  return lookupData.some((item) => item.value === value);
};

export const getValidationErrorForLookup = (
  value: string,
  lookupData: GlobalDataDto[],
  fieldName: string
): string | false => {
  if (!value) return `Please select a ${fieldName}`;
  if (!validateLookupValue(value, lookupData)) {
    return `The selected ${fieldName} is no longer valid. Please select a different option.`;
  }
  return false;
};
