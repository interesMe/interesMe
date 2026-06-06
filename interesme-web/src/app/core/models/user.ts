export interface User {
  id: string;
  email: string;
  displayName: string;
  firstName?: string | null;
  lastName?: string | null;
  avatarUrl?: string | null;
  roles?: string[];
}
