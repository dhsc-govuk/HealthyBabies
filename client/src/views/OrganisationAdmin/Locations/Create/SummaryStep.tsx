import React from 'react';
import { SummaryList } from '../../../../components/GovUKComponents';
import { SiteFormQuestionDto } from '../../../../components/Global/SiteForms';
import { LocationFormState } from '../Common';

interface SummaryStepProps {
  state: LocationFormState;
  questions: SiteFormQuestionDto[];
  onChangeAll?: () => void;
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

const SummaryStep = ({ state, questions, onChangeAll }: SummaryStepProps): React.ReactElement => {
  const sortedQuestions = [...questions]
    .filter((q) => q.isActive)
    .sort((a, b) => a.displayOrder - b.displayOrder);

  const rows = sortedQuestions
    .filter((q) => isQuestionVisible(q, state.answers))
    .map((q) => ({
      label: q.label,
      value: getDisplayValue(q, state.answers[q.code]),
    }));

  return (
    <>
      {/* The GOV.UK Check answers pattern uses the Summary list component, which stacks to a
          single column below the tablet breakpoint. The previous <table> could not reflow, so
          at high zoom the columns collapsed and text fragmented mid-word
          (DAC_Styles_and_Layout_Usability_02). */}
      <SummaryList items={rows.map((row) => ({ ...row, onAction: onChangeAll, actionLabel: 'Change' }))} noOuterBorder />

      <h2 className="govuk-heading-m govuk-!-margin-top-6">Now save your site</h2>
      <p className="govuk-body">
        By saving delivery location, you are confirming that, to the best of your knowledge,
        the details you are providing are correct. You may return and update these
        details whenever necessary if anything changes.
      </p>
    </>
  );
};

export default SummaryStep;
