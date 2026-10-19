import { formatReps, formatSetNumber, formatWeight } from './formatting';

test('sets are numbered from one', () => {
  expect(formatSetNumber(0)).toBe('第 1 組');
});

test('reps are counted in times', () => {
  expect(formatReps(10)).toBe('10 次');
});

test.each<[number, string]>([
  [60, '60 kg'],
  [62.5, '62.5 kg'],
  [0, '自身重量'],
])('weight %s kg is shown as %s', (weightKg, expected) => {
  expect(formatWeight(weightKg)).toBe(expected);
});
