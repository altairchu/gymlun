import { type NoteItem, noteApi } from '../api';

interface Result {
  data: NoteItem[];
  isFetching: boolean;
  isChanged: boolean;
}

const EMPTY_NOTES: NoteItem[] = [];

export const useNotes = (date: string): Result =>
  noteApi.useNotesQuery(
    {
      date,
    },
    {
      selectFromResult: ({ data, isFetching, isSuccess }) => ({
        data: data ?? EMPTY_NOTES,
        isFetching,
        isChanged: !isFetching && isSuccess,
      }),
    },
  );
