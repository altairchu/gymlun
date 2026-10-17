import { type ProductFormValues, productSchema } from './productSchema';
import { QuantityUnit } from './quantityUnit';

const validProduct: ProductFormValues = {
  name: 'Oat granola',
  defaultQuantity: 100,
  quantityUnit: QuantityUnit.Grams,
  categories: [{ id: 1, name: 'Cereals' }],
  calories: 412,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: false,
};

const getErrors = (product: Partial<Record<keyof ProductFormValues, unknown>>): string[] => {
  const result = productSchema.safeParse({ ...validProduct, ...product });
  const [field] = Object.keys(product);
  return (result.error?.issues ?? [])
    .filter(issue => issue.path.at(0) === field)
    .map(issue => issue.message);
};

const givenCategories = (count: number): ProductFormValues['categories'] =>
  Array.from({ length: count }, (_, index) => ({ id: index + 1, name: `Category ${index + 1}` }));

describe('productSchema name', () => {
  test.each([
    ['1 character', '粥'],
    ['2 characters padded with whitespace', '  餃子  '],
    ['100 characters', 'a'.repeat(100)],
    ['an emoji and a letter, 3 UTF-16 units', '😀a'],
  ])('should accept %s', (_, name) => {
    expect(getErrors({ name })).toStrictEqual<string[]>([]);
  });

  test.each([
    ['an empty string', ''],
    ['only whitespace', '     '],
    ['101 characters', 'a'.repeat(101)],
    ['51 emoji, 102 UTF-16 units', '😀'.repeat(51)],
  ])('should reject %s', (_, name) => {
    expect(getErrors({ name })).toStrictEqual<string[]>(['食物名稱須為 1 至 100 個字元']);
  });
});

test('should trim the parsed name', () => {
  const result = productSchema.safeParse({ ...validProduct, name: '  Oat granola  ' });

  expect(result.data?.name).toBe('Oat granola');
});

describe('productSchema categories', () => {
  test.each([0, 1, 10])('should accept %i categories', count => {
    expect(getErrors({ categories: givenCategories(count) })).toStrictEqual<string[]>([]);
  });

  test('should reject more than 10 categories', () => {
    expect(getErrors({ categories: givenCategories(11) })).toStrictEqual<string[]>([
      '最多可選 10 個分類',
    ]);
  });
});

describe('productSchema defaultQuantity', () => {
  test.each([1, 25, 1000, '55'])('should accept %s', defaultQuantity => {
    expect(getErrors({ defaultQuantity })).toStrictEqual<string[]>([]);
  });

  test.each([0, 1001, 2.5, 'abc'])('should reject %s', defaultQuantity => {
    expect(getErrors({ defaultQuantity })).toStrictEqual<string[]>(['請輸入 1 至 1000 之間的整數']);
  });
});

describe('productSchema quantityUnit', () => {
  test.each([QuantityUnit.Grams, QuantityUnit.Servings])('should accept %i', quantityUnit => {
    expect(getErrors({ quantityUnit })).toStrictEqual<string[]>([]);
  });

  test('should reject an unknown unit', () => {
    expect(getErrors({ quantityUnit: 3 })).not.toStrictEqual<string[]>([]);
  });
});
