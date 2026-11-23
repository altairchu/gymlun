import { http, HttpResponse } from 'msw';
import { type GymContext } from '@/entities/gym/gymApi';
import { API_URL } from '@/shared/config';
import { authHandlers } from './auth';
import { categoriesHandlers } from './categories';
import { notesHandlers } from './notes';
import { productsHandlers } from './products';
import { weightLogsHandlers } from './weightLogs';
import { workoutsHandlers } from './workouts';

const emptyGymContext: GymContext = {
  version: 0,
  goal: null,
  eatingApproach: null,
  targetWeightKg: null,
  targetKcal: null,
  proteinPerKg: null,
  targetCarbsG: null,
  targetFatG: null,
  ageYears: null,
  heightCm: null,
  sex: null,
  activityFactor: null,
  latestWeight: null,
  dailyNeeds: {
    restingKcal: null,
    maintenanceKcal: null,
    targetProteinG: null,
    missing: ['weight', 'ageYears', 'heightCm', 'sex', 'activityFactor'],
    method: null,
  },
};

export const handlers = [
  http.get(`${API_URL}/api/v1/gym-context`, () => HttpResponse.json(emptyGymContext)),
  ...authHandlers,
  ...notesHandlers,
  ...productsHandlers,
  ...categoriesHandlers,
  ...weightLogsHandlers,
  ...workoutsHandlers,
];
