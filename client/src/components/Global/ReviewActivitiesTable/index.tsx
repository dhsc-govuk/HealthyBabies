import React from 'react';
import { Link } from 'react-router-dom';

import { GovUKTag, TagColour } from '../../GovUKComponents';

/** One row of an activity-based section's activity list, e.g. an admin's Delivery Plan review. */
export interface ReviewActivityRow {
  activityId: string;
  activityName: string;
  activityStatus: string | null;
  /** Raw option value, e.g. 'on_track'. Used for tag colour so we never match on English text. */
  activityStatusValue: string | null;
  deliveryStatus: string | null;
  deliveryStatusValue: string | null;
}

/**
 * Delivery status is seeded with exactly two options across every activity module, so the colour is
 * keyed off the stored value rather than the label. Off track uses the yellow tag per the Figma design
 * — it flags attention, not an error. An unrecognised or unanswered value falls back to grey rather
 * than implying a state the local authority did not select.
 */
export const getDeliveryStatusTagColour = (deliveryStatusValue: string | null): TagColour => {
  switch (deliveryStatusValue) {
    case 'on_track':
      return 'green';
    case 'off_track':
      return 'yellow';
    default:
      return 'grey';
  }
};

interface ActivitiesTableProps {
  activities: ReviewActivityRow[];
  activityHref: (activityId: string) => string;
}

/**
 * Activity status and delivery status are answers the local authority selected, not submission
 * workflow state. Rollover Spend is seeded without an activity status question, so that column is
 * legitimately empty for section 9. Shared by the live admin review and the read-only view of a
 * previous submission version.
 */
export function ActivitiesTable({ activities, activityHref }: ActivitiesTableProps): React.ReactElement {
  if (activities.length === 0) {
    return <p className="govuk-body">No activities have been added to this section.</p>;
  }

  return (
    <table className="govuk-table">
      <caption className="govuk-visually-hidden">Activities in this section</caption>
      <thead className="govuk-table__head">
        <tr className="govuk-table__row">
          <th scope="col" className="govuk-table__header">
            Activity
          </th>
          <th scope="col" className="govuk-table__header">
            Activity status
          </th>
          <th scope="col" className="govuk-table__header govuk-table__header--numeric">
            Delivery status
          </th>
        </tr>
      </thead>
      <tbody className="govuk-table__body">
        {activities.map((activity) => (
          <tr key={activity.activityId} className="govuk-table__row">
            <th scope="row" className="govuk-table__header">
              <Link className="govuk-link govuk-link--no-visited-state" to={activityHref(activity.activityId)}>
                {activity.activityName}
              </Link>
            </th>
            <td className="govuk-table__cell">{activity.activityStatus ?? ''}</td>
            <td className="govuk-table__cell govuk-table__cell--numeric">
              {activity.deliveryStatus && <GovUKTag colour={getDeliveryStatusTagColour(activity.deliveryStatusValue)}>{activity.deliveryStatus}</GovUKTag>}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
