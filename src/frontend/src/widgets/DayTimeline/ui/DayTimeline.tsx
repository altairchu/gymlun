import { Box, Typography } from '@mui/material';
import { alpha } from '@mui/material/styles';
import { useLayoutEffect, useRef, type FC } from 'react';
import { type NoteItem, noteApi, noteLib, noteModel } from '@/entities/note';
import {
  groupByTime,
  placeGroups,
  timelineHeight,
  toPx,
  useTimelineDrag,
  type PlacedGroup,
} from '../lib';
import { TimelineGroupCard } from './TimelineGroupCard';

interface Props {
  date: string;
  notes: NoteItem[];
  height: number | string;
  focusedMinutes: number | null;
  onEdit: (note: NoteItem) => void;
}

const GUTTER_WIDTH = 52;
const HOURS = Array.from({ length: 25 }, (_, hour) => hour);
const DEFAULT_SCROLL_MINUTES = 7 * 60;

export const DayTimeline: FC<Props> = ({ date, notes, height, focusedMinutes, onEdit }) => {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [updateNote] = noteApi.useUpdateNoteMutation();
  const now = noteLib.useNow();
  const isToday = date === now.date;

  const moveNotes = (notesToMove: NoteItem[], minutes: number): void => {
    notesToMove.forEach(note => {
      void updateNote({
        id: note.id,
        note: {
          date: note.date,
          time: noteModel.toApiTime(minutes),
          productId: note.product.id,
          productQuantity: note.productQuantity,
          displayOrder: note.displayOrder,
        },
      });
    });
  };

  const { drag, startDrag } = useTimelineDrag(scrollRef, moveNotes);
  const groups: PlacedGroup[] = placeGroups(groupByTime(notes));
  const firstMinutes = groups.at(0)?.minutes;

  useLayoutEffect(() => {
    const target =
      focusedMinutes ?? firstMinutes ?? (isToday ? now.minutes : DEFAULT_SCROLL_MINUTES);

    if (scrollRef.current) {
      scrollRef.current.scrollTop = Math.max(0, toPx(target) - 48);
    }
    // Scrolls when another day opens or food was just logged, not on every change of the day
    // eslint-disable-next-line @eslint-react/exhaustive-deps
  }, [date, focusedMinutes]);

  const draggedNoteIds = drag?.notes.map(note => note.id) ?? [];

  return (
    <Box component="section" aria-label="今日時間線">
      {notes.length === 0 && (
        <Typography color="textSecondary" sx={{ p: 2, pb: 1 }}>
          呢日未有記錄。揀好食物加入之後，就會喺呢度按時間排好。
        </Typography>
      )}
      <Box
        ref={scrollRef}
        sx={{
          position: 'relative',
          height,
          overflowY: 'auto',
          overscrollBehavior: 'contain',
          userSelect: drag ? 'none' : undefined,
          cursor: drag ? 'grabbing' : undefined,
        }}
      >
        <Box sx={{ position: 'relative', height: timelineHeight(groups) }}>
          {HOURS.map(hour => (
            <Box
              key={hour}
              aria-hidden
              sx={{
                position: 'absolute',
                top: toPx(hour * 60),
                left: 0,
                right: 0,
                borderTop: theme => `1px solid ${theme.palette.divider}`,
              }}
            >
              <Typography
                variant="caption"
                color="textSecondary"
                sx={{
                  position: 'absolute',
                  top: -9,
                  left: 4,
                  px: 0.5,
                  bgcolor: 'background.paper',
                }}
              >
                {`${hour.toString().padStart(2, '0')}:00`}
              </Typography>
            </Box>
          ))}
          {isToday && (
            <Box
              aria-hidden
              sx={{
                position: 'absolute',
                top: toPx(now.minutes),
                left: GUTTER_WIDTH - 6,
                right: 0,
                borderTop: theme => `2px solid ${theme.palette.error.main}`,
                zIndex: 2,
                '&::before': {
                  content: '""',
                  position: 'absolute',
                  top: -6,
                  left: -2,
                  width: 10,
                  height: 10,
                  borderRadius: '50%',
                  bgcolor: 'error.main',
                },
              }}
            />
          )}
          <Box
            component="ul"
            sx={{ position: 'absolute', inset: 0, left: GUTTER_WIDTH, right: 8, m: 0, p: 0 }}
          >
            {groups.map(group => (
              <TimelineGroupCard
                key={`${group.minutes}-${group.notes[0]?.id}`}
                group={group}
                draggedNoteIds={draggedNoteIds}
                onDragStart={startDrag}
                onMove={moveNotes}
                onEdit={onEdit}
              />
            ))}
          </Box>
          {drag && (
            <Box
              role="status"
              sx={{
                position: 'absolute',
                top: toPx(drag.minutes),
                left: 0,
                right: 0,
                borderTop: theme => `2px dashed ${theme.palette.primary.main}`,
                zIndex: 3,
                pointerEvents: 'none',
              }}
            >
              <Box
                sx={theme => ({
                  position: 'absolute',
                  top: 4,
                  left: GUTTER_WIDTH,
                  right: 8,
                  px: 1.5,
                  py: 0.75,
                  borderRadius: 1,
                  color: 'primary.contrastText',
                  bgcolor: alpha(theme.palette.primary.main, 0.92),
                  boxShadow: theme.shadows[4],
                })}
              >
                <Typography variant="subtitle2" noWrap>
                  {noteModel.formatTime(drag.minutes)} ·{' '}
                  {drag.notes.map(note => note.product.name).join('、')}
                </Typography>
              </Box>
            </Box>
          )}
        </Box>
      </Box>
    </Box>
  );
};
