import {
  type Category,
  type CreateProductRequest,
  type EditProductRequest,
} from '@/entities/product';
import { db, type DbUpdatableProduct, type DbProduct } from '../db';

interface GetProductsRequest {
  pageNumber: number;
  pageSize: number;
  categoryId: number | null;
  productSearchName: string | null;
}

export const get = ({
  pageNumber,
  pageSize,
  categoryId,
  productSearchName,
}: GetProductsRequest): DbProduct[] => {
  let productItems = db.product.findMany({
    orderBy: { name: 'asc' },
  });

  if (categoryId !== null) {
    productItems = productItems.filter(p => p.categoryIds.includes(categoryId));
  }

  if (productSearchName) {
    productItems = productItems.filter(p => p.name.toLowerCase().startsWith(productSearchName));
  }

  return productItems.slice((pageNumber - 1) * pageSize, pageNumber * pageSize);
};

export const getById = (id: number): DbProduct | null =>
  db.product.findFirst({
    where: {
      id: { equals: id },
    },
  });

export const getAll = (): DbProduct[] => db.product.getAll();

export const getCategories = (product: DbProduct): Category[] =>
  db.category
    .findMany({
      where: {
        id: { in: product.categoryIds },
      },
      orderBy: { name: 'asc' },
    })
    .map(({ id, name }) => ({ id, name }));

const allCategoriesExist = (categoryIds: number[]): boolean =>
  db.category.count({
    where: {
      id: { in: categoryIds },
    },
  }) === new Set(categoryIds).size;

export const count = (): number => db.product.count();

interface CreateProductResultSuccess {
  type: 'Success';
  id: number;
}
interface CreateProductResultError {
  type: 'CategoryNotFound';
}

type CreateProductResult = CreateProductResultSuccess | CreateProductResultError;

export const create = ({
  name,
  caloriesCost,
  defaultQuantity,
  quantityUnit,
  categoryIds,
  protein,
  fats,
  carbs,
  sugar,
  salt,
  isFavorite,
}: CreateProductRequest): CreateProductResult => {
  if (!allCategoriesExist(categoryIds)) {
    return { type: 'CategoryNotFound' };
  }

  const maxId =
    db.product
      .findMany({
        orderBy: {
          id: 'desc',
        },
        take: 1,
      })
      ?.at(0)?.id ?? 0;

  const id = maxId + 1;

  db.product.create({
    id,
    name,
    caloriesCost,
    defaultQuantity,
    quantityUnit,
    categoryIds,
    protein,
    fats,
    carbs,
    sugar,
    salt,
    isFavorite,
  } satisfies DbProduct);

  return { type: 'Success', id };
};

type UpdateProductResult = 'Success' | 'CategoryNotFound';

export const update = (
  id: number,
  {
    name,
    caloriesCost,
    defaultQuantity,
    quantityUnit,
    categoryIds,
    protein,
    fats,
    carbs,
    sugar,
    salt,
    isFavorite,
  }: EditProductRequest,
): UpdateProductResult => {
  if (!allCategoriesExist(categoryIds)) {
    return 'CategoryNotFound';
  }

  db.product.update({
    where: {
      id: { equals: id },
    },
    data: {
      name,
      caloriesCost,
      defaultQuantity,
      quantityUnit,
      categoryIds,
      protein,
      fats,
      carbs,
      sugar,
      salt,
      isFavorite,
    } satisfies DbUpdatableProduct,
  });

  return 'Success';
};

export const deleteMany = (ids: number[]): void => {
  db.note.deleteMany({
    where: {
      productId: { in: ids },
    },
  });

  db.product.deleteMany({
    where: {
      id: { in: ids },
    },
  });
};
