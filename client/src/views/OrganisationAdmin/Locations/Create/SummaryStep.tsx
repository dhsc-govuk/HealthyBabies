import React from 'react';
import { Box, Typography } from '@mui/material';
import { SummaryList } from '../../../../components/GovUKComponents';
import { SiteFormQuestionDto } from '../../../../components/Global/SiteForms';
import { LocationFormState } from '../Common';

interface SummaryStepProps {
  state: LocationFormState;
  questions: SiteFormQuestionDto[];
}

const getDisplayValue = (
  question: SiteFormQuestionDto,
  value: string | undefined
): string => {
  if (!value) return 'Not specified';

  if (question.options && question.options.length > 0) {
    if (value.includes(',')) {
      const values = value.split(',');
      const labels = values
        .map((v) => {
          const option = question.options.find((opt) => opt.value === v);
          return option?.label || v;
        })
        .filter(Boolean);
      return labels.join(', ');
    }

    const option = question.options.find((opt) => opt.value === value);
    return option?.label || value;
  }

  return value;
};

const isQuestionVisible = (
  question: SiteFormQuestionDto,
  answers: Record<string, string | undefined>
): boolean => {
  if (!question.conditionalQuestionCode || !question.conditionalValue) {
    return true;
  }
  const parentValue = answers[question.conditionalQuestionCode];
  return parentValue === question.conditionalValue;
};

const SummaryStep = ({ state, questions }: SummaryStepProps): React.ReactElement => {
  // Include all active questions including predefined FHS01/02/03
  const sortedQuestions = [...questions]
    .filter((q) => q.isActive)
    .sort((a, b) => a.displayOrder - b.displayOrder);

  const summaryItems: { label: string; value: string }[] = [];

  for (const question of sortedQuestions) {
    if (!isQuestionVisible(question, state.answers)) continue;

    const value = state.answers[question.code];
    summaryItems.push({
      label: question.label,
      value: getDisplayValue(question, value),
    });
  }

  return (
    <Box>
      <SummaryList items={summaryItems} />

      <Box sx={{ mt: 5 }}>
        <Typography variant="h5" sx={{ mb: 2, fontWeight: 700 }}>
          Now save your site
        </Typography>
        <Typography variant="body1" sx={{ mb: 2 }}>
          By saving delivery location, you are confirming that, to the best of your knowledge,
          the details you are providing are correct. You may return and update these
          details whenever necessary if anything changes.
        </Typography>
        {/* <Typography variant="body1">
          Please review the information regularly and ensure it is up to date, especially
          before the Delivery Plan and Management Information submissions.
        </Typography> */}
      </Box>
    </Box>
  );
};

export default SummaryStep;
