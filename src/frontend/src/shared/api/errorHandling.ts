import { type SerializedError } from '@reduxjs/toolkit';
import { type FetchBaseQueryError } from '@reduxjs/toolkit/query';

interface ProblemDetails {
  title?: string;
  detail?: string;
}

interface ProblemDetailsError {
  data: ProblemDetails;
}

const isSerializedError = (error: unknown): error is SerializedError =>
  typeof error === 'object' && error != null && 'message' in error;

const isFetchBaseQueryError = (error: unknown): error is FetchBaseQueryError =>
  typeof error === 'object' && error != null && 'status' in error;

const isProblemDetails = (value: unknown): value is ProblemDetails =>
  typeof value === 'object' && value !== null;

const isProblemDetailsError = (error: unknown): error is ProblemDetailsError =>
  isFetchBaseQueryError(error) && isProblemDetails(error.data);

const UNKNOWN_ERROR_TITLE = '發生錯誤';
const UNKNOWN_ERROR_MESSAGE = '出了點問題，請稍後再試。';

export interface ClientError {
  title: string;
  message: string;
}

export const parseClientError = (error: unknown): ClientError => {
  if (isSerializedError(error)) {
    return {
      title: UNKNOWN_ERROR_TITLE,
      message: error.message ?? UNKNOWN_ERROR_MESSAGE,
    };
  }

  if (isProblemDetailsError(error)) {
    return {
      title: error.data.title ?? UNKNOWN_ERROR_TITLE,
      message: error.data.detail ?? UNKNOWN_ERROR_MESSAGE,
    };
  }

  return {
    title: UNKNOWN_ERROR_TITLE,
    message: UNKNOWN_ERROR_MESSAGE,
  };
};
