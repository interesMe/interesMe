import { CommentAuthor } from './comment-author';

export interface PostComment {
  id: string;
  postId: string;
  parentCommentId: string | null;
  author: CommentAuthor;
  body: string;
  createdAt: string;
  replies: PostComment[];
}
