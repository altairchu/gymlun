import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Box, Stack } from '@mui/material';
import { useState, type FC } from 'react';
import { Controller, type SubmitHandler, useForm } from 'react-hook-form';
import { type WorkoutSetItem, workoutApi } from '@/entities/workout';
import { Button, Dialog } from '@/shared/ui';
import { type EditFormValues, editSchema } from '../model';
import { ExerciseInput } from './ExerciseInput';
import { RepsInput, WeightKgInput } from './SetInputs';

const formId = 'edit-workout-set-form';

interface Props {
  workoutSet: WorkoutSetItem;
  onClose: () => void;
}

export const EditWorkoutSetDialog: FC<Props> = ({ workoutSet, onClose }) => {
  const [confirmingDelete, setConfirmingDelete] = useState(false);
  const [updateWorkoutSet, updateResult] = workoutApi.useUpdateWorkoutSetMutation();
  const [deleteWorkoutSet, deleteResult] = workoutApi.useDeleteWorkoutSetMutation();

  const { exercises, exercisesLoading } = workoutApi.useExercisesQuery(undefined, {
    selectFromResult: ({ data, isLoading }) => ({
      exercises: data?.exercises ?? [],
      exercisesLoading: isLoading,
    }),
  });

  const { control, handleSubmit, formState } = useForm({
    mode: 'onChange',
    resolver: zodResolver(editSchema),
    defaultValues: {
      exercise: workoutSet.exercise,
      reps: workoutSet.reps,
      weightKg: workoutSet.weightKg,
    },
  });

  const busy = updateResult.isLoading || deleteResult.isLoading;

  const onSubmit: SubmitHandler<EditFormValues> = async values => {
    const { error } = await updateWorkoutSet({
      id: workoutSet.id,
      request: { date: workoutSet.date, ...values },
    });

    if (!error) {
      onClose();
    }
  };

  const handleDelete = async (): Promise<void> => {
    if (!confirmingDelete) {
      setConfirmingDelete(true);
      return;
    }

    const { error } = await deleteWorkoutSet(workoutSet.id);

    if (!error) {
      onClose();
    }
  };

  return (
    <Dialog
      opened
      title="改呢一組"
      onClose={onClose}
      content={
        <Stack spacing={2} sx={{ pt: 1 }}>
          <form id={formId} onSubmit={handleSubmit(onSubmit)}>
            <Stack spacing={1}>
              <Controller
                name="exercise"
                control={control}
                render={({ field, fieldState }) => (
                  <ExerciseInput
                    value={field.value}
                    options={exercises}
                    loading={exercisesLoading}
                    error={fieldState.error?.message}
                    onChange={field.onChange}
                    onBlur={field.onBlur}
                  />
                )}
              />
              <Box sx={{ display: 'flex', gap: 1 }}>
                <Controller
                  name="reps"
                  control={control}
                  render={({ field, fieldState }) => (
                    <RepsInput
                      value={field.value}
                      error={fieldState.error?.message}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                    />
                  )}
                />
                <Controller
                  name="weightKg"
                  control={control}
                  render={({ field, fieldState }) => (
                    <WeightKgInput
                      value={field.value}
                      error={fieldState.error?.message}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                    />
                  )}
                />
              </Box>
            </Stack>
          </form>
          {(updateResult.isError || deleteResult.isError) && (
            <Alert severity="error">儲存唔到，請再試一次</Alert>
          )}
          <Button
            color="error"
            variant={confirmingDelete ? 'contained' : 'outlined'}
            loading={deleteResult.isLoading}
            disabled={updateResult.isLoading}
            onClick={handleDelete}
            sx={{ alignSelf: 'flex-start' }}
          >
            {confirmingDelete ? '確定刪除？' : '刪除呢一組'}
          </Button>
        </Stack>
      }
      renderCancel={props => (
        <Button {...props} onClick={onClose} disabled={busy}>
          取消
        </Button>
      )}
      renderSubmit={props => (
        <Button
          {...props}
          type="submit"
          form={formId}
          disabled={!formState.isValid || deleteResult.isLoading}
          loading={updateResult.isLoading}
        >
          儲存
        </Button>
      )}
    />
  );
};
