import { APP_ROUTES } from '../constants/routes.constants';

const DEFAULT_RETURN_URL = `/${APP_ROUTES.initiatives}`;
const AUTH_PREFIX = `/${APP_ROUTES.auth}`;

export function normalizeReturnUrl(returnUrl: string | null | undefined): string {
  const trimmedReturnUrl = returnUrl?.trim();

  return isValidReturnUrl(trimmedReturnUrl) ? trimmedReturnUrl : DEFAULT_RETURN_URL;
}

export function isValidReturnUrl(returnUrl: string | null | undefined): returnUrl is string {
  if (!returnUrl) {
    return false;
  }

  return (
    returnUrl.startsWith('/') &&
    !returnUrl.startsWith('//') &&
    returnUrl !== AUTH_PREFIX &&
    !returnUrl.startsWith(`${AUTH_PREFIX}/`)
  );
}
