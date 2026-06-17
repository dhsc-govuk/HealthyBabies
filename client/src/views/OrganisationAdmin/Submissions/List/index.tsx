import React from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from 'react-query';
import { LoadingBox } from 'govuk-react';
import { GeneralLayout } from '../../../../layouts';
import { GovUKNotificationBanner, GovUKTaskList, H1, H2, Paragraph } from '../../../../components/GovUKComponents';
import { getSubmissions } from '../queries';
import { Box } from '@mui/material';
import { getSubmissionStatusTagColour } from '../../../../helpers/submissionStatusColour';
import usePageTitle from '../../../../hooks/usePageTitle';

const formatDate = (dateString: string): string => {
  const date = new Date(dateString);
  return date.toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
};

const ListSubmissions = (): React.ReactElement => {
  usePageTitle('Submissions');
  const navigate = useNavigate();
  const { data, isLoading } = useQuery({
    queryKey: ['organisation-admin-submissions'],
    queryFn: getSubmissions,
  });

  const submissions = data?.data ?? [];

  const upcomingSubmissions = submissions.filter((s) => new Date(s.endDate) >= new Date());
  const pastSubmissions = submissions.filter((s) => new Date(s.endDate) < new Date());

  const nearestDeadline = upcomingSubmissions.length > 0 ? Math.min(...upcomingSubmissions.map((s) => s.daysRemaining)) : null;

  return (
    <LoadingBox loading={isLoading}>
      <GeneralLayout
        breadcrumbs={[
          { label: 'Home', link: '/organisation-admin/home' },
          { label: 'Submissions', link: '/organisation-admin/submissions' },
        ]}
        currentPage="">
        <H1>Submissions</H1>
        <Paragraph>Complete, review, and submit Delivery Plans and Management Information reports</Paragraph>

        <H2>Upcoming submissions</H2>

        {nearestDeadline !== null && nearestDeadline <= 7 && (
          <GovUKNotificationBanner type="important" title="Important">
            <p className="govuk-body">
              <strong>You have {nearestDeadline} days left to send your next submission.</strong>
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
                const colour = getSubmissionStatusTagColour(s.status);
                return {
                  id: s.id,
                  title: s.name,
                  hint: `Submit by ${formatDate(s.endDate)}`,
                  href: `/organisation-admin/submissions/${s.id}`,
                  onClick: () => navigate(`/organisation-admin/submissions/${s.id}`),
                  status: {
                    text: s.status,
                    tag: colour !== 'white' ? { text: s.status, colour } : undefined,
                  },
                };
              })}
            />
          </Box>
        ) : (
          <Paragraph>No upcoming submissions.</Paragraph>
        )}

        <H2>Past submissions</H2>
        {pastSubmissions.length > 0 ? (
          <GovUKTaskList
            items={pastSubmissions.map((s) => {
              const colour = getSubmissionStatusTagColour(s.status);
              return {
                id: s.id,
                title: s.name,
                hint: `Submit by ${formatDate(s.endDate)}`,
                href: `/organisation-admin/submissions/${s.id}`,
                onClick: () => navigate(`/organisation-admin/submissions/${s.id}`),
                status: {
                  text: s.status,
                  tag: colour !== 'white' ? { text: s.status, colour } : undefined,
                },
              };
            })}
          />
        ) : (
          <Paragraph>No past submissions.</Paragraph>
        )}
      </GeneralLayout>
    </LoadingBox>
  );
};

export default ListSubmissions;
