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

interface StoredPhoto {
  content: Blob;
  version: number;
}

const photos = new Map<number, StoredPhoto>();
let lastPhotoVersion = 0;

export const photoUrlOf = (productId: number): string | null => {
  const photo = photos.get(productId);
  return photo ? `/api/v1/products/${productId}/photo?v=${photo.version}` : null;
};

export const getPhoto = (productId: number): Blob | null => photos.get(productId)?.content ?? null;

export const setPhoto = (productId: number, content: Blob): void => {
  lastPhotoVersion += 1;
  photos.set(productId, { content, version: lastPhotoVersion });
};

export const deletePhoto = (productId: number): void => {
  photos.delete(productId);
};

export const clearPhotos = (): void => {
  photos.clear();
};

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

  ids.forEach(deletePhoto);
};
