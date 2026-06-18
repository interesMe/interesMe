export type UserInterestLevel = 'interested' | 'learning' | 'experienced' | 'professional';

export interface InterestCategory {
  id: string;
  name: string;
  slug: string;
  description: string;
  icon: string;
  color: string;
  peopleCount: number;
  initiativesCount: number;
  portfolioCount: number;
  interests: Interest[];
}

export interface Interest {
  id: string;
  categoryId: string;
  name: string;
  slug: string;
  description: string;
  icon: string;
  color: string;
  peopleCount: number;
  initiativesCount: number;
  portfolioCount: number;
  subinterests: Subinterest[];
}

export interface Subinterest {
  id: string;
  interestId: string;
  name: string;
  slug: string;
  description?: string;
  peopleCount?: number;
  initiativesCount?: number;
  portfolioCount?: number;
}
