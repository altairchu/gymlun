import { type ProductFormValues } from './productSchema';
import { QuantityUnit } from './quantityUnit';

export const EMPTY_FORM_VALUES: ProductFormValues = {
  name: '',
  defaultQuantity: 100,
  quantityUnit: QuantityUnit.Grams,
  calories: 100,
  categories: [],
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  isFavorite: false,
};
