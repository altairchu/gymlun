import { type NoteItem, noteModel } from '@/entities/note';

export const PX_PER_MINUTE = 1.2;
export const GROUP_HEADER_HEIGHT = 30;
export const ROW_HEIGHT = 44;
export const GROUP_PADDING = 8;
export const GROUP_GAP = 6;

export interface TimelineGroup {
  minutes: number;
  notes: NoteItem[];
}

export interface PlacedGroup extends TimelineGroup {
  top: number;
  height: number;
}

export const groupByTime = (notes: NoteItem[]): TimelineGroup[] =>
  noteModel.sortByTime(notes).reduce<TimelineGroup[]>((groups, note) => {
    const minutes = noteModel.parseTime(note.time);
    const last = groups.at(-1);

    if (last?.minutes === minutes) {
      last.notes.push(note);
    } else {
      groups.push({ minutes, notes: [note] });
    }

    return groups;
  }, []);

export const toPx = (minutes: number): number => minutes * PX_PER_MINUTE;

const heightOf = ({ notes }: TimelineGroup): number =>
  GROUP_HEADER_HEIGHT + notes.length * ROW_HEIGHT + GROUP_PADDING;

/** Groups sit at their time unless an earlier group still covers it; then they move just below it */
export const placeGroups = (groups: TimelineGroup[]): PlacedGroup[] =>
  groups.reduce<PlacedGroup[]>((placed, group) => {
    const previous = placed.at(-1);
    const top = Math.max(
      toPx(group.minutes),
      previous ? previous.top + previous.height + GROUP_GAP : 0,
    );

    placed.push({ ...group, top, height: heightOf(group) });
    return placed;
  }, []);

export const timelineHeight = (placed: PlacedGroup[]): number => {
  const last = placed.at(-1);
  return Math.max(toPx(noteModel.MINUTES_IN_DAY), last ? last.top + last.height + GROUP_GAP : 0);
};
