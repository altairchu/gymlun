import { differenceInDays } from 'date-fns';
import {
  type LogWorkoutSetsRequest,
  type LogWorkoutSetsRequestSet,
  type UpdateWorkoutSetRequest,
} from '@/entities/workout';
import { db, type DbWorkoutSet } from '../db';

const MAX_RANGE_DAYS = 366;
const MAX_SETS = 30;
const MAX_EXERCISES = 200;

const byDateThenId = (a: DbWorkoutSet, b: DbWorkoutSet): number =>
  a.date === b.date ? a.id - b.id : a.date.localeCompare(b.date);

export const isValidRange = (from: string, to: string): boolean => {
  const days = differenceInDays(new Date(to), new Date(from));
  return days >= 0 && days <= MAX_RANGE_DAYS;
};

export const getInRange = (from: string, to: string): DbWorkoutSet[] =>
  db.workoutSet
    .getAll()
    .filter(({ date }) => date >= from && date <= to)
    .sort(byDateThenId);

export const getExercises = (): string[] => {
  const latestFirst = db.workoutSet.getAll().sort((a, b) => byDateThenId(b, a));
  return Array.from(new Set(latestFirst.map(({ exercise }) => exercise))).slice(0, MAX_EXERCISES);
};

const isValidExercise = (exercise: string): boolean => {
  const { length } = exercise.trim();
  return length >= 1 && length <= 100;
};

const isValidSet = ({ reps, weightKg }: LogWorkoutSetsRequestSet): boolean =>
  Number.isInteger(reps) && reps >= 1 && reps <= 1000 && weightKg >= 0 && weightKg <= 1000;

const toStoredExercise = (exercise: string, excludedId?: number): string => {
  const trimmed = exercise.trim();
  const existing = db.workoutSet
    .getAll()
    .find(
      workoutSet =>
        workoutSet.id !== excludedId && workoutSet.exercise.toLowerCase() === trimmed.toLowerCase(),
    );

  return existing?.exercise ?? trimmed;
};

const roundWeight = (weightKg: number): number => Math.round(weightKg * 100) / 100;

const nextId = (): number => Math.max(0, ...db.workoutSet.getAll().map(({ id }) => id)) + 1;

const find = (id: number): DbWorkoutSet | null =>
  db.workoutSet.findFirst({ where: { id: { equals: id } } });

export const log = ({ date, exercise, sets }: LogWorkoutSetsRequest): number[] | null => {
  if (!isValidExercise(exercise) || sets.length < 1 || sets.length > MAX_SETS) {
    return null;
  }

  if (!sets.every(isValidSet)) {
    return null;
  }

  const storedExercise = toStoredExercise(exercise);

  return sets.map(({ reps, weightKg }) => {
    const id = nextId();
    db.workoutSet.create({
      id,
      date,
      exercise: storedExercise,
      reps,
      weightKg: roundWeight(weightKg),
    });
    return id;
  });
};

type UpdateResult = 'Success' | 'NotFound' | 'Invalid';

export const update = (id: number, request: UpdateWorkoutSetRequest): UpdateResult => {
  if (!find(id)) {
    return 'NotFound';
  }

  if (!isValidExercise(request.exercise) || !isValidSet(request)) {
    return 'Invalid';
  }

  db.workoutSet.update({
    where: { id: { equals: id } },
    data: {
      date: request.date,
      exercise: toStoredExercise(request.exercise, id),
      reps: request.reps,
      weightKg: roundWeight(request.weightKg),
    },
  });

  return 'Success';
};

export const deleteOne = (id: number): boolean =>
  db.workoutSet.delete({ where: { id: { equals: id } } }) !== null;
