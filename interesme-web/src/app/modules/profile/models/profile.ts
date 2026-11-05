export interface ProfileResponse {
  id: string;
  userId: string;
  displayName: string;
  headline: string | null;
  bio: string | null;
  city: string | null;
  avatarUrl: string | null;
  birthDate: string | null;
  skills: string[];
  createdAt: string;
  updatedAt: string;
}

export interface ProfileRequest {
  displayName: string;
  headline: string | null;
  bio: string | null;
  city: string | null;
  avatarUrl: string | null;
  birthDate: string | null;
  skills: string[];
}
