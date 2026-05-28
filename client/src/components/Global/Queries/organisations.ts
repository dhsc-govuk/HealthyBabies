import { useQuery } from 'react-query';
import { defaultStaleTime, viewOrganisationCacheKey } from '../../../helpers/queriesParams';
import axios from 'axios';

interface GetOrganisationDto {
  id: string;
  name: string;
  onsCode: string;
  isActive: boolean;
}

export const useOrganisationQuery = ({ organisationId }: { organisationId: string }) =>
  useQuery({
    queryKey: [viewOrganisationCacheKey(organisationId)],
    queryFn: () => axios.get<GetOrganisationDto>(`/organisations/${organisationId}`),
    staleTime: defaultStaleTime,
  });
