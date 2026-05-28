import dayjs from 'dayjs';
import moment from 'moment';

export function timeOffsetToTimeOnly(offset: string) {
  return moment.utc(moment.duration(offset).as('milliseconds')).format('HH:mm');
}

export function timeOnlyToTimeOffset(time: string) {
  const duration = moment.duration(time);
  return `PT${duration.hours()}H${duration.minutes()}M`;
}

export function timeOffsetToDateTime(date: Date, offset: string) {
  const duration = moment.duration(offset);
  date.setHours(duration.hours());
  date.setMinutes(duration.minutes());
  return date;
}

export const parseDate = (dateStr: string) => {
  if (!dateStr) return new Date(0);
  const [day, month, year, hour, minute] = dateStr.split(/\/| |:/).map(Number);
  return new Date(year, month - 1, day, hour, minute);
};

export const sortComparator = (left: string, right: string) => (dayjs(left).isBefore(right) ? 1 : 0);
