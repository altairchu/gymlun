import { findQuickTimeAt, formatTime, parseTime, snapTime, toApiTime } from './time';

test.each([
  ['08:00', 480],
  ['12:30:00', 750],
  ['00:00', 0],
  ['23:59', 1439],
])('%s is %i minutes after midnight', (time, minutes) => {
  expect(parseTime(time)).toBe(minutes);
});

test('minutes after midnight format as a 24-hour time', () => {
  expect(formatTime(5)).toBe('00:05');
  expect(formatTime(1170)).toBe('19:30');
  expect(toApiTime(1170)).toBe('19:30:00');
});

test('a dragged time snaps to 5 minutes and stays within the day', () => {
  expect(snapTime(482.4)).toBe(480);
  expect(snapTime(483)).toBe(485);
  expect(snapTime(-30)).toBe(0);
  expect(snapTime(1500)).toBe(1435);
});

test.each([
  ['07:30', '早餐'],
  ['11:45', '午餐'],
  ['14:00', '午餐'],
  ['14:30', '下午茶'],
  ['18:30', '晚餐'],
  ['21:30', '宵夜'],
  ['02:00', '宵夜'],
])('at %s the meal being logged is %s', (time, label) => {
  expect(findQuickTimeAt(parseTime(time)).label).toBe(label);
});
