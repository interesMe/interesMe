import { PostAttachment } from './post-attachment';

export interface Post {
  id: string;
  authorId: string;
  authorDisplayName: string;
  authorAvatarUrl: string | null;
  body: string;
  initiativeId: string | null;
  initiativeTitle: string | null;
  likesCount: number;
  isLikedByCurrentUser: boolean;
  attachments: PostAttachment[];
  createdAt: string;
}
