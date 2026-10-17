import { productSchema } from './productSchema';

const validProduct = {
  name: 'Oat granola',
  defaultQuantity: 100,
  category: { id: 1, name: 'Cereals' },
  calories: 412,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: false,
};

const getNameErrors = (name: string): string[] => {
  const result = productSchema.safeParse({ ...validProduct, name });
  return (result.error?.issues ?? [])
    .filter(issue => issue.path.join('.') === 'name')
    .map(issue => issue.message);
};

describe('productSchema name', () => {
  test.each([
    ['1 character', '粥'],
    ['2 characters padded with whitespace', '  餃子  '],
    ['100 characters', 'a'.repeat(100)],
    ['an emoji and a letter, 3 UTF-16 units', '😀a'],
  ])('should accept %s', (_, name) => {
    expect(getNameErrors(name)).toStrictEqual<string[]>([]);
  });

  test.each([
    ['an empty string', ''],
    ['only whitespace', '     '],
    ['101 characters', 'a'.repeat(101)],
    ['51 emoji, 102 UTF-16 units', '😀'.repeat(51)],
  ])('should reject %s', (_, name) => {
    expect(getNameErrors(name)).toStrictEqual<string[]>([
      'Product name must be between 1 and 100 characters',
    ]);
  });
});

test('should trim the parsed name', () => {
  const result = productSchema.safeParse({ ...validProduct, name: '  Oat granola  ' });

  expect(result.data?.name).toBe('Oat granola');
});
