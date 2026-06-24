import { PostComment } from './post-comment';

export interface CommentPage {
  items: PostComment[];
  nextCursor: string | null;
}
