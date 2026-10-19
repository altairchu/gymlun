import { zodResolver } from '@hookform/resolvers/zod';
import AddIcon from '@mui/icons-material/Add';
import RemoveCircleOutlineIcon from '@mui/icons-material/RemoveCircleOutlineOutlined';
import { Alert, Box, IconButton, Stack, Typography } from '@mui/material';
import { type FC } from 'react';
import { Controller, type SubmitHandler, useFieldArray, useForm } from 'react-hook-form';
import { workoutApi, workoutLib } from '@/entities/workout';
import { dateLib } from '@/shared/lib';
import { Button, Dialog } from '@/shared/ui';
import { type LogFormInput, type LogFormValues, logSchema, MAX_SETS } from '../model';
import { ExerciseInput } from './ExerciseInput';
import { RepsInput, WeightKgInput } from './SetInputs';

const formId = 'log-workout-form';

const defaultValues: LogFormInput = {
  exercise: '',
  sets: [{ reps: '', weightKg: '' }],
};

interface Props {
  date: string;
  onClose: () => void;
}

export const LogWorkoutDialog: FC<Props> = ({ date, onClose }) => {
  const [logWorkoutSets, logResult] = workoutApi.useLogWorkoutSetsMutation();

  const { exercises, exercisesLoading } = workoutApi.useExercisesQuery(undefined, {
    selectFromResult: ({ data, isLoading }) => ({
      exercises: data?.exercises ?? [],
      exercisesLoading: isLoading,
    }),
  });

  const { control, handleSubmit, formState, getValues } = useForm({
    mode: 'onChange',
    resolver: zodResolver(logSchema),
    defaultValues,
  });

  const { fields, append, remove } = useFieldArray({ control, name: 'sets' });

  const onSubmit: SubmitHandler<LogFormValues> = async ({ exercise, sets }) => {
    const { error } = await logWorkoutSets({ date, exercise, sets });

    if (!error) {
      onClose();
    }
  };

  const addSet = (): void => {
    append({ ...getValues(`sets.${fields.length - 1}`) });
  };

  return (
    <Dialog
      opened
      renderMode="fullScreenOnMobile"
      title="記錄訓練"
      onClose={onClose}
      content={
        <form id={formId} onSubmit={handleSubmit(onSubmit)}>
          <Stack spacing={1} sx={{ pt: 1 }}>
            <Typography color="textSecondary">
              {dateLib.formatToUserFriendlyString(date)}
            </Typography>
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
            {fields.map((setField, index) => {
              const setNumber = workoutLib.formatSetNumber(index);

              return (
                <Box
                  key={setField.id}
                  role="group"
                  aria-label={setNumber}
                  sx={{ display: 'flex', alignItems: 'flex-start', gap: 1 }}
                >
                  <Typography sx={{ flexShrink: 0, pt: 2, minWidth: 48 }}>{setNumber}</Typography>
                  <Controller
                    name={`sets.${index}.reps`}
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
                    name={`sets.${index}.weightKg`}
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
                  <IconButton
                    aria-label={`移除${setNumber}`}
                    disabled={fields.length === 1}
                    onClick={() => remove(index)}
                    sx={{ mt: 1 }}
                  >
                    <RemoveCircleOutlineIcon />
                  </IconButton>
                </Box>
              );
            })}
            <Button
              startIcon={<AddIcon />}
              disabled={fields.length >= MAX_SETS}
              onClick={addSet}
              sx={{ alignSelf: 'flex-start' }}
            >
              加一組
            </Button>
            {logResult.isError && <Alert severity="error">儲存唔到，請再試一次</Alert>}
          </Stack>
        </form>
      }
      renderCancel={props => (
        <Button {...props} onClick={onClose} disabled={logResult.isLoading}>
          取消
        </Button>
      )}
      renderSubmit={props => (
        <Button
          {...props}
          type="submit"
          form={formId}
          disabled={!formState.isValid}
          loading={logResult.isLoading}
        >
          儲存
        </Button>
      )}
    />
  );
};
