import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { POSTS_API_ENDPOINTS } from '../../../core/constants/api.constants';
import { CommentPage, CreateCommentRequest, CreatePostRequest, Post, PostComment, PostPage } from '../models';

@Injectable({ providedIn: 'root' })
export class PostsApiService {
  private readonly http = inject(HttpClient);

  create(request: CreatePostRequest, attachments: readonly File[] = []): Observable<Post> {
    if (attachments.length === 0) {
      return this.http.post<Post>(POSTS_API_ENDPOINTS.create, request);
    }

    const formData = new FormData();
    formData.append('body', request.body);
    for (const attachment of attachments) {
      formData.append('attachments', attachment);
    }

    return this.http.post<Post>(POSTS_API_ENDPOINTS.create, formData);
  }

  getById(postId: string): Observable<Post> {
    return this.http.get<Post>(POSTS_API_ENDPOINTS.byId(postId));
  }

  getMyPosts(cursor?: string | null, pageSize = 20): Observable<PostPage> {
    return this.http.get<PostPage>(POSTS_API_ENDPOINTS.myPosts, {
      params: this.pageParams(cursor, pageSize),
    });
  }

  getUserPosts(userId: string, cursor?: string | null, pageSize = 20): Observable<PostPage> {
    return this.http.get<PostPage>(POSTS_API_ENDPOINTS.userPosts(userId), {
      params: this.pageParams(cursor, pageSize),
    });
  }

  getComments(postId: string, cursor?: string | null, pageSize = 20): Observable<CommentPage> {
    return this.http.get<CommentPage>(POSTS_API_ENDPOINTS.comments(postId), {
      params: this.pageParams(cursor, pageSize),
    });
  }

  createComment(postId: string, request: CreateCommentRequest): Observable<PostComment> {
    return this.http.post<PostComment>(POSTS_API_ENDPOINTS.comments(postId), request);
  }

  createReply(postId: string, commentId: string, request: CreateCommentRequest): Observable<PostComment> {
    return this.http.post<PostComment>(POSTS_API_ENDPOINTS.commentReplies(postId, commentId), request);
  }

  deleteComment(postId: string, commentId: string): Observable<void> {
    return this.http.delete<void>(POSTS_API_ENDPOINTS.comment(postId, commentId));
  }

  private pageParams(cursor: string | null | undefined, pageSize: number): HttpParams {
    let params = new HttpParams().set('pageSize', pageSize);
    if (cursor) {
      params = params.set('cursor', cursor);
    }

    return params;
  }
}
