import { type CategoryFormData } from '@/features/categories';
import { type DbCategory, db } from '../db';

export const getAll = (): DbCategory[] =>
  db.category.findMany({
    orderBy: { name: 'asc' },
  });

export const getProductsCount = (categoryId: number): number =>
  db.product.getAll().filter(product => product.categoryIds.includes(categoryId)).length;

export const create = ({ name }: CategoryFormData): number => {
  const maxId =
    db.category
      .findMany({
        orderBy: {
          id: 'desc',
        },
        take: 1,
      })
      ?.at(0)?.id ?? 0;

  const id = maxId + 1;

  db.category.create({
    id,
    name,
  });

  return id;
};

export const update = (id: number, body: CategoryFormData): void => {
  db.category.update({
    where: {
      id: { equals: id },
    },
    data: body,
  });
};

export const deleteOne = (id: number): void => {
  db.product
    .getAll()
    .filter(product => product.categoryIds.includes(id))
    .forEach(product => {
      db.product.update({
        where: {
          id: { equals: product.id },
        },
        data: {
          categoryIds: product.categoryIds.filter(categoryId => categoryId !== id),
        },
      });
    });

  db.category.delete({
    where: {
      id: { equals: id },
    },
  });
};
