import { z } from 'zod';
import { exerciseSchema, MAX_SETS, repsSchema, weightKgSchema } from './fieldSchemas';

export { MAX_SETS };

export const logSchema = z.object({
  exercise: exerciseSchema,
  sets: z
    .array(z.object({ reps: repsSchema, weightKg: weightKgSchema }))
    .min(1)
    .max(MAX_SETS),
});

export type LogFormInput = z.input<typeof logSchema>;

export type LogFormValues = z.infer<typeof logSchema>;

export const editSchema = z.object({
  exercise: exerciseSchema,
  reps: repsSchema,
  weightKg: weightKgSchema,
});

export type EditFormValues = z.infer<typeof editSchema>;
