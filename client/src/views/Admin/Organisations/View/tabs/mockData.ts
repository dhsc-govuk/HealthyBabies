import { DeliverySummaryDto, OffTrackActivityDto } from '../queries';
import { ALLOCATION_STRANDS } from './constants';

/*
 * TEMPORARY STUB DATA — Delivery data tab only.
 * -------------------------------------------
 * The Delivery data tab has no agreed calculation spec yet (data.md only covers
 * the Financial data tab, which is now wired to the real endpoint). This generator
 * produces random-but-realistic data shaped like DeliverySummaryDto so the tab is
 * viewable. When the delivery calc is specced, swap the useMemo in DeliveryDataTab
 * back to getOrganisationDeliverySummary (still exported from queries.ts) and delete this file.
 *
 * Values are seeded from the filter inputs so they stay stable while you toggle
 * back and forth, but differ across strands / organisations.
 */

const hashSeed = (input: string): number => {
  let hash = 2166136261;
  for (let i = 0; i < input.length; i += 1) {
    hash ^= input.charCodeAt(i);
    hash = Math.imul(hash, 16777619);
  }
  return hash >>> 0;
};

/** Deterministic PRNG (mulberry32) so a given seed always yields the same sequence. */
const makeRng = (seed: number): (() => number) => {
  let state = seed;
  return () => {
    state |= 0;
    state = (state + 0x6d2b79f5) | 0;
    let t = Math.imul(state ^ (state >>> 15), 1 | state);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
};

const round100 = (value: number): number => Math.round(value / 100) * 100;

const ACTIVITY_NAMES = [
  'Family hub outreach sessions',
  'Parent-infant relationship workshops',
  'Home learning environment drop-ins',
  'SEND early support clinics',
  'Perinatal mental health peer support',
  'Infant feeding community café',
  'Healthy start vitamin distribution',
  'Antenatal education programme',
  'Targeted home visiting',
  'Community connector training',
];

export const generateDeliverySummary = (organisationId: string, strand: string): DeliverySummaryDto => {
  const rand = makeRng(hashSeed(`del|${organisationId}|${strand}`));
  const isAll = strand === 'all';
  const scale = isAll ? 1 : 0.35;

  const onTrackCount = Math.max(1, Math.round((isAll ? 14 + rand() * 12 : 1 + rand() * 5)));
  const offTrackCount = Math.round((isAll ? rand() * 6 : rand() * 2));
  const onTrackSpend = round100((180000 + rand() * 320000) * scale);
  const offTrackSpend = round100(rand() * 70000 * scale);

  const completed = Math.round((isAll ? 3 + rand() * 6 : rand() * 2));
  const delayed = Math.round((isAll ? rand() * 4 : rand() * 2));
  const notStarted = Math.round((isAll ? rand() * 5 : rand() * 2));
  const started = Math.max(0, onTrackCount + offTrackCount - completed - delayed - notStarted);

  const strandPool = isAll ? ALLOCATION_STRANDS : ALLOCATION_STRANDS.filter((s) => s.key === strand);
  const offTrackActivities: OffTrackActivityDto[] = Array.from({ length: offTrackCount }, () => {
    const pickStrand = strandPool[Math.floor(rand() * strandPool.length)] ?? ALLOCATION_STRANDS[0];
    return {
      module: pickStrand.key,
      strandDisplayName: pickStrand.label,
      activityName: ACTIVITY_NAMES[Math.floor(rand() * ACTIVITY_NAMES.length)],
      actualSpend: round100(5000 + rand() * 25000),
    };
  });

  return {
    strand,
    onTrack: { activityCount: onTrackCount, totalActualSpend: onTrackSpend },
    offTrack: { activityCount: offTrackCount, totalActualSpend: offTrackSpend },
    notStarted,
    started,
    delayed,
    completed,
    offTrackActivities,
  };
};
