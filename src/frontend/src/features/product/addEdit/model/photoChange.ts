export type PhotoChange =
  { type: 'unchanged' } | { type: 'set'; photo: Blob; previewUrl: string } | { type: 'removed' };

export const PHOTO_UNCHANGED: PhotoChange = { type: 'unchanged' };

export const PHOTO_REMOVED: PhotoChange = { type: 'removed' };
