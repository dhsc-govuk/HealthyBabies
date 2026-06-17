import React, { useEffect, useMemo, useReducer } from 'react';
import { useMutation, useQuery, useQueryClient } from 'react-query';
import { LoadingSpinner, useGovUKNotification } from '../../../../../components/GovUKComponents';
import { processError } from '../../../../../helpers/axiosErrorFallback';
import { defaultStaleTime, viewServiceDeliverySiteCacheKey } from '../../../../../helpers/queriesParams';
import { locationReducer, initialLocationState } from '../Common';
import { getLocationTypes, getSiteTypes } from '../Common/queries';
import Steps from '../Common/components/Steps';
import { LocationReducerAction } from '../Common/components/types';
import { getLocation } from './queries';
import { updateLocation, UpdateLocationDto } from './mutations';

interface Props {
  organisationId: string;
  locationId: string;
  handleFinish: (locationId: string) => void;
  apiBasePath?: string;
  onCancel?: () => void;
}

const LocationsEditComponent = ({ organisationId, locationId, handleFinish, apiBasePath, onCancel }: Props): React.ReactElement => {
  const { setNotification } = useGovUKNotification();
  const queryClient = useQueryClient();

  const [location, dispatchLocation] = useReducer(locationReducer, initialLocationState);

  const { data: locationData, isLoading: locLoading } = useQuery({
    queryKey: [viewServiceDeliverySiteCacheKey(organisationId!, locationId!), apiBasePath],
    queryFn: () => getLocation(organisationId!, locationId!, apiBasePath),
    staleTime: defaultStaleTime,
  });

  const { data: locationTypesData } = useQuery({
    queryKey: ['location-types'],
    queryFn: getLocationTypes,
    staleTime: defaultStaleTime,
  });

  const { data: siteTypesData } = useQuery({
    queryKey: ['site-types'],
    queryFn: getSiteTypes,
    staleTime: defaultStaleTime,
  });

  const locationTypes = useMemo(() => locationTypesData?.data ?? [], [locationTypesData?.data]);
  const siteTypes = useMemo(() => siteTypesData?.data ?? [], [siteTypesData?.data]);

  const { mutateAsync, isLoading: saving } = useMutation({
    mutationKey: ['admin-locations-edit'],
    mutationFn: (location: UpdateLocationDto) => updateLocation(organisationId!, location, apiBasePath),
    onSuccess(data, _variables, _context) {
      queryClient.invalidateQueries([viewServiceDeliverySiteCacheKey(organisationId!, locationId!), apiBasePath]);
      handleFinish(data.data.id!);
    },
    onError(error, _variables, _context) {
      processError(error, (e) => setNotification({ type: 'important', title: 'Error', message: e }));
    },
  });

  useEffect(() => {
    if (locationData) {
      dispatchLocation({ type: LocationReducerAction.INIT, value: locationData?.data });
    }
  }, [locationData]);

  const handleSave = async () => {
    const loc: UpdateLocationDto = {
      bsfhBranding: location.bsfhBranding,
      clarificationComments: location.clarificationComments,
      dateOpened: new Date(location.dateOpened),
      deliverySiteName: location.deliverySiteName,
      isActive: location.isActive,
      locationType: location.locationType,
      name: location.name,
      nameChange: location.nameChange,
      postCode: location.postCode,
      referenceNumber: location.referenceNumber,
      statusOfSite: location.statusOfSite,
      typeOfSite: location.typeOfSite,
      id: locationId!,
    };
    await mutateAsync(loc);
  };

  return (
    <LoadingSpinner loading={locLoading || saving} label={locLoading ? 'Loading location...' : 'Saving changes...'}>
      <Steps completeLabel="Save Changes" location={location} dispatch={dispatchLocation} handleSave={handleSave} locationTypes={locationTypes} siteTypes={siteTypes} onCancel={onCancel} />
    </LoadingSpinner>
  );
};

export default LocationsEditComponent;
