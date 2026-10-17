import { useMemo, useReducer } from 'react';
import { type ProductSelectOption, type productModel } from '@/entities/product';
import { type SelectedFood, selectionReducer } from '../model';

export interface FoodSelection {
  items: SelectedFood[];
  toggle: (product: ProductSelectOption) => void;
  changeAmount: (productId: number, amount: string) => void;
  stepAmount: (productId: number, direction: 1 | -1) => void;
  changeUnit: (productId: number, unit: productModel.QuantityUnit) => void;
  remove: (productId: number) => void;
  clear: () => void;
}

export const useFoodSelection = (): FoodSelection => {
  const [items, dispatch] = useReducer(selectionReducer, []);

  return useMemo(
    () => ({
      items,
      toggle: product => dispatch({ type: 'toggled', product }),
      changeAmount: (productId, amount) => dispatch({ type: 'amountChanged', productId, amount }),
      stepAmount: (productId, direction) =>
        dispatch({ type: 'amountStepped', productId, direction }),
      changeUnit: (productId, unit) => dispatch({ type: 'unitChanged', productId, unit }),
      remove: productId => dispatch({ type: 'removed', productId }),
      clear: () => dispatch({ type: 'cleared' }),
    }),
    [items],
  );
};
