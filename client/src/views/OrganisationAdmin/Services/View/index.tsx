import React from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useQuery } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, SummaryList, GovUKSummaryCard } from '../../../../components/GovUKComponents';
import { getService, serviceCacheKey, ServiceDto } from '../../../../components/Global/Services';
import usePageTitle from '../../../../hooks/usePageTitle';

const ViewService = (): React.ReactElement => {
  usePageTitle('View service');
  const navigate = useNavigate();
  const { serviceId } = useParams<{ serviceId: string }>();

  const { data, isLoading } = useQuery({
    queryKey: serviceCacheKey(serviceId!),
    queryFn: () => getService(serviceId!),
    enabled: !!serviceId,
  });

  const service = data?.data;

  usePageTitle(service?.name ?? 'View service');

  const handleChange = () => {
    navigate(`/organisation-admin/core-data/services/${serviceId}/edit`);
  };

  const handleDelete = () => {
    navigate(`/organisation-admin/core-data/services/${serviceId}/delete`);
  };

  // Build summary items from service answers
  const buildSummaryItems = (svc: ServiceDto) => {
    const items: { label: string; value: string }[] = [];

    // Service name is always first
    items.push({
      label: 'What is the service name?',
      value: svc.name || 'Not provided',
    });

    // Add answers sorted by step and display order
    const sortedAnswers = [...svc.answers].sort((a, b) => {
      if (a.step !== b.step) return a.step - b.step;
      return a.displayOrder - b.displayOrder;
    });

    for (const answer of sortedAnswers) {
      items.push({
        label: answer.questionLabel,
        value: answer.displayValue || answer.value || 'Not provided',
      });
    }

    return items;
  };

  const summaryItems = service ? buildSummaryItems(service) : [];

  return (
    <LoadingSpinner loading={isLoading} label="Loading service">
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Services', link: '/organisation-admin/core-data/services' },
        ]}>
        <span className="govuk-caption-l">Service</span>
        <h1 className="govuk-heading-l">{service?.name ?? 'Loading...'}</h1>

        <p className="govuk-body">This page shows saved information you've provided about this service.</p>
        <p className="govuk-body">
          You can update or delete the service at any time to keep your information accurate. To update the information, select 'Change'. To remove the service, select 'Delete'.
        </p>

        {service && (
          <GovUKSummaryCard
            actions={[
              { label: 'Change', onClick: handleChange, visuallyHiddenText: 'service' },
              { label: 'Delete', onClick: handleDelete, visuallyHiddenText: 'service' },
            ]}>
            <SummaryList items={summaryItems} noOuterBorder halfWidthColumns />
          </GovUKSummaryCard>
        )}
      </GeneralLayout>
    </LoadingSpinner>
  );
};

export default ViewService;
