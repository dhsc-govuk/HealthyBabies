import React from 'react';
import { useNavigate, useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from 'react-query';
import { LoadingBox, Button } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { Paragraph } from '../../../../components/GovUKComponents';
import { getServiceCategory, serviceCategoryCacheKey, deleteServiceCategory, serviceCategoriesCacheKey } from '../../../../components/Global/ServiceCategories';
import { WarningPanel, WarningPanelTitle, WarningPanelBody, WarningPanelActions, WarningPanelLink } from '../../../../styles/govuk-global';
import usePageTitle from '../../../../hooks/usePageTitle';

const DeleteServiceCategory = (): React.ReactElement => {
  usePageTitle('Delete wider services category');
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { serviceCategoryId } = useParams<{ serviceCategoryId: string }>();

  const {
    data,
    isLoading: isLoadingCategory,
    isError,
  } = useQuery({
    queryKey: serviceCategoryCacheKey(serviceCategoryId!),
    queryFn: () => getServiceCategory(serviceCategoryId!),
    enabled: !!serviceCategoryId,
  });

  const serviceCategory = data?.data;

  const deleteMutation = useMutation({
    mutationFn: () => deleteServiceCategory(serviceCategoryId!),
    onSuccess: () => {
      queryClient.invalidateQueries(serviceCategoriesCacheKey());
      queryClient.invalidateQueries(['organisation-admin-submission']);
      queryClient.invalidateQueries(['wider-service-users-module']);
      navigate('/organisation-admin/core-data/wider-service-categories', {
        state: {
          notification: {
            type: 'success',
            title: 'Success',
            message: `${serviceCategory?.categoryName} has been deleted successfully.`,
          },
        },
      });
    },
  });

  const handleDelete = async () => {
    await deleteMutation.mutateAsync();
  };

  const handleCancel = () => {
    navigate(`/organisation-admin/core-data/wider-service-categories/${serviceCategoryId}`);
  };

  const isLoading = isLoadingCategory || deleteMutation.isLoading;

  if (isLoadingCategory) {
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

  return (
    <LoadingBox loading={isLoading}>
      <GeneralLayout backLink={{ href: `/organisation-admin/core-data/wider-service-categories/${serviceCategoryId}` }}>
        <WarningPanel>
          <WarningPanelTitle>Are you sure you want to delete the wider services category?</WarningPanelTitle>
          <WarningPanelBody>
            Deleting &apos;{serviceCategory.categoryName}&apos; will remove all saved information about this service category from your Best Start Family Hub and Healthy Baby
            account.
          </WarningPanelBody>
          <WarningPanelBody>You will also no longer be able to use its information for future Delivery Plan and Management Information submissions.</WarningPanelBody>
          <WarningPanelBody>If you delete the service category, you can add it back later, but you will need to enter the information again.</WarningPanelBody>
          <WarningPanelBody>If you want to keep this wider services category&apos;s information, you can go back.</WarningPanelBody>
          <WarningPanelActions>
            <Button onClick={handleDelete} disabled={isLoading} buttonColour="#ffffff" buttonTextColour="#1d70b8">
              Yes, I want to delete
            </Button>
            <WarningPanelLink type="button" onClick={handleCancel}>
              Go back
            </WarningPanelLink>
          </WarningPanelActions>
        </WarningPanel>
      </GeneralLayout>
    </LoadingBox>
  );
};

export default DeleteServiceCategory;
