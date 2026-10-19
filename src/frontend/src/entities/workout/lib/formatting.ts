export const formatSetNumber = (index: number): string => `第 ${index + 1} 組`;

export const formatReps = (reps: number): string => `${reps} 次`;

export const formatWeight = (weightKg: number): string =>
  weightKg === 0 ? '自身重量' : `${weightKg} kg`;
