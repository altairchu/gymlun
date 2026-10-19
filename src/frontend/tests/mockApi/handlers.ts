import { authHandlers } from './auth';
import { categoriesHandlers } from './categories';
import { notesHandlers } from './notes';
import { productsHandlers } from './products';
import { weightLogsHandlers } from './weightLogs';
import { workoutsHandlers } from './workouts';

export const handlers = [
  ...authHandlers,
  ...notesHandlers,
  ...productsHandlers,
  ...categoriesHandlers,
  ...weightLogsHandlers,
  ...workoutsHandlers,
];
