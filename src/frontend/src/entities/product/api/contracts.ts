import { type QuantityUnit } from '../model/quantityUnit';

export interface Product {
  id: number;
  name: string;
  caloriesCost: number;
  defaultQuantity: number;
  quantityUnit: QuantityUnit;
  categories: Category[];
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
  isFavorite: boolean;
  /** Relative to `API_URL` */
  photoUrl: string | null;
}

export interface Category {
  id: number;
  name: string;
}

export interface GetProductsResponse {
  productItems: Product[];
  totalProductsCount: number;
}

export interface GetProductsRequest {
  pageNumber: number;
  pageSize: number;
  productSearchName?: string;
  categoryId?: number;
}

export interface GetProductByIdResponse {
  id: number;
  name: string;
  caloriesCost: number;
  defaultQuantity: number;
  quantityUnit: QuantityUnit;
  categories: Category[];
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
  isFavorite: boolean;
  photoUrl: string | null;
}

export interface ProductSelectOption {
  id: number;
  name: string;
  defaultQuantity: number;
  quantityUnit: QuantityUnit;
  categoryIds: number[];
  calories: number;
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
  isFavorite: boolean;
  photoUrl: string | null;
}

export interface CreateProductRequest {
  name: string;
  caloriesCost: number;
  defaultQuantity: number;
  quantityUnit: QuantityUnit;
  categoryIds: number[];
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
  isFavorite: boolean;
}

export interface CreateProductResponse {
  id: number;
}

export interface EditProductRequest {
  id: number;
  name: string;
  caloriesCost: number;
  defaultQuantity: number;
  quantityUnit: QuantityUnit;
  categoryIds: number[];
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
  isFavorite: boolean;
}

export interface SetProductPhotoRequest {
  productId: number;
  photo: Blob;
}

export interface SetProductPhotoResponse {
  photoUrl: string;
}

export interface DeleteProductsRequest {
  ids: number[];
}

export interface SuggestProductNutritionRequest {
  name: string;
}

export interface SuggestProductNutritionResponse {
  calories: number | null;
  protein: number | null;
  fats: number | null;
  carbs: number | null;
  sugar: number | null;
  salt: number | null;
}
