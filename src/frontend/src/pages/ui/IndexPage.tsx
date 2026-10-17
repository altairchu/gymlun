import {
  Box,
  Card,
  CardContent,
  Drawer,
  Paper,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  Typography,
  type Theme,
  useMediaQuery,
} from '@mui/material';
import { useState, type FC, type ReactElement } from 'react';
import { type NoteItem, noteLib, noteModel } from '@/entities/note';
import { SelectedFoodsPanel, logFoodsModel, useFoodSelection } from '@/features/logFoods';
import { EditNoteDialog } from '@/features/note/edit';
import { APP_BAR_HEIGHT_SM } from '@/shared/constants';
import { Button, PageContainer } from '@/shared/ui';
import { DayTimeline } from '@/widgets/DayTimeline';
import { FoodPicker } from '@/widgets/FoodPicker';
import { NutritionSummaryWidget } from '@/widgets/NutritionSummaryWidget';

interface Props {
  date: string;
}

type MobileTab = 'pick' | 'timeline';

const SELECTION_BAR_HEIGHT = 72;

export const IndexPage: FC<Props> = ({ date }) => {
  const { data: notes } = noteLib.useNotes(date);
  const nutritionValues = noteModel.calculateNutritionValues(notes);
  const selection = useFoodSelection();
  const isDesktop = useMediaQuery<Theme>(theme => theme.breakpoints.up('md'), { noSsr: true });
  const [tab, setTab] = useState<MobileTab>('pick');
  const [panelOpened, setPanelOpened] = useState(false);
  const [editedNote, setEditedNote] = useState<NoteItem | null>(null);
  const [loggedMessage, setLoggedMessage] = useState<string | null>(null);
  const [focusedMinutes, setFocusedMinutes] = useState<number | null>(null);

  const selectedCalories = selection.items.reduce(
    (sum, item) => sum + logFoodsModel.caloriesOf(item),
    0,
  );

  const handleLogged = (count: number, minutes: number): void => {
    setPanelOpened(false);
    setFocusedMinutes(minutes);
    setLoggedMessage(`已加入 ${count} 樣食物（${noteModel.formatTime(minutes)}）`);
  };

  const panel = <SelectedFoodsPanel date={date} selection={selection} onLogged={handleLogged} />;

  const timeline = (height: string): ReactElement => (
    <Card>
      <DayTimeline
        date={date}
        notes={notes}
        height={height}
        focusedMinutes={focusedMinutes}
        onEdit={setEditedNote}
      />
    </Card>
  );

  return (
    <>
      <PageContainer
        sx={{
          pb: !isDesktop && tab === 'pick' && selection.items.length > 0 ? 12 : undefined,
        }}
      >
        <Stack spacing={2}>
          <NutritionSummaryWidget nutritionValues={nutritionValues} />
          {isDesktop ? (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: 'minmax(0, 1fr) 400px',
                gap: 3,
                alignItems: 'start',
              }}
            >
              <FoodPicker selection={selection} />
              <Stack
                spacing={2}
                sx={{
                  position: 'sticky',
                  top: APP_BAR_HEIGHT_SM + 24,
                  maxHeight: `calc(100vh - ${APP_BAR_HEIGHT_SM + 48}px)`,
                  overflowY: 'auto',
                  '& > *': { flexShrink: 0 },
                }}
              >
                <Card>
                  <CardContent>{panel}</CardContent>
                </Card>
                {timeline('480px')}
              </Stack>
            </Box>
          ) : (
            <>
              <Tabs
                value={tab}
                variant="fullWidth"
                onChange={(_event, value: MobileTab) => setTab(value)}
              >
                <Tab value="pick" label="揀食物" />
                <Tab value="timeline" label={`時間線（${notes.length}）`} />
              </Tabs>
              {tab === 'pick' ? (
                <FoodPicker selection={selection} />
              ) : (
                timeline('max(360px, calc(100dvh - 250px))')
              )}
            </>
          )}
        </Stack>
      </PageContainer>
      {!isDesktop && tab === 'pick' && selection.items.length > 0 && (
        <Paper
          elevation={8}
          square
          sx={{
            position: 'fixed',
            left: 0,
            right: 0,
            bottom: 0,
            height: SELECTION_BAR_HEIGHT,
            px: 2,
            display: 'flex',
            alignItems: 'center',
            gap: 2,
            zIndex: theme => theme.zIndex.appBar - 1,
            pb: 'env(safe-area-inset-bottom)',
          }}
        >
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography sx={{ fontWeight: 500 }}>已揀 {selection.items.length} 樣</Typography>
            <Typography variant="body2" color="textSecondary">
              {selectedCalories} kcal
            </Typography>
          </Box>
          <Button variant="contained" size="large" onClick={() => setPanelOpened(true)}>
            揀數量同時間
          </Button>
        </Paper>
      )}
      {!isDesktop && (
        <Drawer
          anchor="bottom"
          open={panelOpened}
          onClose={() => setPanelOpened(false)}
          slotProps={{
            paper: {
              sx: {
                maxHeight: '92dvh',
                borderTopLeftRadius: 16,
                borderTopRightRadius: 16,
                px: 2,
                pt: 1,
                pb: 'calc(16px + env(safe-area-inset-bottom))',
              },
            },
          }}
        >
          <Box
            aria-hidden
            sx={{
              width: 40,
              height: 4,
              borderRadius: 2,
              bgcolor: 'divider',
              mx: 'auto',
              mb: 1.5,
              flexShrink: 0,
            }}
          />
          {panel}
        </Drawer>
      )}
      {editedNote && <EditNoteDialog note={editedNote} onClose={() => setEditedNote(null)} />}
      <Snackbar
        open={loggedMessage !== null}
        autoHideDuration={4000}
        message={loggedMessage}
        onClose={() => setLoggedMessage(null)}
        action={
          !isDesktop && (
            <Button
              color="inherit"
              size="small"
              onClick={() => {
                setTab('timeline');
                setLoggedMessage(null);
              }}
            >
              睇時間線
            </Button>
          )
        }
      />
    </>
  );
};
