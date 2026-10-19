import { type NoteItem, noteModel } from '@/entities/note';
import { productModel } from '@/entities/product';
import {
  GROUP_GAP,
  GROUP_HEADER_HEIGHT,
  GROUP_PADDING,
  ROW_HEIGHT,
  groupByTime,
  placeGroups,
  toPx,
} from './layout';

const note = (id: number, time: string): NoteItem => ({
  id,
  date: '2025-06-10',
  time,
  mealType: noteModel.MealType.Breakfast,
  displayOrder: 0,
  productQuantity: 100,
  product: {
    id: 1,
    name: 'Oats',
    defaultQuantity: 100,
    quantityUnit: productModel.QuantityUnit.Grams,
    calories: 100,
    protein: null,
    fats: null,
    carbs: null,
    sugar: null,
    salt: null,
    photoUrl: null,
  },
});

test('foods eaten at the same time form one group, in time order', () => {
  const groups = groupByTime([note(1, '12:30:00'), note(2, '08:00:00'), note(3, '12:30:00')]);

  expect(groups.map(group => [group.minutes, group.notes.map(n => n.id)])).toStrictEqual([
    [480, [2]],
    [750, [1, 3]],
  ]);
});

test('a group sits at its time of day', () => {
  const [breakfast] = placeGroups(groupByTime([note(1, '08:00:00')]));

  expect(breakfast?.top).toBe(toPx(480));
});

test('a group that an earlier one still covers moves just below it', () => {
  const [first, second] = placeGroups(
    groupByTime([note(1, '08:00:00'), note(2, '08:00:00'), note(3, '08:10:00')]),
  );

  const firstHeight = GROUP_HEADER_HEIGHT + 2 * ROW_HEIGHT + GROUP_PADDING;
  expect(first?.height).toBe(firstHeight);
  expect(second?.top).toBe(toPx(480) + firstHeight + GROUP_GAP);
});

test('a group far enough from the previous one keeps its own time', () => {
  const [, dinner] = placeGroups(groupByTime([note(1, '08:00:00'), note(2, '19:00:00')]));

  expect(dinner?.top).toBe(toPx(19 * 60));
});
