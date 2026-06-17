import React, { useMemo } from 'react';
import { Box } from '@mui/material';
import { useQuery } from 'react-query';
import axios from 'axios';
import { ServiceFormQuestionDto } from '../../../../components/Global/Services';
import { ServiceFormState, ServiceReducerAction, ValidationErrors } from '../Common';
import DynamicQuestionRenderer from './DynamicQuestionRenderer';
import { useAuthProvider } from '../../../../components/AuthProvider';

interface LocationDto {
  id: string;
  name: string;
  isActive: boolean;
}

interface StepTwoProps {
  state: ServiceFormState;
  dispatch: React.Dispatch<{ type: ServiceReducerAction; payload?: any }>;
  errors: ValidationErrors;
  questions: ServiceFormQuestionDto[];
}

const StepTwo = ({ state, dispatch, errors, questions }: StepTwoProps): React.ReactElement => {
  const { organisationId } = useAuthProvider();

  // Fetch locations for SMD19 - always fetch so they're ready when needed
  const { data: locationsData } = useQuery(
    ['locations', organisationId],
    () => axios.get<LocationDto[]>(`/organisations/${organisationId}/locations`),
    {
      enabled: !!organisationId,
      staleTime: 5 * 60 * 1000,
    }
  );

  const locations = locationsData?.data ?? [];

  // Filter questions for step 2 only and inject location options for SMD19
  const stepTwoQuestions = useMemo(() => {
    return questions
      .filter((q) => q.step === 2)
      .map((q) => {
        if (q.code === 'SMD19') {
          // Inject all active locations as options
          // The conditional visibility is handled by DynamicQuestionRenderer using conditionalQuestionCode/conditionalValue
          const activeLocations = locations.filter((loc) => loc.isActive);
          
          if (activeLocations.length === 0) {
            return {
              ...q,
              options: [],
              hint: 'No sites have been registered for your organisation. Please add sites first.',
            };
          }
          
          return {
            ...q,
            options: activeLocations.map((loc, index) => ({
              id: loc.id,
              value: loc.id,
              label: loc.name,
              displayOrder: index + 1,
            })),
          };
        }
        return q;
      });
  }, [questions, locations]);

  return (
    <Box>
      <DynamicQuestionRenderer
        questions={stepTwoQuestions}
        state={state}
        dispatch={dispatch}
        errors={errors}
      />
    </Box>
  );
};

export default StepTwo;
