import { useCallback, useEffect, useRef, useState, type PointerEvent, type RefObject } from 'react';
import { type NoteItem, noteModel } from '@/entities/note';
import { PX_PER_MINUTE } from './layout';

export interface TimelineDrag {
  notes: NoteItem[];
  fromMinutes: number;
  minutes: number;
}

type DropFn = (notes: NoteItem[], minutes: number) => void;

const AUTO_SCROLL_EDGE = 48;
const AUTO_SCROLL_STEP = 8;

interface Result {
  drag: TimelineDrag | null;
  startDrag: (event: PointerEvent<HTMLElement>, notes: NoteItem[], fromMinutes: number) => void;
}

export const useTimelineDrag = (
  scrollRef: RefObject<HTMLElement | null>,
  onDrop: DropFn,
): Result => {
  const [drag, setDrag] = useState<TimelineDrag | null>(null);
  const stopRef = useRef<(() => void) | null>(null);
  const onDropRef = useRef(onDrop);

  useEffect(() => {
    onDropRef.current = onDrop;
  }, [onDrop]);

  useEffect(() => () => stopRef.current?.(), []);

  const startDrag = useCallback<Result['startDrag']>(
    (event, notes, fromMinutes) => {
      const container = scrollRef.current;

      if (!container || event.button !== 0) {
        return;
      }

      event.preventDefault();
      stopRef.current?.();

      const startY = event.clientY;
      const startScrollTop = container.scrollTop;
      let pointerY = startY;
      let minutes = fromMinutes;
      let frame = 0;

      const update = (): void => {
        const offset = pointerY - startY + container.scrollTop - startScrollTop;
        minutes = noteModel.snapTime(fromMinutes + offset / PX_PER_MINUTE);
        setDrag({ notes, fromMinutes, minutes });
      };

      const autoScroll = (): void => {
        const { top, bottom, height } = container.getBoundingClientRect();
        let step = 0;

        if (height > 0 && pointerY < top + AUTO_SCROLL_EDGE) {
          step = -AUTO_SCROLL_STEP;
        } else if (height > 0 && pointerY > bottom - AUTO_SCROLL_EDGE) {
          step = AUTO_SCROLL_STEP;
        }

        if (step !== 0) {
          container.scrollTop += step;
          update();
        }

        frame = requestAnimationFrame(autoScroll);
      };

      const handleMove = (moveEvent: globalThis.PointerEvent): void => {
        pointerY = moveEvent.clientY;
        update();
      };

      const stop = (): void => {
        cancelAnimationFrame(frame);
        window.removeEventListener('pointermove', handleMove);
        window.removeEventListener('pointerup', handleUp);
        window.removeEventListener('pointercancel', stop);
        stopRef.current = null;
        setDrag(null);
      };

      const handleUp = (): void => {
        stop();

        if (minutes !== fromMinutes) {
          onDropRef.current(notes, minutes);
        }
      };

      window.addEventListener('pointermove', handleMove);
      window.addEventListener('pointerup', handleUp);
      window.addEventListener('pointercancel', stop);
      stopRef.current = stop;
      frame = requestAnimationFrame(autoScroll);
      setDrag({ notes, fromMinutes, minutes });
    },
    [scrollRef],
  );

  return { drag, startDrag };
};
