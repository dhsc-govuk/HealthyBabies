import React from 'react';
import { Link } from 'govuk-react';
import { LoadingSpinner, GovUKTable } from '../../../../../components/GovUKComponents';
import type { Column } from '../../../../../components/GovUKComponents';
import { getLocations, GetLocationDto } from './queries';
import { booleanToYesNo } from '../../../../../helpers/stringUtils';
import { useQuery } from 'react-query';

interface Props {
  organisationId: string;
  handleView: (locationId: string) => void;
  handleEdit?: (locationId: string) => void;
  handleCreate?: () => void;
  apiBasePath?: string;
}

const ListLocationsComponent = ({ organisationId, handleView, handleEdit, handleCreate: _handleCreate, apiBasePath }: Props): React.ReactElement => {
  const { data, isLoading } = useQuery({
    queryKey: ['admin-locations-list', organisationId, apiBasePath],
    queryFn: () => getLocations(organisationId!, apiBasePath),
  });

  const columns: Column<GetLocationDto>[] = [
    {
      key: 'name',
      header: 'Name',
      render: (location) => (
        <Link
          href="#"
          onClick={(e: React.MouseEvent) => {
            e.preventDefault();
            handleView(location.id);
          }}>
          {location.name}
        </Link>
      ),
    },
    {
      key: 'isActive',
      header: 'Status',
      render: (location) => booleanToYesNo(location.isActive),
    },
    {
      key: 'actions',
      header: '',
      align: 'right',
      render: (location) => (
        <div className="govuk-table__actions">
          <Link
            href="#"
            onClick={(e: React.MouseEvent) => {
              e.preventDefault();
              handleView(location.id);
            }}>
            View<span className="govuk-visually-hidden"> {location.name}</span>
          </Link>
          {handleEdit && (
            <>
              {' '}
              <Link
                href="#"
                onClick={(e: React.MouseEvent) => {
                  e.preventDefault();
                  handleEdit(location.id);
                }}>
                Edit<span className="govuk-visually-hidden"> {location.name}</span>
              </Link>
            </>
          )}
        </div>
      ),
    },
  ];

  return (
    <LoadingSpinner loading={isLoading} label="Loading">
      <GovUKTable<GetLocationDto> data={data?.data ?? []} columns={columns} keyExtractor={(location) => location.id} searchable={false} emptyMessage="No locations found" />
    </LoadingSpinner>
  );
};

export default ListLocationsComponent;
