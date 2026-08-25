import type { TagColour } from '../components/GovUKComponents/GovUKTag';

/**
 * Single source of truth for submission / task list status tag colours.
 * Matches GOV.UK Design System task list conventions:
 *   - Completed → white (plain text, no emphasis)
 *   - In progress → yellow
 *   - Not started → light-blue
 *   - Incomplete → light-blue (alias for "not started")
 *   - Draft → grey
 *   - Closed → white (same as completed — terminal state)
 */
export function getSubmissionStatusTagColour(status: string): TagColour {
  switch (status.toLowerCase().trim()) {
    case 'completed':
    case 'closed':
      return 'white';
    case 'in progress':
      return 'yellow';
    case 'not started':
    case 'incomplete':
      return 'light-blue';
    case 'draft':
      return 'grey';
    // Review states. "Reopened" is how a rejection is presented to users.
    case 'submitted':
      return 'blue';
    case 'under review':
      return 'purple';
    case 'approved':
      return 'green';
    case 'reopened':
    case 'rejected':
    case 'requires changes':
      return 'red';
    default:
      return 'grey';
  }
}

export interface SubmissionListDisplay {
  statusText: string;
  colour: TagColour;
  hint: string;
}

/**
 * A reopened collection reads as "Not started" on the submissions list — same tag and colour as a
 * fresh one, since that's exactly what the local authority needs to do next. The hint text is what
 * actually tells them it's a resubmission rather than a first-time one, and by how long.
 *
 * An approved collection reads as "Completed" — same plain, uncoloured treatment as "Closed" —
 * rather than "Approved" with its green tag.
 */
export function getSubmissionListDisplay(status: string, endDate: string, formatDate: (dateString: string) => string): SubmissionListDisplay {
  const isReopened = status.toLowerCase().trim() === 'reopened';
  const isApproved = status.toLowerCase().trim() === 'approved';
  const statusText = isReopened ? 'Not started' : isApproved ? 'Completed' : status;

  return {
    statusText,
    colour: getSubmissionStatusTagColour(statusText),
    hint: isReopened ? `This collection has been reopened. Resubmit by ${formatDate(endDate)}` : `Submit by ${formatDate(endDate)}`,
  };
}
