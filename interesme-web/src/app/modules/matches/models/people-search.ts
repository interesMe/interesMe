export interface SharedInterest {
  id: string;
  name: string;
  color: string;
}

export interface PersonMatch {
  userId: string;
  displayName: string;
  headline: string | null;
  city: string | null;
  avatarUrl: string | null;
  sharedInterestCount: number;
  sharedInterests: SharedInterest[];
  skills: string[];
}

export interface PeopleSearchResponse {
  interestIds: string[];
  usedMyInterests: boolean;
  items: PersonMatch[];
}
