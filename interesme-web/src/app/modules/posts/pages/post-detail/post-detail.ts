import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { getApiErrorMessage, normalizeApiError } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../../auth/services/auth.service';
import { PostAttachmentGallery } from '../../components/post-attachment-gallery/post-attachment-gallery';
import { Post, PostComment } from '../../models';
import { PostsApiService } from '../../services/posts-api.service';

const COMMENT_BODY_MAX_LENGTH = 1200;
const COMMENTS_PAGE_SIZE = 20;

@Component({
  selector: 'app-post-detail-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, PostAttachmentGallery],
  templateUrl: './post-detail.html',
  styleUrl: './post-detail.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostDetailPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly postsApi = inject(PostsApiService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  readonly maxCommentLength = COMMENT_BODY_MAX_LENGTH;
  readonly profileRoute = ['/', APP_ROUTES.profile];
  readonly postId = signal<string | null>(null);
  readonly post = signal<Post | null>(null);
  readonly postLoading = signal(true);
  readonly postError = signal<string | null>(null);
  readonly comments = signal<PostComment[]>([]);
  readonly commentsLoading = signal(true);
  readonly commentsLoadingMore = signal(false);
  readonly commentsNextCursor = signal<string | null>(null);
  readonly commentsError = signal<string | null>(null);
  readonly isSubmitting = signal(false);
  readonly isSubmittingReply = signal(false);
  readonly isDeleting = signal<string | null>(null);
  readonly replyingToCommentId = signal<string | null>(null);
  readonly totalCommentCount = computed(() =>
    this.comments().reduce((total, comment) => total + 1 + comment.replies.length, 0),
  );
  readonly commentForm = this.formBuilder.group({
    body: ['', [Validators.required, Validators.maxLength(COMMENT_BODY_MAX_LENGTH)]],
  });
  readonly replyForm = this.formBuilder.group({
    body: ['', [Validators.required, Validators.maxLength(COMMENT_BODY_MAX_LENGTH)]],
  });
  readonly currentUserId = computed(() => this.authService.user()?.id ?? null);
  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      backToProfile: this.i18n.t('postDetail.backToProfile'),
      loadingPost: this.i18n.t('postDetail.loadingPost'),
      postErrorTitle: this.i18n.t('postDetail.postErrorTitle'),
      postErrorFallback: this.i18n.t('postDetail.postErrorFallback'),
      retry: this.i18n.t('postDetail.retry'),
      commentsTitle: this.i18n.t('postDetail.commentsTitle'),
      commentsSubtitle: this.i18n.t('postDetail.commentsSubtitle'),
      loadingComments: this.i18n.t('postDetail.loadingComments'),
      commentsErrorTitle: this.i18n.t('postDetail.commentsErrorTitle'),
      commentsErrorFallback: this.i18n.t('postDetail.commentsErrorFallback'),
      commentsEmptyTitle: this.i18n.t('postDetail.commentsEmptyTitle'),
      commentsEmptyDescription: this.i18n.t('postDetail.commentsEmptyDescription'),
      commentLabel: this.i18n.t('postDetail.commentLabel'),
      commentPlaceholder: this.i18n.t('postDetail.commentPlaceholder'),
      commentHint: this.i18n.t('postDetail.commentHint', { max: COMMENT_BODY_MAX_LENGTH }),
      commentRequired: this.i18n.t('postDetail.commentRequired'),
      commentTooLong: this.i18n.t('postDetail.commentTooLong', { max: COMMENT_BODY_MAX_LENGTH }),
      submitComment: this.i18n.t('postDetail.submitComment'),
      submittingComment: this.i18n.t('postDetail.submittingComment'),
      loadMore: this.i18n.t('postDetail.loadMore'),
      loadingMore: this.i18n.t('postDetail.loadingMore'),
      deleteComment: this.i18n.t('postDetail.deleteComment'),
      deletingComment: this.i18n.t('postDetail.deletingComment'),
      deleteErrorFallback: this.i18n.t('postDetail.deleteErrorFallback'),
      reply: this.i18n.t('postDetail.reply'),
      replyLabel: this.i18n.t('postDetail.replyLabel'),
      replyPlaceholder: this.i18n.t('postDetail.replyPlaceholder'),
      submitReply: this.i18n.t('postDetail.submitReply'),
      submittingReply: this.i18n.t('postDetail.submittingReply'),
      cancelReply: this.i18n.t('postDetail.cancelReply'),
      replyErrorFallback: this.i18n.t('postDetail.replyErrorFallback'),
    };
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.postId.set(params.get('postId'));
      this.loadPost();
      this.loadComments();
    });
  }

  loadPost(): void {
    const postId = this.postId();
    if (!postId) {
      return;
    }

    this.postLoading.set(true);
    this.postError.set(null);
    this.postsApi
      .getById(postId)
      .pipe(finalize(() => this.postLoading.set(false)))
      .subscribe({
        next: (post) => this.post.set(post),
        error: (error: unknown) => {
          this.post.set(null);
          this.postError.set(getApiErrorMessage(error, this.text().postErrorFallback));
        },
      });
  }

  loadComments(): void {
    const postId = this.postId();
    if (!postId) {
      return;
    }

    this.commentsLoading.set(true);
    this.commentsError.set(null);
    this.comments.set([]);
    this.commentsNextCursor.set(null);

    this.postsApi
      .getComments(postId, null, COMMENTS_PAGE_SIZE)
      .pipe(finalize(() => this.commentsLoading.set(false)))
      .subscribe({
        next: (page) => {
          this.comments.set(page.items);
          this.commentsNextCursor.set(page.nextCursor);
          this.cancelReply();
        },
        error: (error: unknown) => {
          this.commentsError.set(getApiErrorMessage(error, this.text().commentsErrorFallback));
        },
      });
  }

  loadMoreComments(): void {
    const postId = this.postId();
    const cursor = this.commentsNextCursor();
    if (!postId || !cursor || this.commentsLoadingMore()) {
      return;
    }

    this.commentsLoadingMore.set(true);
    this.commentsError.set(null);
    this.postsApi
      .getComments(postId, cursor, COMMENTS_PAGE_SIZE)
      .pipe(finalize(() => this.commentsLoadingMore.set(false)))
      .subscribe({
        next: (page) => {
          this.comments.update((current) => [...current, ...page.items]);
          this.commentsNextCursor.set(page.nextCursor);
        },
        error: (error: unknown) => {
          this.commentsError.set(getApiErrorMessage(error, this.text().commentsErrorFallback));
        },
      });
  }

  submitComment(): void {
    const postId = this.postId();
    if (!postId || this.isSubmitting()) {
      return;
    }

    const body = this.commentForm.controls.body.value.trim();
    if (!body) {
      this.commentForm.controls.body.setErrors({ required: true });
      this.commentForm.controls.body.markAsTouched();
      return;
    }

    if (body.length > COMMENT_BODY_MAX_LENGTH) {
      this.commentForm.controls.body.setErrors({ maxlength: true });
      this.commentForm.controls.body.markAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.commentsError.set(null);
    this.postsApi
      .createComment(postId, { body })
      .pipe(finalize(() => this.isSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.commentForm.reset();
          this.loadComments();
        },
        error: (error: unknown) => {
          const apiError = normalizeApiError(error, this.text().commentsErrorFallback);
          const bodyValidationError =
            apiError.validationErrors?.['Body']?.[0] ?? apiError.validationErrors?.['body']?.[0];
          this.commentsError.set(bodyValidationError ?? getApiErrorMessage(error, this.text().commentsErrorFallback));
        },
      });
  }

  deleteComment(commentId: string): void {
    const postId = this.postId();
    if (!postId || this.isDeleting()) {
      return;
    }

    this.isDeleting.set(commentId);
    this.commentsError.set(null);
    this.postsApi
      .deleteComment(postId, commentId)
      .pipe(finalize(() => this.isDeleting.set(null)))
      .subscribe({
        next: () => {
          this.comments.update((comments) =>
            comments
              .filter((comment) => comment.id !== commentId)
              .map((comment) => ({
                ...comment,
                replies: comment.replies.filter((reply) => reply.id !== commentId),
              })),
          );
          if (this.replyingToCommentId() === commentId) {
            this.cancelReply();
          }
        },
        error: (error: unknown) => {
          this.commentsError.set(getApiErrorMessage(error, this.text().deleteErrorFallback));
        },
      });
  }

  startReply(commentId: string): void {
    if (this.isSubmittingReply()) {
      return;
    }

    this.replyingToCommentId.set(commentId);
    this.replyForm.reset();
    this.commentsError.set(null);
  }

  cancelReply(): void {
    this.replyingToCommentId.set(null);
    this.replyForm.reset();
  }

  submitReply(parentCommentId: string): void {
    const postId = this.postId();
    if (!postId || this.isSubmittingReply()) {
      return;
    }

    const body = this.replyForm.controls.body.value.trim();
    if (!body) {
      this.replyForm.controls.body.setErrors({ required: true });
      this.replyForm.controls.body.markAsTouched();
      return;
    }

    if (body.length > COMMENT_BODY_MAX_LENGTH) {
      this.replyForm.controls.body.setErrors({ maxlength: true });
      this.replyForm.controls.body.markAsTouched();
      return;
    }

    this.isSubmittingReply.set(true);
    this.commentsError.set(null);
    this.postsApi
      .createReply(postId, parentCommentId, { body })
      .pipe(finalize(() => this.isSubmittingReply.set(false)))
      .subscribe({
        next: () => {
          this.cancelReply();
          this.loadComments();
        },
        error: (error: unknown) => {
          const apiError = normalizeApiError(error, this.text().replyErrorFallback);
          const bodyValidationError =
            apiError.validationErrors?.['Body']?.[0] ?? apiError.validationErrors?.['body']?.[0];
          this.commentsError.set(bodyValidationError ?? getApiErrorMessage(error, this.text().replyErrorFallback));
        },
      });
  }

  canDelete(comment: PostComment): boolean {
    return comment.author.id === this.currentUserId();
  }

  avatarUrl(value: string | null): string | null {
    if (!value?.trim()) {
      return null;
    }

    const avatarUrl = value.trim();
    if (/^https?:\/\//i.test(avatarUrl) || !avatarUrl.startsWith('/uploads')) {
      return avatarUrl;
    }

    return `${APP_ENVIRONMENT.apiBaseUrl.replace(/\/api\/?$/, '')}${avatarUrl}`;
  }

  initials(displayName: string): string {
    return displayName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  }

  formatDate(value: string): string {
    const locale = this.i18n.currentLocale() === 'uk' ? 'uk-UA' : 'en-US';
    return new Intl.DateTimeFormat(locale, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  commentValidationMessage(): string | null {
    return this.validationMessage(this.commentForm.controls.body);
  }

  replyValidationMessage(): string | null {
    return this.validationMessage(this.replyForm.controls.body);
  }

  private validationMessage(control: typeof this.commentForm.controls.body): string | null {
    if (!control.touched) {
      return null;
    }

    if (control.hasError('required') || control.value.trim().length === 0) {
      return this.text().commentRequired;
    }

    return control.hasError('maxlength') ? this.text().commentTooLong : null;
  }
}
