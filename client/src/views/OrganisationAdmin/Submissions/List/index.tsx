import React, { useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from 'react-query';
import { LoadingBox, Select } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKNotificationBanner, GovUKTaskList, H1, H2, Paragraph } from '../../../../components/GovUKComponents';
import type { FilterOption } from '../../../../components/GovUKComponents';
import SearchIcon from '../../../../components/Logos/SearchIcon';
import '../../../../components/GovUKComponents/GovUKTable/styles.css';
import { getSubmissions, SubmissionDto, DataCollectionTypeValue } from '../queries';
import { Box } from '@mui/material';
import { getSubmissionListDisplay } from '../../../../helpers/submissionStatusColour';
import { getDaysRemainingMessage } from '../../../../helpers/datetime';
import usePageTitle from '../../../../hooks/usePageTitle';
import { ROUTES } from '../../../../constants/routes';

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

const SORT_OPTIONS: FilterOption[] = [
  { value: 'name-asc', label: 'Name, A-Z' },
  { value: 'name-desc', label: 'Name, Z-A' },
  { value: 'date-desc', label: 'Latest first' },
  { value: 'date-asc', label: 'Earliest first' },
];

const TYPE_FILTER_OPTIONS: FilterOption[] = [
  { value: 'delivery-plan', label: 'Delivery plan' },
  { value: 'management-information', label: 'Management information' },
];

const sortSubmissions = (items: SubmissionDto[], sortValue: string): SubmissionDto[] => {
  const sorted = [...items];
  switch (sortValue) {
    case 'name-desc':
      return sorted.sort((a, b) => b.name.localeCompare(a.name));
    case 'date-desc':
      return sorted.sort((a, b) => new Date(b.endDate).getTime() - new Date(a.endDate).getTime());
    case 'date-asc':
      return sorted.sort((a, b) => new Date(a.endDate).getTime() - new Date(b.endDate).getTime());
    default:
      return sorted.sort((a, b) => a.name.localeCompare(b.name));
  }
};

const ListSubmissions = (): React.ReactElement => {
  usePageTitle('Submissions');
  const navigate = useNavigate();
  const { data, isLoading } = useQuery({
    queryKey: ['organisation-admin-submissions'],
    queryFn: getSubmissions,
  });

  const [searchTerm, setSearchTerm] = useState('');
  const [typeFilter, setTypeFilter] = useState<DataCollectionTypeValue | ''>('');
  const [sortValue, setSortValue] = useState('name-asc');

  const submissions = data?.data ?? [];

  const upcomingSubmissions = submissions.filter((s) => !s.isWindowClosed);
  const pastSubmissions = submissions.filter((s) => s.isWindowClosed);

  const visiblePastSubmissions = useMemo(() => {
    let result = pastSubmissions;

    if (searchTerm) {
      const lowerSearch = searchTerm.toLowerCase();
      result = result.filter((s) => s.name.toLowerCase().includes(lowerSearch));
    }

    if (typeFilter) {
      result = result.filter((s) => s.type === typeFilter);
    }

    return sortSubmissions(result, sortValue);
  }, [pastSubmissions, searchTerm, typeFilter, sortValue]);

  const nearestDeadline = upcomingSubmissions.length > 0 ? Math.min(...upcomingSubmissions.map((s) => s.daysRemaining)) : null;

  return (
    <LoadingBox loading={isLoading}>
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: ROUTES.organisationAdmin.home },
          { label: 'Submissions', link: ROUTES.organisationAdmin.submissions.list },
        ]}
        currentPage="">
        <H1>Submissions</H1>
        <Paragraph>Complete, review, and submit Delivery Plans and Management Information reports</Paragraph>

        <H2>Upcoming data collections</H2>

        {nearestDeadline !== null && nearestDeadline <= 7 && (
          <GovUKNotificationBanner type="important" title="Important">
            <p className="govuk-body">
              <strong>{getDaysRemainingMessage(nearestDeadline, 'your next submission', false)}</strong>
            </p>
            <p className="govuk-body">
              Contact{' '}
              <a href="mailto:healthybabies.dataanddigital@dhsc.gov.uk" className="govuk-link">
                healthybabies.dataanddigital@dhsc.gov.uk
              </a>{' '}
              if you think there&apos;s a problem.
            </p>
          </GovUKNotificationBanner>
        )}

        {upcomingSubmissions.length > 0 ? (
          <Box mb={2}>
            <GovUKTaskList
              items={upcomingSubmissions.map((s) => {
                const { statusText, colour, hint } = getSubmissionListDisplay(s.status, s.endDate, formatDate);
                return {
                  id: s.id,
                  title: s.name,
                  hint,
                  href: ROUTES.organisationAdmin.submissions.view(s.id),
                  onClick: () => navigate(ROUTES.organisationAdmin.submissions.view(s.id)),
                  status: {
                    text: statusText,
                    tag: colour !== 'white' ? { text: statusText, colour } : undefined,
                  },
                };
              })}
            />
          </Box>
        ) : (
          <Paragraph>No upcoming data collections.</Paragraph>
        )}

        <H2>Past data collections</H2>
        {pastSubmissions.length > 0 ? (
          <>
            <div className="govuk-grid-row govuk-table-filter">
              <div className="govuk-grid-column-one-quarter">
                <label htmlFor="past-submissions-search" className="govuk-label">
                  Search by name
                </label>
                <div className="govuk-table-filter__search">
                  <input
                    id="past-submissions-search"
                    type="search"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="govuk-input govuk-table-filter__input"
                  />
                  <button type="button" className="govuk-table-filter__search-button" aria-label="Search" onClick={() => {}}>
                    <SearchIcon />
                  </button>
                </div>
              </div>
              <div className="govuk-grid-column-one-half govuk-table-filter__filter">
                <Select
                  label="Filter by type"
                  input={{
                    value: typeFilter,
                    onChange: (e: React.ChangeEvent<HTMLSelectElement>) => setTypeFilter(e.target.value as DataCollectionTypeValue | ''),
                  }}>
                  <option value="">Select...</option>
                  {TYPE_FILTER_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="govuk-grid-column-one-quarter govuk-table-filter__sort">
                <label htmlFor="past-submissions-sort" className="govuk-label">
                  Sort by
                </label>
                <select id="past-submissions-sort" className="govuk-select" value={sortValue} onChange={(e) => setSortValue(e.target.value)}>
                  {SORT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {visiblePastSubmissions.length > 0 ? (
              <GovUKTaskList
                items={visiblePastSubmissions.map((s) => {
                  const { statusText, colour, hint } = getSubmissionListDisplay(s.status, s.endDate, formatDate);
                  return {
                    id: s.id,
                    title: s.name,
                    hint,
                    href: ROUTES.organisationAdmin.submissions.view(s.id),
                    onClick: () => navigate(ROUTES.organisationAdmin.submissions.view(s.id)),
                    status: {
                      text: statusText,
                      tag: colour !== 'white' ? { text: statusText, colour } : undefined,
                    },
                  };
                })}
              />
            ) : (
              <Paragraph>No data collections found.</Paragraph>
            )}
          </>
        ) : (
          <Paragraph>No past data collections.</Paragraph>
        )}
      </GeneralLayout>
    </LoadingBox>
  );
};

export default ListSubmissions;
