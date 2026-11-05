export type ApiValidationErrors = Record<string, string[]>;

export interface ApiError {
  status: number;
  /** Machine-readable error code from ApiErrorResponse (e.g. "initiative.apply.already_exists"). */
  code: string | null;
  message: string;
  validationErrors: ApiValidationErrors | null;
  raw?: unknown;
}
