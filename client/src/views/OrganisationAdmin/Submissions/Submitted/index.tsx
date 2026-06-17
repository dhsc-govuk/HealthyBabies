import React from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { GeneralLayout } from '../../../../layouts';
import { GovUKButton, GovUKPanel } from '../../../../components/GovUKComponents';

interface LocationState {
  submissionName?: string;
}

const SubmissionSubmitted = (): React.ReactElement => {
  const navigate = useNavigate();
  const location = useLocation();
  useParams<{ submissionId: string }>();
  const locationState = location.state as LocationState | null;

  const submissionName = locationState?.submissionName || 'Your submission';

  return (
    <GeneralLayout
      breadcrumbs={[
        { label: 'Home', link: '/organisation-admin/home' },
        { label: 'Submissions', link: '/organisation-admin/submissions' },
      ]}
      currentPage="Submission complete">
      <GovUKPanel title="Submission complete">
        <p>{submissionName} has been submitted successfully.</p>
      </GovUKPanel>

      <h2 className="govuk-heading-m">What happens next</h2>
      <p className="govuk-body">We have received your submission. You will receive a confirmation email shortly.</p>
      <p className="govuk-body">
        If you have any questions, contact{' '}
        <a href="mailto:healthybabies.dataanddigital@dhsc.gov.uk" className="govuk-link">
          healthybabies.dataanddigital@dhsc.gov.uk
        </a>
      </p>

      <div className="govuk-button-group" style={{ marginTop: '30px' }}>
        <GovUKButton onClick={() => navigate('/organisation-admin/submissions')}>Return to my submissions</GovUKButton>
      </div>
    </GeneralLayout>
  );
};

export default SubmissionSubmitted;
