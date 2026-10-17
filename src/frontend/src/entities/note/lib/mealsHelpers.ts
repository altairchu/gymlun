import { z } from 'zod';
import { MealType } from '../model';

const MEAL_NAMES: Record<MealType, string> = {
  [MealType.Breakfast]: '早餐',
  [MealType.SecondBreakfast]: '上午小食',
  [MealType.Lunch]: '午餐',
  [MealType.AfternoonSnack]: '下午茶',
  [MealType.Dinner]: '晚餐',
};

const mealTypeSchema = z.enum(MealType);

export const getMealTypes = (): MealType[] =>
  Object.keys(MEAL_NAMES).map(key => mealTypeSchema.parse(Number(key)));

export const getMealName = (mealType: MealType): string => MEAL_NAMES[mealType];
