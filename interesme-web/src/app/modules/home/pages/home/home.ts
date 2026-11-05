import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthService } from '../../../auth/services/auth.service';
import { DirectConversation } from '../../../chat/models';
import { ChatApiService } from '../../../chat/services/chat-api.service';
import { Initiative } from '../../../initiatives/models';
import { InitiativesApiService } from '../../../initiatives/services/initiatives-api.service';
import { PostAttachmentGallery } from '../../../posts/components/post-attachment-gallery/post-attachment-gallery';
import { Post } from '../../../posts/models';
import { PostsApiService } from '../../../posts/services/posts-api.service';

/**
 * Home dashboard (IME-16). Everything shown here comes from the API:
 * the personalised feed, the latest open initiatives and the user's recent conversations.
 */
@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [RouterLink, PostAttachmentGallery],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class HomePage implements OnInit {
  private static readonly feedPageSize = 10;

  private readonly i18n = inject(I18nService);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly postsApi = inject(PostsApiService);
  private readonly initiativesApi = inject(InitiativesApiService);
  private readonly chatApi = inject(ChatApiService);

  readonly createInitiativePath = `/${APP_ROUTES.initiatives}/new`;
  readonly initiativesPath = `/${APP_ROUTES.initiatives}`;
  readonly chatsPath = `/${APP_ROUTES.chat}`;
  readonly peoplePath = `/${APP_ROUTES.matches}`;
  readonly createPostPath = `/${APP_ROUTES.posts}/new`;

  readonly posts = signal<Post[]>([]);
  readonly nextCursor = signal<string | null>(null);
  readonly isFeedLoading = signal(true);
  readonly isLoadingMore = signal(false);
  readonly feedError = signal<string | null>(null);
  readonly likePending = signal<Set<string>>(new Set<string>());

  readonly initiatives = signal<Initiative[]>([]);
  readonly initiativesError = signal(false);
  readonly conversations = signal<DirectConversation[]>([]);
  readonly conversationsError = signal(false);
  readonly searchText = signal('');

  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      welcomeBack: this.i18n.t('home.hero.eyebrow'),
      create: this.i18n.t('home.hero.create'),
      explore: this.i18n.t('home.hero.explore'),
      description: this.i18n.t('home.hero.description'),
      searchPlaceholder: this.i18n.t('home.search.placeholder'),
      searchButton: this.i18n.t('home.search.button'),
      createInitiativeTitle: this.i18n.t('home.actions.create.title'),
      createInitiativeDescription: this.i18n.t('home.actions.create.description'),
      createInitiativeButton: this.i18n.t('home.actions.create.button'),
      exploreInitiativesTitle: this.i18n.t('home.actions.explore.title'),
      exploreInitiativesDescription: this.i18n.t('home.actions.explore.description'),
      exploreInitiativesButton: this.i18n.t('home.actions.explore.button'),
      feedEyebrow: this.i18n.t('home.feed.eyebrow'),
      feedTitle: this.i18n.t('home.feed.title'),
      feedLoading: this.i18n.t('home.feed.loading'),
      feedError: this.i18n.t('home.feed.error'),
      retry: this.i18n.t('home.feed.retry'),
      feedEmptyTitle: this.i18n.t('home.feed.emptyTitle'),
      feedEmptyDescription: this.i18n.t('home.feed.emptyDescription'),
      findPeople: this.i18n.t('home.feed.findPeople'),
      writePost: this.i18n.t('home.feed.writePost'),
      loadMore: this.i18n.t('home.feed.loadMore'),
      openPost: this.i18n.t('home.feed.openPost'),
      inInitiative: this.i18n.t('home.feed.inInitiative'),
      like: this.i18n.t('home.feed.like'),
      unlike: this.i18n.t('home.feed.unlike'),
      featuredInitiatives: this.i18n.t('home.featured.title'),
      noInitiatives: this.i18n.t('home.featured.empty'),
      viewAll: this.i18n.t('home.common.viewAll'),
      messages: this.i18n.t('home.messages.title'),
      noConversations: this.i18n.t('home.messages.empty'),
      noMessagePreview: this.i18n.t('home.messages.noPreview'),
      sidebarError: this.i18n.t('home.sidebar.error'),
      missionTitle: this.i18n.t('home.mission.title'),
      missionDescription: this.i18n.t('home.mission.description'),
    };
  });

  ngOnInit(): void {
    this.loadFeed();
    this.initiativesApi.getAll({ limit: 3 }).subscribe({
      next: (initiatives) => this.initiatives.set(initiatives),
      error: () => this.initiativesError.set(true),
    });
    this.chatApi.getDirectConversations().subscribe({
      next: (conversations) =>
        this.conversations.set(
          [...conversations].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).slice(0, 3),
        ),
      error: () => this.conversationsError.set(true),
    });
  }

  loadFeed(): void {
    this.isFeedLoading.set(true);
    this.feedError.set(null);

    this.postsApi.getFeed(null, HomePage.feedPageSize).subscribe({
      next: (page) => {
        this.posts.set(page.items);
        this.nextCursor.set(page.nextCursor);
        this.isFeedLoading.set(false);
      },
      error: (error: unknown) => {
        this.feedError.set(getApiErrorMessage(error, this.text().feedError));
        this.isFeedLoading.set(false);
      },
    });
  }

  loadMore(): void {
    const cursor = this.nextCursor();

    if (!cursor || this.isLoadingMore()) {
      return;
    }

    this.isLoadingMore.set(true);
    this.postsApi.getFeed(cursor, HomePage.feedPageSize).subscribe({
      next: (page) => {
        this.posts.update((posts) => [...posts, ...page.items]);
        this.nextCursor.set(page.nextCursor);
        this.isLoadingMore.set(false);
      },
      error: (error: unknown) => {
        this.feedError.set(getApiErrorMessage(error, this.text().feedError));
        this.isLoadingMore.set(false);
      },
    });
  }

  toggleLike(post: Post): void {
    if (this.likePending().has(post.id)) {
      return;
    }

    this.likePending.update((pending) => new Set(pending).add(post.id));
    const request = post.isLikedByCurrentUser ? this.postsApi.unlikePost(post.id) : this.postsApi.likePost(post.id);

    request.subscribe({
      next: (state) => {
        this.posts.update((posts) =>
          posts.map((current) =>
            current.id === post.id
              ? { ...current, likesCount: state.likesCount, isLikedByCurrentUser: state.isLikedByCurrentUser }
              : current,
          ),
        );
        this.clearLikePending(post.id);
      },
      error: () => this.clearLikePending(post.id),
    });
  }

  isLikePending(postId: string): boolean {
    return this.likePending().has(postId);
  }

  updateSearch(event: Event): void {
    this.searchText.set((event.target as HTMLInputElement).value);
  }

  search(event: Event): void {
    event.preventDefault();
    const query = this.searchText().trim();
    void this.router.navigate(['/', APP_ROUTES.initiatives], { queryParams: query ? { search: query } : {} });
  }

  profilePath(userId: string): string[] {
    return ['/', 'u', userId];
  }

  initiativePath(initiativeId: string): string {
    return `/${APP_ROUTES.initiatives}/${initiativeId}`;
  }

  postPath(postId: string): string {
    return `/${APP_ROUTES.posts}/${postId}`;
  }

  conversationPath(conversation: DirectConversation): string {
    return `/${APP_ROUTES.chat}/direct/${conversation.id}`;
  }

  conversationPartner(conversation: DirectConversation): string {
    const myId = this.authService.user()?.id;
    return (
      conversation.participants.find((participant) => participant.userId !== myId)?.displayName ??
      conversation.participants[0]?.displayName ??
      ''
    );
  }

  initial(name: string): string {
    return name.trim().charAt(0).toUpperCase() || 'I';
  }

  avatarUrl(url: string | null): string | null {
    const trimmed = url?.trim();

    if (!trimmed) {
      return null;
    }

    if (!trimmed.startsWith('/uploads')) {
      return trimmed;
    }

    const apiBase = APP_ENVIRONMENT.apiBaseUrl.replace(/\/$/, '').replace(/\/api$/, '');
    const origin = /^https?:\/\//i.test(apiBase) ? apiBase : (globalThis.location?.origin ?? '');
    return `${origin}${trimmed}`;
  }

  formatDate(value: string): string {
    const locale = this.i18n.currentLocale() === 'uk' ? 'uk-UA' : 'en-US';
    return new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));
  }

  private clearLikePending(postId: string): void {
    this.likePending.update((pending) => {
      const next = new Set(pending);
      next.delete(postId);
      return next;
    });
  }
}
