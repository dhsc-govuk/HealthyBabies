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
    default:
      return 'grey';
  }
}
