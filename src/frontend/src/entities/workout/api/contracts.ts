export interface GetWorkoutSetsRequest {
  from: string;
  to: string;
}

export interface GetWorkoutSetsResponse {
  workoutSets: WorkoutSetItem[];
}

export interface WorkoutSetItem {
  id: number;
  date: string;
  exercise: string;
  reps: number;
  /** 0 means no added weight (bodyweight) */
  weightKg: number;
}

export interface GetExercisesResponse {
  exercises: string[];
}

export interface LogWorkoutSetsRequest {
  date: string;
  exercise: string;
  sets: LogWorkoutSetsRequestSet[];
}

export interface LogWorkoutSetsRequestSet {
  reps: number;
  weightKg: number;
}

export interface LogWorkoutSetsResponse {
  ids: number[];
}

export interface UpdateWorkoutSetRequest {
  date: string;
  exercise: string;
  reps: number;
  weightKg: number;
}

export interface UpdateWorkoutSetArgs {
  id: number;
  request: UpdateWorkoutSetRequest;
}
