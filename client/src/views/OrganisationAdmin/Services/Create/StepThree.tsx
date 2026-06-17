import React from 'react';
import { Box, Typography } from '@mui/material';
import { useQuery } from 'react-query';
import axios from 'axios';
import { SummaryList } from '../../../../components/GovUKComponents';
import { ServiceFormQuestionDto } from '../../../../components/Global/Services';
import { ServiceFormState, shouldSkipServiceCharacteristics } from '../Common';
import { useAuthProvider } from '../../../../components/AuthProvider';
import './StepThree.css';

interface LocationDto {
  id: string;
  name: string;
}

interface StepThreeProps {
  state: ServiceFormState;
  questions: ServiceFormQuestionDto[];
}

const getDisplayValue = (
  question: ServiceFormQuestionDto,
  value: string | undefined,
  locations: LocationDto[]
): string => {
  if (!value) return 'Not specified';

  // Special handling for SMD19 - resolve location IDs to names
  if (question.code === 'SMD19') {
    const locationIds = value.split(',');
    const locationNames = locationIds
      .map((id) => {
        const location = locations.find((loc) => loc.id === id);
        return location?.name || id;
      })
      .filter(Boolean);
    return locationNames.length > 0 ? locationNames.join(', ') : 'Not specified';
  }

  // For questions with options, convert value to label.
  // Greedy parse handles option values that contain commas (e.g. SMD10 "Strengthening Families,
  // Strengthening Communities") and trailing spaces (e.g. SMD06 "As needed ", SMD16 "A telephone helpline ").
  if (question.options && question.options.length > 0) {
    const parts = value.split(',').map((v) => v.trim()).filter(Boolean);
    const labels: string[] = [];
    let i = 0;
    while (i < parts.length) {
      let matched = false;
      for (let j = parts.length; j > i; j--) {
        const candidate = parts.slice(i, j).join(', ');
        const option = question.options.find(
          (opt) => opt.value.trim() === candidate || opt.label.trim() === candidate,
        );
        if (option) {
          labels.push(option.label);
          i = j;
          matched = true;
          break;
        }
      }
      if (!matched) {
        labels.push(parts[i]);
        i++;
      }
    }
    if (labels.length > 0) return labels.join(', ');
  }

  return value;
};

const isQuestionVisible = (
  question: ServiceFormQuestionDto,
  answers: Record<string, string | undefined>
): boolean => {
  if (!question.conditionalQuestionCode || !question.conditionalValue) {
    return true;
  }
  const parentValue = answers[question.conditionalQuestionCode] || '';
  const conditionalValues = question.conditionalValue.split(',').map((v) => v.trim());

  // For checkbox parent questions, check if any of the selected values match any conditional value
  const parentSelectedValues = parentValue.split(',').map((v) => v.trim());
  return conditionalValues.some((cv) => parentSelectedValues.includes(cv));
};

const StepThree = ({ state, questions }: StepThreeProps): React.ReactElement => {
  const { organisationId } = useAuthProvider();

  // Fetch locations for SMD19 display
  const { data: locationsData } = useQuery(
    ['locations', organisationId],
    () => axios.get<LocationDto[]>(`/organisations/${organisationId}/locations`),
    {
      enabled: !!organisationId,
      staleTime: 5 * 60 * 1000,
    }
  );

  const locations = locationsData?.data ?? [];

  const skipStepTwo = shouldSkipServiceCharacteristics(state);

  // Sort questions by step and display order, excluding step 2 when skipped
  const sortedQuestions = [...questions]
    .filter((q) => q.isActive && (!skipStepTwo || q.step !== 2))
    .sort((a, b) => {
      if (a.step !== b.step) return a.step - b.step;
      return a.displayOrder - b.displayOrder;
    });

  // Build summary items
  const summaryItems: { label: string; value: string }[] = [];

  // Always add service name first
  summaryItems.push({
    label: 'Service name',
    value: state.name || 'Not specified',
  });

  // Add answers for each visible question
  for (const question of sortedQuestions) {
    // Skip name question (already added)
    if (question.code === 'SMD01') continue;

    // Check if question should be visible based on conditional logic
    if (!isQuestionVisible(question, state.answers)) continue;

    const value = state.answers[question.code];
    summaryItems.push({
      label: question.label,
      value: getDisplayValue(question, value, locations),
    });
  }

  return (
    <Box className="step-three-summary">
      <SummaryList items={summaryItems} />

      <Box sx={{ mt: 5 }}>
        <Typography variant="h5" sx={{ mb: 2, fontWeight: 700 }}>
          Now save your service
        </Typography>
        <Typography variant="body1" sx={{ mb: 2 }}>
          By saving the service, you are confirming that, to the best of your knowledge,
          the details you are providing are correct. You may return and update these
          details whenever necessary if anything changes.
        </Typography>
        <Typography variant="body1">
          Please review the information regularly and ensure it is up to date, especially
          before the Delivery Plan and Management Information submissions.
        </Typography>
      </Box>
    </Box>
  );
};

export default StepThree;