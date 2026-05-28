import React from 'react';
import { useNavigate } from 'react-router-dom';
import { SummaryList, Button } from '../../../../../components/GovUKComponents';
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

  if (locLoading) {
    return (
      <div className="nhsuk-u-margin-top-4">
        <p>Loading service delivery location details...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="nhsuk-u-margin-top-4">
        <div className="nhsuk-error-summary" aria-labelledby="error-summary-title" role="alert" tabIndex={-1}>
          <h2 className="nhsuk-error-summary__title" id="error-summary-title">
            There is a problem
          </h2>
          <div className="nhsuk-error-summary__body">
            <p>Unable to load service delivery location details. Please try again later.</p>
          </div>
        </div>
      </div>
    );
  }

  if (!locationData) {
    return (
      <div className="nhsuk-u-margin-top-4">
        <p>Service delivery location not found.</p>
      </div>
    );
  }

  const loc = locationData.data;
  const answers: SiteAnswerDto[] = loc.answers || [];

  // Sort answers by displayOrder - includes predefined fields (FHS01/02/03) dynamically
  const sortedAnswers = [...answers].sort((a, b) => a.displayOrder - b.displayOrder);

  const summaryItems: { label: string; value: string }[] = [];

  for (const answer of sortedAnswers) {
    summaryItems.push({
      label: answer.questionLabel,
      value: answer.displayValue || answer.value || 'Not provided',
    });
  }

  summaryItems.push({
    label: 'Active in system',
    value: booleanToYesNo(loc.isActive),
  });

  return (
    <div className="nhsuk-u-margin-top-4">
      <div className="nhsuk-u-margin-bottom-5">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', flexWrap: 'wrap', gap: '1rem' }}>
          <div>
            <p className="nhsuk-body-l nhsuk-u-margin-bottom-0">Review all the information for this site. You can edit any details if needed.</p>
          </div>
        </div>
      </div>

      <div className="nhsuk-u-margin-bottom-6">
        <h2 className="nhsuk-heading-l">Site Information</h2>
        <SummaryList items={summaryItems} />
      </div>

      <div className="nhsuk-u-margin-top-6">
        <Button onClick={handleEdit} size="small">
          Edit Site
        </Button>
      </div>
    </div>
  );
};

export default ViewLocationDetails;
