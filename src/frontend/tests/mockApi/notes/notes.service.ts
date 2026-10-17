import {
  type CreateNotesRequest,
  type NoteRequestBody,
  type UpdateNoteRequest,
  noteModel,
} from '@/entities/note';
import { db, type DbNote, type DbProduct } from '../db';

export const getByDate = (date: string): DbNote[] =>
  noteModel.sortByTime(
    db.note.findMany({
      where: {
        date: { equals: date },
      },
    }),
  );

const MEAL_TIMES: Record<noteModel.MealType, string> = {
  [noteModel.MealType.Breakfast]: '08:00:00',
  [noteModel.MealType.SecondBreakfast]: '10:30:00',
  [noteModel.MealType.Lunch]: '12:30:00',
  [noteModel.MealType.AfternoonSnack]: '15:30:00',
  [noteModel.MealType.Dinner]: '19:00:00',
};

const toMealType = (time: string): noteModel.MealType => {
  const minutes = noteModel.parseTime(time);

  if (minutes >= 4 * 60 && minutes < 10 * 60) {
    return noteModel.MealType.Breakfast;
  }

  if (minutes >= 10 * 60 && minutes < 12 * 60) {
    return noteModel.MealType.SecondBreakfast;
  }

  if (minutes >= 12 * 60 && minutes < 15 * 60) {
    return noteModel.MealType.Lunch;
  }

  if (minutes >= 15 * 60 && minutes < 18 * 60) {
    return noteModel.MealType.AfternoonSnack;
  }

  return noteModel.MealType.Dinner;
};

type TimedNote = Pick<DbNote, 'time' | 'mealType'>;

const toTimedNote = ({ time, mealType }: NoteRequestBody): TimedNote | null => {
  if (time !== undefined) {
    return { time, mealType: mealType ?? toMealType(time) };
  }

  return mealType !== undefined ? { time: MEAL_TIMES[mealType], mealType } : null;
};

const findProduct = (id: number): DbProduct | null =>
  db.product.findFirst({
    where: {
      id: { equals: id },
    },
  });

const nextNoteId = (): number =>
  (db.note
    .findMany({
      orderBy: { id: 'desc' },
      take: 1,
    })
    .at(0)?.id ?? 0) + 1;

export const getHistory = (from: string, to: string): Map<string, DbNote[]> =>
  db.note
    .findMany({
      where: {
        date: {
          gte: from,
          lte: to,
        },
      },
      orderBy: {
        date: 'asc',
      },
    })
    .reduce((groups, note) => {
      const group = groups.get(note.date);
      if (group) {
        group.push(note);
      } else {
        groups.set(note.date, [note]);
      }
      return groups;
    }, new Map<string, DbNote[]>());

export const getProducts = (notes: DbNote[]): Map<number, DbProduct> => {
  return notes
    .map(n => n.productId)
    .reduce((map, id) => {
      if (map.has(id)) {
        return map;
      }

      const product = db.product.findFirst({
        where: {
          id: { equals: id },
        },
      });

      if (product) {
        map.set(id, product);
      }

      return map;
    }, new Map<number, DbProduct>());
};

export const calculateCalories = (quantity: number, caloriesCost: number): number =>
  Math.floor((quantity * caloriesCost) / 100);

type Result = 'Success' | 'ProductNotFound' | 'TimeMissing';

export const create = (note: NoteRequestBody): Result => {
  const timedNote = toTimedNote(note);

  if (!timedNote) {
    return 'TimeMissing';
  }

  if (!findProduct(note.productId)) {
    return 'ProductNotFound';
  }

  db.note.create({
    id: nextNoteId(),
    date: note.date,
    ...timedNote,
    quantity: note.productQuantity,
    displayOrder: note.displayOrder ?? 0,
    productId: note.productId,
  });

  return 'Success';
};

export const createMany = ({ date, time, items }: CreateNotesRequest): number[] | null => {
  if (items.some(item => !findProduct(item.productId))) {
    return null;
  }

  return items.map((item, index) => {
    const id = nextNoteId();

    db.note.create({
      id,
      date,
      time,
      mealType: toMealType(time),
      quantity: item.productQuantity,
      displayOrder: index,
      productId: item.productId,
    });

    return id;
  });
};

export const update = ({ id, note }: UpdateNoteRequest): Result => {
  const timedNote = toTimedNote(note);

  if (!timedNote) {
    return 'TimeMissing';
  }

  if (!findProduct(note.productId)) {
    return 'ProductNotFound';
  }

  db.note.update({
    where: {
      id: { equals: id },
    },
    data: {
      date: note.date,
      ...timedNote,
      quantity: note.productQuantity,
      displayOrder: note.displayOrder ?? 0,
      productId: note.productId,
    },
  });

  return 'Success';
};

export const deleteOne = (id: number): void => {
  db.note.delete({
    where: {
      id: { equals: id },
    },
  });
};
