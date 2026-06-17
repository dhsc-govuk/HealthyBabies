import React from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { SummaryList, Paragraph } from '../../../../components/GovUKComponents';
import { getServiceCategory, serviceCategoryCacheKey } from '../../../../components/Global/ServiceCategories';
import { replaceCategoryNamePlaceholder } from '../Common';
import './styles.css';

const ViewServiceCategory = (): React.ReactElement => {
  const { serviceCategoryId } = useParams<{ serviceCategoryId: string }>();

  const { data, isLoading, isError } = useQuery({
    queryKey: serviceCategoryCacheKey(serviceCategoryId!),
    queryFn: () => getServiceCategory(serviceCategoryId!),
    enabled: !!serviceCategoryId,
  });

  const serviceCategory = data?.data;

  if (isLoading) {
    return (
      <LoadingBox loading>
        <GeneralLayout>
          <Paragraph>Loading...</Paragraph>
        </GeneralLayout>
      </LoadingBox>
    );
  }

  if (isError || !serviceCategory) {
    return (
      <GeneralLayout>
        <Paragraph>Service category not found.</Paragraph>
        <Link to="/organisation-admin/core-data/wider-service-categories" className="govuk-link">
          Back to categories
        </Link>
      </GeneralLayout>
    );
  }

  const summaryItems = serviceCategory.answers
    .sort((a, b) => a.step - b.step || a.displayOrder - b.displayOrder)
    .map((answer) => ({
      label: replaceCategoryNamePlaceholder(answer.questionLabel, serviceCategory.categoryName),
      value: answer.displayValue || answer.value || 'Not provided',
    }));

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Wider service categories', link: '/organisation-admin/core-data/wider-service-categories' },
      ]}
      currentPage={serviceCategory.categoryName}>
      <span className="govuk-caption-l">Wider services category</span>

      <Paragraph>This page shows saved information you&apos;ve provided about this wider services category.</Paragraph>
      <Paragraph>
        You can update or delete the wider services category at any time to keep your information accurate. To update the information, select &apos;Change&apos;. To remove the
        category, select &apos;Delete&apos;.
      </Paragraph>

      <div className="service-category-view__card">
        <div className="service-category-view__card-header">
          <div className="service-category-view__actions">
            <Link to={`/organisation-admin/core-data/wider-service-categories/${serviceCategoryId}/edit`} className="govuk-link">
              Change
            </Link>
            <span className="service-category-view__separator">|</span>
            <Link to={`/organisation-admin/core-data/wider-service-categories/${serviceCategoryId}/delete`} className="govuk-link">
              Delete
            </Link>
          </div>
        </div>
        <SummaryList items={summaryItems} noBorder />
      </div>
    </GeneralLayout>
  );
};

export default ViewServiceCategory;
