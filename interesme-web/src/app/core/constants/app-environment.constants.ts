import { environment } from '../../../environments/environment';

interface RuntimeEnvironment {
  apiBaseUrl?: string;
  googleClientId?: string;
}

const runtimeEnvironment = (globalThis as typeof globalThis & { __INTERESME_CONFIG__?: RuntimeEnvironment })
  .__INTERESME_CONFIG__;

export const APP_ENVIRONMENT = {
  ...environment,
  ...runtimeEnvironment,
} as const;
