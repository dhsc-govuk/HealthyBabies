import React from 'react';
import { Box } from '@mui/material';
import { SiteFormQuestionDto } from '../../../../components/Global/SiteForms';
import { LocationFormState, LocationReducerAction, ValidationErrors } from '../Common';
import DynamicQuestionRenderer from './DynamicQuestionRenderer';

interface LocationFormProps {
  state: LocationFormState;
  dispatch: React.Dispatch<{ type: LocationReducerAction; payload?: any }>;
  errors: ValidationErrors;
  questions: SiteFormQuestionDto[];
}

const LocationForm = ({
  state,
  dispatch,
  errors,
  questions,
}: LocationFormProps): React.ReactElement => {
  return (
    <Box>
      <DynamicQuestionRenderer
        questions={questions}
        state={state}
        dispatch={dispatch}
        errors={errors}
      />
    </Box>
  );
};

export default LocationForm;
