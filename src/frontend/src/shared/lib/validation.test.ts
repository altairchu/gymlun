import { validateCategoryName } from './validation';

describe('validateCategoryName', () => {
  test.each([
    ['a 1-character Chinese name', '飯'],
    ['a 2-character Chinese name', '肉類'],
    ['a name padded with whitespace', '  飯麵  '],
    ['64 characters', 'a'.repeat(64)],
  ])('should accept %s', (_, name) => {
    expect(validateCategoryName(name)).toBe(true);
  });

  test.each([
    ['an empty string', ''],
    ['only whitespace', '   '],
    ['65 characters', 'a'.repeat(65)],
  ])('should reject %s', (_, name) => {
    expect(validateCategoryName(name)).toBe(false);
  });
});
