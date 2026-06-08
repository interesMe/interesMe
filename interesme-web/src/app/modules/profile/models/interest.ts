export interface InterestResponse {
  id: string;
  name: string;
  slug: string;
  createdAt: string;
}

export interface UpdateUserInterestsRequest {
  interestIds: string[];
}
