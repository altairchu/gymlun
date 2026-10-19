import EditIcon from '@mui/icons-material/Edit';
import {
  Box,
  Card,
  CardHeader,
  List,
  ListItem,
  ListItemButton,
  Stack,
  Typography,
} from '@mui/material';
import { type FC, useMemo, useState } from 'react';
import { type WorkoutSetItem, workoutApi, workoutLib } from '@/entities/workout';
import { EditWorkoutSetDialog, LogWorkoutButton } from '@/features/logWorkout';

interface Props {
  date: string;
}

export const WorkoutDayList: FC<Props> = ({ date }) => {
  const [editedWorkoutSet, setEditedWorkoutSet] = useState<WorkoutSetItem | null>(null);

  const { data, isSuccess } = workoutApi.useWorkoutSetsQuery({ from: date, to: date });

  const exercises = useMemo(() => workoutLib.groupSetsByExercise(data?.workoutSets ?? []), [data]);

  return (
    <>
      <Stack spacing={2}>
        <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
          <LogWorkoutButton date={date} />
        </Box>
        {isSuccess && exercises.length === 0 && (
          <Typography color="textSecondary">呢日未有訓練記錄</Typography>
        )}
        {exercises.map(({ exercise, sets }) => (
          <Card key={exercise} component="section" aria-label={exercise}>
            <CardHeader
              title={exercise}
              slotProps={{ title: { variant: 'h6', component: 'h2' } }}
              sx={{ pb: 0 }}
            />
            <List>
              {sets.map((workoutSet, index) => {
                const setNumber = workoutLib.formatSetNumber(index);
                const reps = workoutLib.formatReps(workoutSet.reps);
                const weight = workoutLib.formatWeight(workoutSet.weightKg);
                const description = `${setNumber}：${reps}，${weight}`;

                return (
                  <ListItem key={workoutSet.id} disablePadding aria-label={description}>
                    <ListItemButton
                      aria-label={`修改${description}`}
                      onClick={() => setEditedWorkoutSet(workoutSet)}
                      sx={{ gap: 2 }}
                    >
                      <Typography color="textSecondary" sx={{ minWidth: 56 }}>
                        {setNumber}
                      </Typography>
                      <Typography sx={{ minWidth: 56 }}>{reps}</Typography>
                      <Typography sx={{ flex: 1 }}>{weight}</Typography>
                      <EditIcon fontSize="small" color="action" />
                    </ListItemButton>
                  </ListItem>
                );
              })}
            </List>
          </Card>
        ))}
      </Stack>
      {editedWorkoutSet && (
        <EditWorkoutSetDialog
          workoutSet={editedWorkoutSet}
          onClose={() => setEditedWorkoutSet(null)}
        />
      )}
    </>
  );
};
