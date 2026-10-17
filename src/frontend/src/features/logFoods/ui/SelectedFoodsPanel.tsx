import { Alert, Box, Divider, Stack, Typography } from '@mui/material';
import { useState, type FC } from 'react';
import { TimeOfDayPicker, noteApi, noteLib, noteModel } from '@/entities/note';
import { Button } from '@/shared/ui';
import { type FoodSelection } from '../lib';
import { caloriesOf, gramsOf } from '../model';
import { SelectedFoodItem } from './SelectedFoodItem';

interface Props {
  date: string;
  selection: FoodSelection;
  onLogged: (count: number, minutes: number) => void;
}

export const SelectedFoodsPanel: FC<Props> = ({ date, selection, onLogged }) => {
  const now = noteLib.useNow();
  const [minutes, setMinutes] = useState(() => noteModel.findQuickTimeAt(now.minutes).minutes);
  const [createNotes, { isLoading, isError, reset }] = noteApi.useCreateNotesMutation();
  const { items } = selection;

  const quantities = items.map(gramsOf);
  const valid = items.length > 0 && quantities.every(grams => grams !== null);
  const totalCalories = items.reduce((sum, item) => sum + caloriesOf(item), 0);

  const handleSubmit = async (): Promise<void> => {
    const requestItems = items.flatMap(item => {
      const grams = gramsOf(item);
      return grams === null ? [] : [{ productId: item.product.id, productQuantity: grams }];
    });

    const result = await createNotes({
      date,
      time: noteModel.toApiTime(minutes),
      items: requestItems,
    });

    if (!result.error) {
      selection.clear();
      reset();
      setMinutes(noteModel.findQuickTimeAt(now.minutes).minutes);
      onLogged(requestItems.length, minutes);
    }
  };

  return (
    <Stack spacing={2} component="section" aria-labelledby="selected-foods-title">
      <Stack direction="row" sx={{ alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography id="selected-foods-title" variant="h6" component="h2">
          {items.length > 0 ? `已揀 ${items.length} 樣 · ${totalCalories} kcal` : '已揀食物'}
        </Typography>
        {items.length > 0 && (
          <Button size="small" color="inherit" onClick={selection.clear}>
            清除
          </Button>
        )}
      </Stack>
      {items.length > 0 ? (
        <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0 }}>
          {items.map((item, index) => (
            <Box key={item.product.id}>
              {index > 0 && <Divider component="div" />}
              <SelectedFoodItem item={item} selection={selection} />
            </Box>
          ))}
        </Box>
      ) : (
        <Typography color="textSecondary">撳食物加入呢度，揀好數量再一次過記低。</Typography>
      )}
      {items.length > 0 && (
        <>
          <Divider />
          <Stack spacing={1}>
            <Typography variant="subtitle2" component="h3">
              幾時食
            </Typography>
            <TimeOfDayPicker value={minutes} onChange={setMinutes} />
          </Stack>
          {isError && <Alert severity="error">加入唔到，請再試一次</Alert>}
          <Button
            variant="contained"
            size="large"
            fullWidth
            disabled={!valid}
            loading={isLoading}
            onClick={handleSubmit}
          >
            加入 {items.length} 樣（{noteModel.formatTime(minutes)}）
          </Button>
        </>
      )}
    </Stack>
  );
};
