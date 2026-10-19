import { db } from '../db';
import data from './workouts.data.json';

export { handlers as workoutsHandlers } from './workouts.handlers';

export const fillWorkoutSets = (): void => {
  data.forEach(workoutSet => {
    db.workoutSet.create(workoutSet);
  });
};
