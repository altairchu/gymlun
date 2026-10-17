export const MINUTES_IN_DAY = 24 * 60;

const pad = (value: number): string => value.toString().padStart(2, '0');

/** Minutes after midnight for an `HH:mm` or `HH:mm:ss` time */
export const parseTime = (time: string): number => {
  const [hours = 0, minutes = 0] = time.split(':').map(Number);
  return hours * 60 + minutes;
};

export const formatTime = (minutes: number): string =>
  `${pad(Math.floor(minutes / 60))}:${pad(minutes % 60)}`;

export const toApiTime = (minutes: number): string => `${formatTime(minutes)}:00`;

export const clampTime = (minutes: number): number =>
  Math.min(Math.max(Math.round(minutes), 0), MINUTES_IN_DAY - 1);

export const snapTime = (minutes: number, step = 5): number =>
  Math.floor(clampTime(Math.round(minutes / step) * step) / step) * step;

export const minutesOfDate = (date: Date): number => date.getHours() * 60 + date.getMinutes();

interface Timed {
  id: number;
  time: string;
  displayOrder: number;
}

export const compareByTime = (a: Timed, b: Timed): number =>
  parseTime(a.time) - parseTime(b.time) || a.displayOrder - b.displayOrder || a.id - b.id;

export const sortByTime = <T extends Timed>(items: T[]): T[] => [...items].sort(compareByTime);

export interface QuickTime {
  label: string;
  minutes: number;
}

export const QUICK_TIMES: QuickTime[] = [
  { label: '早餐', minutes: 8 * 60 },
  { label: '午餐', minutes: 12 * 60 + 30 },
  { label: '下午茶', minutes: 15 * 60 + 30 },
  { label: '晚餐', minutes: 19 * 60 },
  { label: '宵夜', minutes: 22 * 60 },
];

/** The meal being logged at a time of day; a meal counts as started an hour before its usual time */
export const findQuickTimeAt = (minutes: number): QuickTime => {
  const startOf = (quickTime: QuickTime): number => quickTime.minutes - 60;
  const started = QUICK_TIMES.filter(quickTime => startOf(quickTime) <= minutes);
  return started.at(-1) ?? QUICK_TIMES[QUICK_TIMES.length - 1];
};
