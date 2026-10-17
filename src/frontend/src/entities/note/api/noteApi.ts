import {
  type UpdateNoteRequest,
  type GetNotesRequest,
  type RecognizeNoteResponse,
  type GetNotesHistoryRequest,
  type GetNotesResponse,
  type GetNotesHistoryResponse,
  type NoteItem,
  type NoteRequestBody,
  type CreateNotesRequest,
  type CreateNotesResponse,
} from '@/entities/note';
import { api } from '@/shared/api';
import { createUrl } from '@/shared/lib';
import { compareByTime, sortByTime } from '../model/time';

export const noteApi = api.injectEndpoints({
  endpoints: builder => ({
    notes: builder.query<NoteItem[], GetNotesRequest>({
      query: ({ date }) => `/api/v1/notes?date=${date}`,
      providesTags: ['note'],
      transformResponse: ({ notes }: GetNotesResponse) => sortByTime(notes),
    }),

    notesHistory: builder.query<GetNotesHistoryResponse, GetNotesHistoryRequest>({
      query: request => createUrl('/api/v1/notes/history', { ...request }),
      providesTags: ['note'],
    }),

    createNote: builder.mutation<void, NoteRequestBody>({
      query: request => ({
        method: 'POST',
        url: '/api/v1/notes',
        body: request,
      }),
      invalidatesTags: ['note'],
    }),

    createNotes: builder.mutation<CreateNotesResponse, CreateNotesRequest>({
      query: request => ({
        method: 'POST',
        url: '/api/v1/notes/batch',
        body: request,
      }),
      invalidatesTags: ['note'],
    }),

    updateNote: builder.mutation<void, UpdateNoteRequest>({
      query: ({ id, note }) => ({
        method: 'PUT',
        url: `/api/v1/notes/${id}`,
        body: note,
      }),
      invalidatesTags: ['note'],
      onQueryStarted: async ({ id, note }, { dispatch, queryFulfilled }) => {
        const patch = dispatch(
          noteApi.util.updateQueryData('notes', { date: note.date }, notes => {
            const cached = notes.find(n => n.id === id);

            if (cached) {
              cached.time = note.time ?? cached.time;
              cached.productQuantity = note.productQuantity;
            }

            notes.sort(compareByTime);
          }),
        );

        try {
          await queryFulfilled;
        } catch {
          patch.undo();
        }
      },
    }),

    deleteNote: builder.mutation<void, number>({
      query: id => ({
        method: 'DELETE',
        url: `/api/v1/notes/${id}`,
      }),
      invalidatesTags: ['note'],
    }),

    recognize: builder.mutation<RecognizeNoteResponse, FormData>({
      query: body => ({
        method: 'POST',
        url: `/api/v1/notes/recognitions`,
        body,
      }),
    }),
  }),
});
