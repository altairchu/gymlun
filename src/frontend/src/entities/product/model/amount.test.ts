import { amountToGrams, convertAmount, parseAmount, stepAmount } from './amount';
import { QuantityUnit } from './quantityUnit';

test.each([
  ['10', 10],
  ['1.5', 1.5],
  ['1,5', 1.5],
  [' 2 ', 2],
  ['', null],
  ['0', null],
  ['-1', null],
  ['abc', null],
])('the amount "%s" reads as %s', (amount, expected) => {
  expect(parseAmount(amount)).toBe(expected);
});

test('ten servings of 25 g are 250 g', () => {
  expect(amountToGrams('10', QuantityUnit.Servings, 25)).toBe(250);
});

test('half a serving of 25 g rounds to 13 g', () => {
  expect(amountToGrams('0.5', QuantityUnit.Servings, 25)).toBe(13);
});

test('more than 5000 g is not a valid amount', () => {
  expect(amountToGrams('201', QuantityUnit.Servings, 25)).toBeNull();
  expect(amountToGrams('5001', QuantityUnit.Grams, 25)).toBeNull();
});

test('switching ten servings of 25 g to grams shows 250', () => {
  expect(convertAmount('10', QuantityUnit.Servings, QuantityUnit.Grams, 25)).toBe('250');
});

test('switching 300 g to servings of 200 g shows 1.5', () => {
  expect(convertAmount('300', QuantityUnit.Grams, QuantityUnit.Servings, 200)).toBe('1.5');
});

test('switching an empty amount to another unit starts from one serving', () => {
  expect(convertAmount('', QuantityUnit.Servings, QuantityUnit.Grams, 25)).toBe('25');
  expect(convertAmount('', QuantityUnit.Grams, QuantityUnit.Servings, 25)).toBe('1');
});

test.each<[string, QuantityUnit, 1 | -1, string]>([
  ['1', QuantityUnit.Servings, 1, '2'],
  ['2', QuantityUnit.Servings, -1, '1'],
  ['1', QuantityUnit.Servings, -1, '0.5'],
  ['0.5', QuantityUnit.Servings, 1, '1'],
  ['', QuantityUnit.Servings, 1, '1'],
  ['200', QuantityUnit.Grams, 1, '210'],
  ['200', QuantityUnit.Grams, -1, '190'],
  ['5', QuantityUnit.Grams, -1, '1'],
])('stepping %s (unit %s) by %s gives %s', (amount, unit, direction, expected) => {
  expect(stepAmount(amount, unit, direction)).toBe(expected);
});
