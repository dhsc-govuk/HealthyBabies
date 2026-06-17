import { timeOffsetToTimeOnly, timeOnlyToTimeOffset } from '../helpers/datetime';

export enum DayOfWeek {
  MONDAY = 'MONDAY',
  TUESDAY = 'TUESDAY',
  WEDNESDAY = 'WEDNESDAY',
  THURSDAY = 'THURSDAY',
  FRIDAY = 'FRIDAY',
  SATURDAY = 'SATURDAY',
  SUNDAY = 'SUNDAY',
}

export const DaysOfWeek = [DayOfWeek.SUNDAY, DayOfWeek.MONDAY, DayOfWeek.TUESDAY, DayOfWeek.WEDNESDAY, DayOfWeek.THURSDAY, DayOfWeek.FRIDAY, DayOfWeek.SATURDAY];

export interface DailyAvailability {
  dayOfWeek: string;
  notAvailable: boolean;
  startAt: string;
  endAt: string;
}

export function defaultAvailability(): DailyAvailability[] {
  return DaysOfWeek.map((day) => ({
    dayOfWeek: day,
    startAt: '09:00',
    endAt: '18:00',
    notAvailable: day === DayOfWeek.SATURDAY || day === DayOfWeek.SUNDAY ? true : false,
  }));
}

export function fromDailyAvailabilityPayload(item: DailyAvailability): DailyAvailability {
  return {
    dayOfWeek: item.dayOfWeek,
    notAvailable: item.notAvailable,
    startAt: timeOffsetToTimeOnly(item.startAt),
    endAt: timeOffsetToTimeOnly(item.endAt),
  };
}

export function toDailyAvailabilityPayload(item: DailyAvailability): DailyAvailability {
  return {
    dayOfWeek: item.dayOfWeek,
    notAvailable: item.notAvailable,
    startAt: timeOnlyToTimeOffset(item.startAt),
    endAt: timeOnlyToTimeOffset(item.endAt),
  };
}
