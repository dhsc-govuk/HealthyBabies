import React from 'react';

interface NotFoundProps {
  goBack: (path: number) => void;
}

const NotFound = ({ goBack }: NotFoundProps): React.ReactElement => {
  return (
    <div style={{ textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', minHeight: '40vh' }}>
      <h1 className="govuk-heading-xl" style={{ marginBottom: '10px' }}>404</h1>
      <h2 className="govuk-heading-l">Page Not Found</h2>
      <p className="govuk-body">The page you are looking for does not exist.</p>
      <button
        type="button"
        className="govuk-button"
        onClick={() => goBack(-1)}
      >
        Go Back
      </button>
    </div>
  );
};

export default NotFound;
