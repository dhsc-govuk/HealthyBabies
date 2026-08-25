// Canonical display order for data-collection form fields. Kept in one place so every screen
// agrees with the API, which orders fields by section first and display order second (see
// SubmissionsControllerService.GetServiceForm and friends).
//
// Sorting on displayOrder alone is only equivalent while displayOrder happens to be monotonic
// across the whole module, and nothing enforces that: questions created through the admin UI get
// a module-wide max+1 whatever section they are put in, and reordering rewrites the numbers. Once
// the two diverge, a flat sort interleaves sections and questions appear scattered through the
// review screen (BLUM-7219).
export interface OrderableFormField {
  stepNumber?: number | null;
  displayOrder: number;
}

/**
 * Compares two fields by section (stepNumber) then display order. Sectionless fields are treated
 * as section 0 to match the backend's `?? 0`; they cannot be answered in the wizard, which filters
 * by section, so in practice they never carry a value to show.
 */
export const compareFieldsForDisplay = <T extends OrderableFormField>(a: T, b: T): number => (a.stepNumber ?? 0) - (b.stepNumber ?? 0) || a.displayOrder - b.displayOrder;

/** Returns a new array in canonical display order. Never sorts the caller's array in place. */
export const sortFieldsForDisplay = <T extends OrderableFormField>(fields: readonly T[]): T[] => [...fields].sort(compareFieldsForDisplay);

// Grouping for the summary screens. The wizard's input renderer nests a gate question's inline
// children underneath it and buckets them by configuration.group, so the demographic breakdowns
// read as one block each (see FormFieldRenderer). The summary screens flatten the module into a
// single list instead, so they only agree with that while displayOrder happens to keep every
// group contiguous — and it does not: on live databases the age boxes were inserted straight
// through the sex boxes, so the two breakdowns interleave (BLUM-7222). Bucketing here anchors each
// group at its first member, which is what the migration also does to the numbers themselves.
export interface GroupableFormField extends OrderableFormField {
  code: string;
  label: string;
  configuration: string | null;
  conditionalRules: string | null;
}

/** One row group for a summary list: either a whole breakdown, or a single standalone question. */
export interface SummaryFieldGroup<T> {
  /** The configuration.group these fields share, or null for a standalone question. */
  groupKey: string | null;
  /** Heading for a breakdown: configuration.groupLabel, falling back to the first field's label. */
  label: string;
  /** The members that earned a row — a breakdown's unanswered boxes are not listed. */
  fields: T[];
}

interface SummaryFieldConfiguration {
  group?: string;
  groupLabel?: string;
}

interface SummaryConditionalRules {
  displayInline?: boolean;
  showWhen?: {
    fieldKey?: string;
    allOf?: Array<{ fieldKey?: string }>;
  };
}

const parseJson = <T,>(json: string | null): T | null => {
  if (!json) return null;
  try {
    return JSON.parse(json) as T;
  } catch {
    return null;
  }
};

/**
 * The question this field hangs off. `showWhen.fieldKey` covers the simple form; for `allOf`, the
 * last condition naming a field is the immediate gate — the same rule FormFieldRenderer applies
 * when it decides which radio to nest a field under, and the same one the display-order migrations
 * apply in SQL. Conditions keyed on `serviceFieldKey` describe the Service record rather than a
 * question on this form, so they resolve to null and leave the field a root.
 */
export const resolveGateFieldKey = (conditionalRules: string | null): string | null => {
  const showWhen = parseJson<SummaryConditionalRules>(conditionalRules)?.showWhen;
  if (showWhen?.fieldKey) return showWhen.fieldKey;

  const allOf = showWhen?.allOf;
  if (!Array.isArray(allOf)) return null;

  return [...allOf].reverse().find((condition) => !!condition?.fieldKey)?.fieldKey ?? null;
};

/**
 * Returns every field in the order the wizard asks the questions: each field, then the fields it
 * gates, depth first.
 *
 * The summary screens flatten the module into one list, so ordering them by display order alone
 * only agrees with the wizard while those numbers happen to keep every gate next to its children —
 * and nothing keeps them that way. A question created through the admin UI gets a module-wide
 * max + 1 whatever section it is filed under, so a breakdown added after the fact sorts away from
 * the question it breaks down and its answers appear scattered through the review screen. That is
 * BLUM-7219 and BLUM-7222, and repairing the numbers themselves has not held: BLUM-7602 is the
 * second retest failure. Deriving the order from the gates instead makes the interleave impossible
 * whatever the numbers say.
 *
 * A gate is honoured only inside its own section. The wizard asks each section as its own step, so
 * a question gated on an answer given two steps back is a top-level question there, not a nested
 * one; without the restriction the whole module collapses into a single tree rooted at the first
 * question and stepNumber stops meaning anything.
 */
export const orderFieldsForSummary = <T extends GroupableFormField>(fields: readonly T[]): T[] => {
  // First occurrence wins, so a duplicated code cannot make one field the parent of another's
  // children.
  const byCode = new Map<string, T>();
  fields.forEach((field) => {
    if (!byCode.has(field.code)) byCode.set(field.code, field);
  });

  const roots: T[] = [];
  const childrenOf = new Map<string, T[]>();

  fields.forEach((field) => {
    const gateKey = resolveGateFieldKey(field.conditionalRules);
    const gate = gateKey === null ? undefined : byCode.get(gateKey);

    // A gate outside the module, in another section, or pointing at the field itself leaves it a
    // root, where display order alone decides its place.
    if (!gate || gate === field || (gate.stepNumber ?? 0) !== (field.stepNumber ?? 0)) {
      roots.push(field);
      return;
    }

    const siblings = childrenOf.get(gate.code);
    if (siblings) {
      siblings.push(field);
    } else {
      childrenOf.set(gate.code, [field]);
    }
  });

  const ordered: T[] = [];
  // Keyed on identity rather than code so two fields sharing a code both survive the walk.
  const emitted = new Set<T>();

  const emit = (field: T): void => {
    if (emitted.has(field)) return;
    emitted.add(field);
    ordered.push(field);
    sortFieldsForDisplay(childrenOf.get(field.code) ?? []).forEach(emit);
  };

  sortFieldsForDisplay(roots).forEach(emit);
  // A gate cycle produces no root, so nothing above ever reaches it. This sweep is a no-op on
  // well-formed data and is what guarantees the walk can neither drop a field nor repeat one.
  sortFieldsForDisplay(fields).forEach(emit);

  return ordered;
};

/**
 * True when a standalone field should still be indented under the question above it — the
 * treatment an inline conditional answer, or a breakdown box left on its own because it was the
 * only one answered, has always had on these screens.
 */
export const isInlineSubAnswer = (field: GroupableFormField): boolean =>
  !!parseJson<SummaryFieldConfiguration>(field.configuration)?.group || !!parseJson<SummaryConditionalRules>(field.conditionalRules)?.displayInline;

/**
 * Buckets fields into summary groups, in the order the wizard asks them (see
 * orderFieldsForSummary). Fields sharing a configuration.group are emitted together at the position
 * of the first one reached in that walk, so a breakdown stays whole however its members are
 * numbered. A group of one is left as a standalone question: the wizard does the same (it only
 * draws a grouped fieldset for 2+ fields), and a lone box reads better under its own label than
 * under a heading repeating it.
 *
 * Pass the module's whole field list and say which ones earned a row through `shouldInclude`,
 * rather than filtering first. Only one box per breakdown carries the groupLabel that names the
 * question — always the first one, and every box is optional — so grouping a pre-filtered list
 * would lose the heading the moment an LA left the first band blank, and fall back to repeating the
 * next answered band's own label above it.
 */
export const groupFieldsForSummary = <T extends GroupableFormField>(fields: readonly T[], shouldInclude: (field: T) => boolean): SummaryFieldGroup<T>[] => {
  interface Bucket {
    groupKey: string | null;
    label: string;
    namedByGroupLabel: boolean;
    fields: T[];
  }

  const buckets = new Map<string, Bucket>();

  orderFieldsForSummary(fields).forEach((field) => {
    const config = parseJson<SummaryFieldConfiguration>(field.configuration);
    const groupKey = config?.group ?? null;
    // Prefixed so a group and a standalone field can share a name without colliding: the legacy
    // age boxes are grouped under "QSU13", which is also the code of the outcome-scores question.
    const bucketKey = groupKey ?? `field:${field.code}`;
    const bucket = buckets.get(bucketKey);

    if (!bucket) {
      buckets.set(bucketKey, {
        groupKey,
        label: config?.groupLabel ?? field.label,
        namedByGroupLabel: !!config?.groupLabel,
        fields: shouldInclude(field) ? [field] : [],
      });
      return;
    }

    if (!bucket.namedByGroupLabel && config?.groupLabel) {
      bucket.label = config.groupLabel;
      bucket.namedByGroupLabel = true;
    }

    if (shouldInclude(field)) {
      bucket.fields.push(field);
    }
  });

  return [...buckets.values()]
    .filter((bucket) => bucket.fields.length > 0)
    .map(({ groupKey, label, fields: included }) => ({ groupKey: included.length > 1 ? groupKey : null, label, fields: included }));
};
