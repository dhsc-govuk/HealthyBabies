import React from 'react';
import { Box } from '@mui/material';
import { GovUKFieldset } from '../../../../components/GovUKComponents';
import { ServiceFormQuestionDto } from '../../../../components/Global/Services';
import { ServiceFormState, ServiceReducerAction, ValidationErrors } from '../Common';
import DynamicQuestionRenderer from './DynamicQuestionRenderer';

interface StepOneProps {
  state: ServiceFormState;
  dispatch: React.Dispatch<{ type: ServiceReducerAction; payload?: any }>;
  errors: ValidationErrors;
  questions: ServiceFormQuestionDto[];
}

const StepOne = ({ state, dispatch, errors, questions }: StepOneProps): React.ReactElement => {
  // Get the name question for its hint
  const nameQuestion = questions.find((q) => q.code === 'SMD01');

  // Filter questions for step 1 only
  const stepOneQuestions = questions.filter((q) => q.step === 1);

  return (
    <Box>
      {/* Service name is always first and handled separately */}
      <GovUKFieldset.Input
        id="service-name"
        name="name"
        label={nameQuestion?.label || 'What is the service name?'}
        questionCode="SMD01"
        value={state.name}
        error={errors.name}
        hint={nameQuestion?.hint || 'Tell us what the service is called. Service names must be unique.'}
        onChange={(e) => dispatch({ type: ServiceReducerAction.SET_NAME, payload: e.target.value })}
      />

      {/* Render remaining step 1 questions dynamically */}
      <Box sx={{ mt: 3 }}>
        <DynamicQuestionRenderer
          questions={stepOneQuestions}
          state={state}
          dispatch={dispatch}
          errors={errors}
        />
      </Box>
    </Box>
  );
};

export default StepOne;
