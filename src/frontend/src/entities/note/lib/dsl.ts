import { productModel } from '@/entities/product';
import { type Product, type NoteItem } from '../api';
import { MealType } from '../model';

const defaultProduct: Product = {
  id: 1,
  name: 'Sample product',
  defaultQuantity: 100,
  quantityUnit: productModel.QuantityUnit.Grams,
  calories: 100,
  protein: null,
  fats: null,
  carbs: null,
  sugar: null,
  salt: null,
  photoUrl: null,
};

export const create = {
  note: ({
    id = 1,
    time = '08:00:00',
    product = defaultProduct,
    productQuantity = 100,
  }: Partial<NoteItem>): NoteItem => ({
    id,
    date: '2025-06-10',
    time,
    mealType: MealType.Breakfast,
    displayOrder: 1,
    productQuantity,
    product,
  }),
  product: (overrides: Partial<Product> = {}): Product => ({
    ...defaultProduct,
    ...overrides,
  }),
} as const;
