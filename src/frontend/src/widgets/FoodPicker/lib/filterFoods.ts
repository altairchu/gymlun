import { type ProductSelectOption } from '@/entities/product';

export interface FoodFilter {
  categoryIds: number[];
  favoritesOnly: boolean;
  query: string;
}

export const EMPTY_FOOD_FILTER: FoodFilter = {
  categoryIds: [],
  favoritesOnly: false,
  query: '',
};

const matches = (product: ProductSelectOption, { categoryIds, favoritesOnly, query }: FoodFilter) =>
  categoryIds.every(id => product.categoryIds.includes(id)) &&
  (!favoritesOnly || product.isFavorite) &&
  product.name.toLowerCase().includes(query.trim().toLowerCase());

export const filterFoods = (
  products: ProductSelectOption[],
  filter: FoodFilter,
): ProductSelectOption[] => products.filter(product => matches(product, filter));

export const isFilterActive = ({ categoryIds, favoritesOnly }: FoodFilter): boolean =>
  categoryIds.length > 0 || favoritesOnly;

export const toggleCategory = (filter: FoodFilter, categoryId: number): FoodFilter => ({
  ...filter,
  categoryIds: filter.categoryIds.includes(categoryId)
    ? filter.categoryIds.filter(id => id !== categoryId)
    : [...filter.categoryIds, categoryId],
});
