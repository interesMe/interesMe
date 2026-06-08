export interface ProfileResponse {
  id: string;
  userId: string;
  displayName: string;
  city: string | null;
  avatarUrl: string | null;
  birthDate: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ProfileRequest {
  displayName: string;
  city: string | null;
  avatarUrl: string | null;
  birthDate: string | null;
}
