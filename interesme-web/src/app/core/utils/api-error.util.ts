import { HttpErrorResponse } from '@angular/common/http';

import { ApiError, ApiValidationErrors } from '../models/api-error';

const FALLBACK_MESSAGE = 'Something went wrong. Please try again.';

export function normalizeApiError(error: unknown, fallbackMessage = FALLBACK_MESSAGE): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return {
      status: 0,
      message: fallbackMessage,
      validationErrors: null,
      raw: error,
    };
  }

  const errorBody = isRecord(error.error) ? error.error : null;

  return {
    status: error.status,
    message: readMessage(errorBody, error.message, fallbackMessage),
    validationErrors: readValidationErrors(errorBody),
    raw: error.error,
  };
}

export function getApiErrorMessage(error: unknown, fallbackMessage = FALLBACK_MESSAGE): string {
  return normalizeApiError(error, fallbackMessage).message;
}

function readMessage(
  errorBody: Record<string, unknown> | null,
  httpMessage: string,
  fallbackMessage: string,
): string {
  const bodyMessage = errorBody?.['message'];

  if (typeof bodyMessage === 'string' && bodyMessage.trim().length > 0) {
    return bodyMessage;
  }

  return httpMessage.trim().length > 0 ? httpMessage : fallbackMessage;
}

function readValidationErrors(errorBody: Record<string, unknown> | null): ApiValidationErrors | null {
  if (!errorBody) {
    return null;
  }

  const validationErrors = errorBody['validationErrors'] ?? errorBody['errors'];

  if (!isRecord(validationErrors)) {
    return null;
  }

  const normalized: ApiValidationErrors = {};

  for (const [key, value] of Object.entries(validationErrors)) {
    if (Array.isArray(value)) {
      normalized[key] = value.filter((item): item is string => typeof item === 'string');
      continue;
    }

    if (typeof value === 'string') {
      normalized[key] = [value];
    }
  }

  return Object.keys(normalized).length > 0 ? normalized : null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
