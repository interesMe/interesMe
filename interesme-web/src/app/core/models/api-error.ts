export type ApiValidationErrors = Record<string, string[]>;

export interface ApiError {
  status: number;
  message: string;
  validationErrors: ApiValidationErrors | null;
  raw?: unknown;
}
