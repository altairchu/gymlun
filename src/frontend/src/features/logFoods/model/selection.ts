import { productModel, type ProductSelectOption } from '@/entities/product';

export interface SelectedFood {
  product: ProductSelectOption;
  unit: productModel.QuantityUnit;
  amount: string;
}

export type SelectionAction =
  | { type: 'toggled'; product: ProductSelectOption }
  | { type: 'amountChanged'; productId: number; amount: string }
  | { type: 'amountStepped'; productId: number; direction: 1 | -1 }
  | { type: 'unitChanged'; productId: number; unit: productModel.QuantityUnit }
  | { type: 'removed'; productId: number }
  | { type: 'cleared' };

const select = (product: ProductSelectOption): SelectedFood => ({
  product,
  unit: product.quantityUnit,
  amount: productModel.defaultAmount(product.quantityUnit, product.defaultQuantity),
});

const updateItem = (
  items: SelectedFood[],
  productId: number,
  update: (item: SelectedFood) => SelectedFood,
): SelectedFood[] => items.map(item => (item.product.id === productId ? update(item) : item));

export const selectionReducer = (
  items: SelectedFood[],
  action: SelectionAction,
): SelectedFood[] => {
  switch (action.type) {
    case 'toggled':
      return items.some(item => item.product.id === action.product.id)
        ? items.filter(item => item.product.id !== action.product.id)
        : [...items, select(action.product)];
    case 'amountChanged':
      return updateItem(items, action.productId, item => ({ ...item, amount: action.amount }));
    case 'amountStepped':
      return updateItem(items, action.productId, item => ({
        ...item,
        amount: productModel.stepAmount(item.amount, item.unit, action.direction),
      }));
    case 'unitChanged':
      return updateItem(items, action.productId, item => ({
        ...item,
        unit: action.unit,
        amount: productModel.convertAmount(
          item.amount,
          item.unit,
          action.unit,
          item.product.defaultQuantity,
        ),
      }));
    case 'removed':
      return items.filter(item => item.product.id !== action.productId);
    case 'cleared':
      return [];
  }
};

export const gramsOf = ({ amount, unit, product }: SelectedFood): number | null =>
  productModel.amountToGrams(amount, unit, product.defaultQuantity);

export const caloriesOf = (item: SelectedFood): number => {
  const grams = gramsOf(item);
  return grams === null ? 0 : productModel.caloriesOf(item.product.calories, grams);
};
