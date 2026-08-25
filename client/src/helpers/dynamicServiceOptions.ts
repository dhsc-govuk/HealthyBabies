/**
 * Fields configured with {"dynamicOptions":"services"} take their options from the organisation's
 * services rather than a seeded list — Outcome Scores' PPS01 (single-select) and the delivery
 * plan's "Which service is the activity funding?" (multi-select).
 *
 * The option value is the service ID, not the name, so renaming a service never orphans an answer.
 */
export interface AvailableService {
  serviceId: string;
  serviceName: string;
}

interface OptionLike {
  value: string;
  label: string;
  displayOrder: number;
}

interface FieldLike {
  configuration: string | null;
  options?: OptionLike[];
}

export function isServiceBackedField(configuration: string | null): boolean {
  if (!configuration) return false;
  try {
    return (JSON.parse(configuration) as { dynamicOptions?: string }).dynamicOptions === 'services';
  } catch {
    return false;
  }
}

/**
 * Returns the field with its options replaced by the service list, or unchanged when the field
 * isn't service-backed. Returning the field untouched (rather than with empty options) matters:
 * an empty list would render a question with no answers and no explanation.
 */
export function withServiceOptions<T extends FieldLike>(field: T, services: AvailableService[] | undefined): T {
  if (!isServiceBackedField(field.configuration) || !services?.length) {
    return field;
  }

  return {
    ...field,
    options: services.map((service, index) => ({
      value: service.serviceId,
      label: service.serviceName,
      displayOrder: index + 1,
    })),
  };
}
