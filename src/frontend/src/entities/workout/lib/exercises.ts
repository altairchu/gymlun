import { type WorkoutSetItem } from '../api';

export interface WorkoutExercise {
  exercise: string;
  sets: WorkoutSetItem[];
}

export const groupSetsByExercise = (workoutSets: WorkoutSetItem[]): WorkoutExercise[] => {
  const groups = new Map<string, WorkoutSetItem[]>();

  [...workoutSets]
    .sort((a, b) => a.id - b.id)
    .forEach(workoutSet => {
      const group = groups.get(workoutSet.exercise);

      if (group) {
        group.push(workoutSet);
      } else {
        groups.set(workoutSet.exercise, [workoutSet]);
      }
    });

  return Array.from(groups, ([exercise, sets]) => ({ exercise, sets }));
};
