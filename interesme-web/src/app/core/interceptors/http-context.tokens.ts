import { HttpContextToken } from '@angular/common/http';

export const BYPASS_AUTH = new HttpContextToken<boolean>(() => false);
export const BYPASS_REFRESH = new HttpContextToken<boolean>(() => false);
