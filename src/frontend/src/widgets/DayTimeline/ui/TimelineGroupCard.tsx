import DragIndicatorIcon from '@mui/icons-material/DragIndicator';
import { Box, ButtonBase, Paper, Stack, Typography } from '@mui/material';
import { type FC, type KeyboardEvent, type PointerEvent } from 'react';
import { type NoteItem, noteModel } from '@/entities/note';
import { productModel } from '@/entities/product';
import { GROUP_HEADER_HEIGHT, ROW_HEIGHT, type PlacedGroup } from '../lib';

interface Props {
  group: PlacedGroup;
  draggedNoteIds: number[];
  onDragStart: (event: PointerEvent<HTMLElement>, notes: NoteItem[], fromMinutes: number) => void;
  onMove: (notes: NoteItem[], minutes: number) => void;
  onEdit: (note: NoteItem) => void;
}

const KEYBOARD_STEP_MINUTES = 15;

const HANDLE_SX = {
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'center',
  width: 36,
  alignSelf: 'stretch',
  flexShrink: 0,
  color: 'text.secondary',
  cursor: 'grab',
  touchAction: 'none',
  borderRadius: 1,
} as const;

export const TimelineGroupCard: FC<Props> = ({
  group,
  draggedNoteIds,
  onDragStart,
  onMove,
  onEdit,
}) => {
  const time = noteModel.formatTime(group.minutes);
  const calories = group.notes.reduce((sum, note) => sum + noteModel.calculateCalories(note), 0);

  const handleKeyDown = (event: KeyboardEvent<HTMLElement>, notes: NoteItem[]): void => {
    const direction = { ArrowUp: -1, ArrowDown: 1 }[event.key];

    if (direction !== undefined) {
      event.preventDefault();
      onMove(notes, noteModel.clampTime(group.minutes + direction * KEYBOARD_STEP_MINUTES));
    }
  };

  return (
    <Paper
      variant="outlined"
      component="li"
      aria-label={`${time}，${group.notes.length} 樣，${calories} kcal`}
      sx={{
        position: 'absolute',
        top: group.top,
        left: 0,
        right: 0,
        height: group.height,
        overflow: 'hidden',
        borderLeft: theme => `4px solid ${theme.palette.primary.main}`,
        opacity: group.notes.every(note => draggedNoteIds.includes(note.id)) ? 0.4 : 1,
      }}
    >
      <Stack
        direction="row"
        sx={{ height: GROUP_HEADER_HEIGHT, alignItems: 'center', pr: 1.5, bgcolor: 'action.hover' }}
      >
        <ButtonBase
          aria-label={`移動 ${time} 全部食物`}
          onPointerDown={event => onDragStart(event, group.notes, group.minutes)}
          onKeyDown={event => handleKeyDown(event, group.notes)}
          sx={HANDLE_SX}
        >
          <DragIndicatorIcon fontSize="small" />
        </ButtonBase>
        <Typography variant="subtitle2" component="span" sx={{ flex: 1 }}>
          {time}
        </Typography>
        <Typography variant="caption" color="textSecondary">
          {calories} kcal
        </Typography>
      </Stack>
      <Box component="ul" sx={{ listStyle: 'none', m: 0, p: 0 }}>
        {group.notes.map(note => (
          <Stack
            key={note.id}
            component="li"
            direction="row"
            sx={{
              height: ROW_HEIGHT,
              alignItems: 'center',
              opacity: draggedNoteIds.includes(note.id) ? 0.4 : 1,
            }}
          >
            <ButtonBase
              aria-label={`移動${note.product.name}`}
              onPointerDown={event => onDragStart(event, [note], group.minutes)}
              onKeyDown={event => handleKeyDown(event, [note])}
              sx={HANDLE_SX}
            >
              <DragIndicatorIcon fontSize="small" />
            </ButtonBase>
            <ButtonBase
              aria-label={`修改${note.product.name}`}
              onClick={() => onEdit(note)}
              sx={{
                flex: 1,
                minWidth: 0,
                height: '100%',
                justifyContent: 'flex-start',
                gap: 1,
                pr: 1.5,
                textAlign: 'left',
              }}
            >
              <Box sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="body2" noWrap sx={{ fontWeight: 500 }}>
                  {note.product.name}
                </Typography>
                <Typography variant="caption" color="textSecondary" noWrap component="div">
                  {productModel.formatQuantity(
                    note.productQuantity,
                    note.product.quantityUnit,
                    note.product.defaultQuantity,
                  )}
                </Typography>
              </Box>
              <Typography variant="body2" color="textSecondary" sx={{ whiteSpace: 'nowrap' }}>
                {noteModel.calculateCalories(note)} kcal
              </Typography>
            </ButtonBase>
          </Stack>
        ))}
      </Box>
    </Paper>
  );
};
