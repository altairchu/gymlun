import { productModel, type ProductSelectOption } from '@/entities/product';
import { type SelectedFood, caloriesOf, gramsOf, selectionReducer } from './selection';

const dumplings: ProductSelectOption = {
  id: 1,
  name: '豬肉韭菜餃',
  defaultQuantity: 25,
  quantityUnit: productModel.QuantityUnit.Servings,
  categoryIds: [],
  calories: 200,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: true,
};

const rice: ProductSelectOption = {
  ...dumplings,
  id: 2,
  name: '白飯',
  defaultQuantity: 200,
  quantityUnit: productModel.QuantityUnit.Grams,
  calories: 130,
};

const pick = (...products: ProductSelectOption[]): SelectedFood[] =>
  products.reduce<SelectedFood[]>(
    (items, product) => selectionReducer(items, { type: 'toggled', product }),
    [],
  );

test('a picked food starts at one serving or its usual grams, in its own unit', () => {
  expect(pick(dumplings, rice)).toStrictEqual<SelectedFood[]>([
    { product: dumplings, unit: productModel.QuantityUnit.Servings, amount: '1' },
    { product: rice, unit: productModel.QuantityUnit.Grams, amount: '200' },
  ]);
});

test('picking a food again removes it', () => {
  const items = selectionReducer(pick(dumplings, rice), { type: 'toggled', product: dumplings });

  expect(items.map(item => item.product.name)).toStrictEqual<string[]>(['白飯']);
});

test('each food keeps its own unit', () => {
  const items = selectionReducer(pick(dumplings, rice), {
    type: 'unitChanged',
    productId: rice.id,
    unit: productModel.QuantityUnit.Servings,
  });

  expect(items.map(item => [item.unit, item.amount])).toStrictEqual([
    [productModel.QuantityUnit.Servings, '1'],
    [productModel.QuantityUnit.Servings, '1'],
  ]);
});

test('ten servings of 25 g dumplings at 200 kcal per 100 g are 250 g and 500 kcal', () => {
  const items = selectionReducer(pick(dumplings), {
    type: 'amountChanged',
    productId: dumplings.id,
    amount: '10',
  });

  expect(items.map(gramsOf)).toStrictEqual([250]);
  expect(items.map(caloriesOf)).toStrictEqual([500]);
});
