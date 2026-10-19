import { type WorkoutSetItem } from '../api';
import { groupSetsByExercise, type WorkoutExercise } from './exercises';

const set = (id: number, exercise: string): WorkoutSetItem => ({
  id,
  date: '2023-10-19',
  exercise,
  reps: 10,
  weightKg: 60,
});

test('no sets make no exercises', () => {
  expect(groupSetsByExercise([])).toStrictEqual<WorkoutExercise[]>([]);
});

test('sets are grouped by exercise in the order the exercises were first done', () => {
  const exercises = groupSetsByExercise([
    set(5, 'Squat'),
    set(2, 'Bench Press'),
    set(4, 'Bench Press'),
    set(3, 'Squat'),
  ]);

  expect(exercises).toStrictEqual<WorkoutExercise[]>([
    { exercise: 'Bench Press', sets: [set(2, 'Bench Press'), set(4, 'Bench Press')] },
    { exercise: 'Squat', sets: [set(3, 'Squat'), set(5, 'Squat')] },
  ]);
});
