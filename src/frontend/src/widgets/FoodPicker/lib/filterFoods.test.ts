import { productModel, type ProductSelectOption } from '@/entities/product';
import { EMPTY_FOOD_FILTER, filterFoods, toggleCategory } from './filterFoods';

const product = (
  id: number,
  name: string,
  categoryIds: number[],
  isFavorite = false,
): ProductSelectOption => ({
  id,
  name,
  categoryIds,
  isFavorite,
  defaultQuantity: 100,
  quantityUnit: productModel.QuantityUnit.Grams,
  calories: 100,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
});

const MEAT = 1;
const FROZEN = 2;
const HIGH_PROTEIN = 3;

const dumplings = product(1, '豬肉韭菜餃', [MEAT, FROZEN], true);
const chickenBreast = product(2, '雞胸肉', [MEAT, HIGH_PROTEIN], true);
const frozenPeas = product(3, 'Frozen peas', [FROZEN]);
const rice = product(4, '白飯', []);
const products = [dumplings, chickenBreast, frozenPeas, rice];

const names = (foods: ProductSelectOption[]): string[] => foods.map(food => food.name);

test('no filter shows every food', () => {
  expect(filterFoods(products, EMPTY_FOOD_FILTER)).toStrictEqual<ProductSelectOption[]>(products);
});

test('one category shows the foods in it', () => {
  const filter = toggleCategory(EMPTY_FOOD_FILTER, MEAT);

  expect(names(filterFoods(products, filter))).toStrictEqual<string[]>(['豬肉韭菜餃', '雞胸肉']);
});

test('several categories show only the foods in all of them', () => {
  const filter = toggleCategory(toggleCategory(EMPTY_FOOD_FILTER, MEAT), FROZEN);

  expect(names(filterFoods(products, filter))).toStrictEqual<string[]>(['豬肉韭菜餃']);
});

test('picking a category again stops filtering by it', () => {
  const filter = toggleCategory(toggleCategory(EMPTY_FOOD_FILTER, MEAT), MEAT);

  expect(filterFoods(products, filter)).toHaveLength(4);
});

test('favourites only shows the foods eaten often, within the picked categories', () => {
  const filter = { ...toggleCategory(EMPTY_FOOD_FILTER, FROZEN), favoritesOnly: true };

  expect(names(filterFoods(products, filter))).toStrictEqual<string[]>(['豬肉韭菜餃']);
});

test('a search finds foods by any part of the name, ignoring case', () => {
  expect(names(filterFoods(products, { ...EMPTY_FOOD_FILTER, query: '餃' }))).toStrictEqual<
    string[]
  >(['豬肉韭菜餃']);
  expect(names(filterFoods(products, { ...EMPTY_FOOD_FILTER, query: 'PEAS ' }))).toStrictEqual<
    string[]
  >(['Frozen peas']);
});
