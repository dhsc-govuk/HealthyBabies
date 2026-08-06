import React from 'react';
import { useNavigate } from 'react-router-dom';
import { GovUKSummaryTable } from '../../../../../components/GovUKComponents';
import { booleanToYesNo } from '../../../../../helpers/stringUtils';
import { useQuery } from 'react-query';
import { defaultStaleTime, viewServiceDeliverySiteCacheKey } from '../../../../../helpers/queriesParams';
import { getLocation, SiteAnswerDto } from '../Edit/queries';

interface Props {
  organisationId: string;
  locationId: string;
  apiBasePath?: string;
  editPath?: string;
}

const ViewLocationDetails = ({ organisationId, locationId, apiBasePath, editPath }: Props): React.ReactElement => {
  const navigate = useNavigate();

  const {
    data: locationData,
    isLoading: locLoading,
    error,
  } = useQuery({
    queryKey: [viewServiceDeliverySiteCacheKey(organisationId, locationId), apiBasePath],
    queryFn: () => getLocation(organisationId, locationId, apiBasePath),
    staleTime: defaultStaleTime,
  });

  const handleEdit = () => {
    const path = editPath || `/organisation-admin/core-data/delivery-locations/${locationId}/edit`;
    navigate(path);
  };

  const handleDelete = () => {
    navigate(`/organisation-admin/core-data/delivery-locations/${locationId}/delete`);
  };

  if (locLoading) {
    return <p className="govuk-body">Loading service delivery location details...</p>;
  }

  if (error) {
    return (
      <div className="govuk-error-summary" data-module="govuk-error-summary" role="alert">
        <h2 className="govuk-error-summary__title">There is a problem</h2>
        <div className="govuk-error-summary__body">
          <p className="govuk-body">Unable to load service delivery location details. Please try again later.</p>
        </div>
      </div>
    );
  }

  if (!locationData) {
    return <p className="govuk-body">Service delivery location not found.</p>;
  }

  const loc = locationData.data;
  const answers: SiteAnswerDto[] = loc.answers || [];

  const sortedAnswers = [...answers].sort((a, b) => a.displayOrder - b.displayOrder);

  const rows = [
    ...sortedAnswers.map((answer) => ({
      label: answer.questionLabel,
      value: answer.displayValue || answer.value || 'Not provided',
    })),
    { label: 'Active in system', value: booleanToYesNo(loc.isActive) },
  ];

  return (
    <>
      <p className="govuk-body-l govuk-!-margin-bottom-6">
        Review all the information for this site. You can edit any details if needed.
      </p>

      <h2 className="govuk-heading-m">Site Information</h2>
      <GovUKSummaryTable
        variant="card"
        rows={rows}
        actions={[
          { label: 'Change', onClick: handleEdit },
          { label: 'Delete', onClick: handleDelete },
        ]}
      />
    </>
  );
};

export default ViewLocationDetails;
