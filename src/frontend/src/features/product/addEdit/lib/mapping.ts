import {
  type EditProductRequest,
  type CreateProductRequest,
  type productModel,
} from '@/entities/product';

export const mapToCreateProductRequest = ({
  name,
  calories: caloriesCost,
  defaultQuantity,
  quantityUnit,
  categories,
  protein,
  fats,
  carbs,
  sugar,
  salt,
  isFavorite,
}: productModel.ProductFormValues): CreateProductRequest => ({
  name,
  caloriesCost,
  defaultQuantity,
  quantityUnit,
  categoryIds: categories.map(category => category.id),
  protein,
  fats,
  carbs,
  sugar,
  salt,
  isFavorite,
});

export const mapToEditProductRequest = (
  id: number,
  product: productModel.ProductFormValues,
): EditProductRequest => ({
  id,
  ...mapToCreateProductRequest(product),
});
