import { Post } from './post';

export interface PostPage {
  items: Post[];
  nextCursor: string | null;
}
