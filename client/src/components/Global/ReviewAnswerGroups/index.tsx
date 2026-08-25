import React from 'react';

import { GovUKSummaryCard, SummaryList } from '../../GovUKComponents';

/** Answers grouped by the form section they belong to. Each group renders as one summary card. */
export interface ReviewAnswerGroup {
  sectionId: string | null;
  title: string | null;
  answers: ReviewAnswer[];
}

export interface ReviewAnswer {
  questionCode: string;
  question: string;
  value: string | null;
  fieldType: string;
}

/** Shown in place of an empty answer so a blank cell is never mistaken for a rendering fault. */
export const NOT_ANSWERED = 'Not answered';

/**
 * Question and value pairs, one summary card per form section. Used for both the live admin
 * review of a submitted Delivery Plan and the read-only view of a previous submission version -
 * both shapes present the same grouped question/answer data.
 */
export function AnswerGroups({ groups }: { groups: ReviewAnswerGroup[] }): React.ReactElement {
  if (groups.length === 0) {
    return <p className="govuk-body">No answers have been submitted for this section.</p>;
  }

  return (
    <>
      {groups.map((group, index) => (
        <GovUKSummaryCard key={group.sectionId ?? `group-${index}`} title={group.title ?? undefined}>
          <SummaryList
            noOuterBorder
            halfWidthColumns
            items={group.answers.map((answer) => ({
              label: answer.question,
              value: answer.value ? answer.value : <span className="govuk-hint govuk-!-margin-bottom-0">{NOT_ANSWERED}</span>,
            }))}
          />
        </GovUKSummaryCard>
      ))}
    </>
  );
}
