import React from 'react';
import { LoadingBox } from 'govuk-react';

interface LoadingSpinnerProps {
  loading?: boolean;
  children?: React.ReactNode;
  label?: string;
}

const LoadingSpinner = ({ loading = true, children, label }: LoadingSpinnerProps): React.ReactElement => {
  return (
    <LoadingBox loading={loading} title={label}>
      {children}
    </LoadingBox>
  );
};

export default LoadingSpinner;
