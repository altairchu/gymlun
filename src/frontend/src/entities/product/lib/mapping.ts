import { type Product, type GetProductsRequest } from '../api';
import { type ProductFormValues, type ProductItemsFilter } from '../model';

export const mapToGetProductsRequest = ({
  pageNumber,
  pageSize,
  productSearchName,
  category,
}: ProductItemsFilter): GetProductsRequest => ({
  pageNumber,
  pageSize,
  productSearchName,
  categoryId: category?.id,
});

export const mapToProductFormData = ({
  name,
  caloriesCost,
  defaultQuantity,
  quantityUnit,
  categories,
  protein,
  fats,
  carbs,
  sugar,
  salt,
  isFavorite,
}: Product): ProductFormValues => ({
  name,
  calories: caloriesCost,
  defaultQuantity,
  quantityUnit,
  categories: categories.map(({ id, name }) => ({ id, name })),
  protein,
  fats,
  carbs,
  sugar,
  salt,
  isFavorite,
});
