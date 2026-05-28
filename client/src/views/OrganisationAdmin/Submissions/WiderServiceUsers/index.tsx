import React, { useState, useMemo } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useQuery } from 'react-query';
import { GeneralLayout } from '../../../../layouts';
import { LoadingSpinner, GovUKButton, GovUKTag, GovUKDetails, GovUKNotificationBanner } from '../../../../components/GovUKComponents';
import { getWiderServiceUsersModule } from '../queries';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import './styles.css';

interface WiderServiceCategoryStatus {
  serviceCategoryId: string;
  categoryName: string;
  status: 'Not started' | 'In progress' | 'Completed';
  userCount?: number;
}

const WiderServiceUsersList = (): React.ReactElement => {
  const navigate = useNavigate();
  const location = useLocation();
  const { submissionId, moduleId } = useParams<{
    submissionId: string;
    moduleId: string;
  }>();

  const [searchTerm, setSearchTerm] = useState('');
  const [sortBy, setSortBy] = useState('status');

  const notification = location.state?.notification as
    | {
        type: 'success' | 'error';
        title: string;
        message: string;
      }
    | undefined;

  const { data, isLoading } = useQuery({
    queryKey: ['wider-service-users-module', submissionId, moduleId],
    queryFn: () => getWiderServiceUsersModule(submissionId!, moduleId!),
    enabled: !!submissionId && !!moduleId,
  });

  const moduleData = data?.data;

  const filteredAndSortedCategories = useMemo(() => {
    if (!moduleData?.categories) return [];

    let categories = [...moduleData.categories];

    if (searchTerm) {
      categories = categories.filter((c: WiderServiceCategoryStatus) => c.categoryName.toLowerCase().includes(searchTerm.toLowerCase()));
    }

    categories.sort((a: WiderServiceCategoryStatus, b: WiderServiceCategoryStatus) => {
      if (sortBy === 'status') {
        const statusOrder: Record<string, number> = { 'Not started': 0, 'In progress': 1, Completed: 2 };
        return (statusOrder[a.status] ?? 0) - (statusOrder[b.status] ?? 0);
      }
      return a.categoryName.localeCompare(b.categoryName);
    });

    return categories;
  }, [moduleData?.categories, searchTerm, sortBy]);

  const handleCategoryClick = (categoryId: string) => {
    navigate(`/organisation-admin/submissions/${submissionId}/modules/${moduleId}/wider-services/${categoryId}`);
  };

  const handleManageCategories = () => {
    navigate('/organisation-admin/core-data/wider-service-categories');
  };

  if (isLoading) {
    return (
      <GeneralLayout>
        <LoadingSpinner loading label="Loading wider services" />
      </GeneralLayout>
    );
  }

  if (!moduleData) {
    return (
      <GeneralLayout>
        <h1 className="govuk-heading-l">Module not found</h1>
        <p className="govuk-body">The module you are looking for does not exist.</p>
        <GovUKButton onClick={() => navigate(`/organisation-admin/submissions/${submissionId}`)}>Back to submission</GovUKButton>
      </GeneralLayout>
    );
  }

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Submissions', link: '/organisation-admin/submissions' },
        { label: moduleData.submissionName || 'Submission', link: `/organisation-admin/submissions/${submissionId}` },
        { label: 'Section 3: Wider services', link: '#' },
      ]}
      currentPage=""
      backLink={!notification ? { href: `/organisation-admin/submissions/${submissionId}` } : undefined}>
      {notification && (
        <GovUKNotificationBanner type={notification.type === 'success' ? 'success' : 'important'} title={notification.title}>
          <p className="govuk-body">{notification.message}</p>
        </GovUKNotificationBanner>
      )}

      <span className="govuk-caption-l">Section 3</span>
      <h1 className="govuk-heading-l">Wider service users</h1>

      <h2 className="govuk-heading-m">Tell us about your service users</h2>
      <p className="govuk-body">
        Provide information about the users of any wider services your local authority has delivered, or arranged to be delivered on its behalf, through your Best Start Family Hub
        and Healthy Babies Network over the past 3 months.
      </p>
      <p className="govuk-body">If you do not have any user data for this period, you can tell us that instead.</p>

      <h3 className="govuk-heading-s">How to provide service user numbers using the guided form</h3>
      <p className="govuk-body">
        Select the wider services category you want to report on from the list below. The guided form will take you through the data you need to enter, provide relevant guidance
        and check for errors as you go.
      </p>
      <p className="govuk-body">You can save your progress at any time and return later to complete your submission.</p>

      <h3 className="govuk-heading-s">About your list of wider services</h3>
      <p className="govuk-body">The services shown below have been pre-populated from your Core Wider Services List.</p>
      <p className="govuk-body">
        If a service is missing, or if a service is no longer delivered in your local authority, you can add, remove or edit services using the 'Manage categories' button.
      </p>

      <GovUKDetails summary="Help with Best Start Family Hub and Healthy Babies wider services">
        <p className="govuk-body">
          Wider services are additional services that support families beyond the core Family Hub offer. These may include services like debt advice, housing support, mental health
          services, and more.
        </p>
      </GovUKDetails>

      <hr className="govuk-section-break govuk-section-break--l govuk-section-break--visible" />

      <div className="wider-service-users__header">
        <h2 className="govuk-heading-m govuk-!-margin-bottom-0">Wider services categories</h2>
        <div className="wider-service-users__actions">
          <GovUKButton className="govuk-button govuk-button--secondary govuk-!-margin-bottom-0" onClick={handleManageCategories}>
            Manage categories
          </GovUKButton>
        </div>
      </div>

      <div className="govuk-grid-row govuk-!-margin-bottom-4">
        <div className="govuk-grid-column-two-thirds">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="search-categories">
              Search category by name
            </label>
            <div className="wider-service-users__search-wrapper">
              <input className="govuk-input" id="search-categories" name="search-categories" type="text" value={searchTerm} onChange={(e) => setSearchTerm(e.target.value)} />
              <button type="button" className="wider-service-users__search-button" aria-label="Search">
                <svg width="20" height="20" viewBox="0 0 20 20" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="8" cy="8" r="6" stroke="white" strokeWidth="2" />
                  <line x1="12.5" y1="12.5" x2="18" y2="18" stroke="white" strokeWidth="2" />
                </svg>
              </button>
            </div>
          </div>
        </div>
        <div className="govuk-grid-column-one-third">
          <div className="govuk-form-group">
            <label className="govuk-label" htmlFor="sort-categories">
              Sort categories
            </label>
            <select className="govuk-select govuk-!-width-full" id="sort-categories" name="sort-categories" value={sortBy} onChange={(e) => setSortBy(e.target.value)}>
              <option value="status">By status</option>
              <option value="name">By name</option>
            </select>
          </div>
        </div>
      </div>

      {filteredAndSortedCategories.length > 0 ? (
        <ul className="govuk-list wider-service-users__list">
          {filteredAndSortedCategories.map((category: WiderServiceCategoryStatus) => (
            <li key={category.serviceCategoryId} className="wider-service-users__list-item">
              <a
                href="#"
                className="govuk-link wider-service-users__link"
                onClick={(e) => {
                  e.preventDefault();
                  handleCategoryClick(category.serviceCategoryId);
                }}>
                {category.categoryName}
              </a>
              <GovUKTag colour={getSubmissionStatusTagColour(category.status)}>{category.status}</GovUKTag>
            </li>
          ))}
        </ul>
      ) : (
        <div className="govuk-inset-text">
          {searchTerm ? (
            <p className="govuk-body">No categories match your search.</p>
          ) : (
            <>
              <p className="govuk-body">
                You have not added any wider service categories. Based on this, you have nothing to report for Section 3 and this section is marked as complete.
              </p>
              <p className="govuk-body">If this is wrong, use 'Manage categories' above to add services you deliver.</p>
            </>
          )}
        </div>
      )}
    </GeneralLayout>
  );
};

export default WiderServiceUsersList;
