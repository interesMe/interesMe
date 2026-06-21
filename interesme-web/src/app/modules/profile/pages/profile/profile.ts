import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { I18nService } from '../../../../core/i18n/i18n.service';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { ChatApiService } from '../../../chat/services/chat-api.service';
import { AuthService } from '../../../auth/services/auth.service';
import { InitiativeStatus } from '../../../initiatives/models';
import { PostAttachmentGallery } from '../../../posts/components/post-attachment-gallery/post-attachment-gallery';
import { Post } from '../../../posts/models';
import { PostsApiService } from '../../../posts/services/posts-api.service';
import { ProfileHistoryTimelineComponent } from '../../components/profile-history-timeline/profile-history-timeline';
import { ProfileAboutCardComponent } from '../../components/profile-about-card/profile-about-card';
import { ProfileActivityStatus, ProfileInitiativePreview, ProfileStatus, ProfileViewResponse } from '../../models';
import { ProfileApiService } from '../../services/profile-api.service';

type ProfileTab = 'posts' | 'history' | 'initiatives' | 'interests';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [RouterLink, ProfileAboutCardComponent, ProfileHistoryTimelineComponent, PostAttachmentGallery],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly profileApi = inject(ProfileApiService);
  private readonly postsApi = inject(PostsApiService);
  private readonly chatApi = inject(ChatApiService);
  private readonly authService = inject(AuthService);
  private readonly i18n = inject(I18nService);

  readonly profile = signal<ProfileViewResponse | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly activeTab = signal<ProfileTab>('posts');
  readonly followLoading = signal(false);
  readonly messageLoading = signal(false);
  readonly publicUserId = signal<string | null>(null);
  readonly posts = signal<Post[]>([]);
  readonly postsLoading = signal(true);
  readonly postsLoadingMore = signal(false);
  readonly postsError = signal<string | null>(null);
  readonly postsNextCursor = signal<string | null>(null);
  readonly isOwnProfile = computed(() => {
    const publicUserId = this.publicUserId();
    return publicUserId === null || publicUserId === this.authService.user()?.id;
  });
  readonly editProfileRoute = ['/', APP_ROUTES.profile, 'edit'];
  readonly createPostRoute = ['/', APP_ROUTES.posts, 'new'];
  readonly totalInitiatives = computed(() => {
    const stats = this.profile()?.stats;
    return (stats?.createdInitiativesCount ?? 0) + (stats?.joinedInitiativesCount ?? 0);
  });
  readonly text = computed(() => {
    this.i18n.currentLocale();

    return {
      loading: this.i18n.t('profileView.loading'),
      errorTitle: this.i18n.t('profileView.error.title'),
      errorFallback: this.i18n.t('profileView.error.fallback'),
      retry: this.i18n.t('profileView.error.retry'),
      sharedInterests: this.i18n.t('profileView.header.sharedInterests'),
      joined: this.i18n.t('profileView.header.joined'),
      statusUnavailable: this.i18n.t('profileView.status.unavailable'),
      statusOnline: this.i18n.t('profileView.status.online'),
      statusRecentlyActive: this.i18n.t('profileView.status.recentlyActive'),
      statusOffline: this.i18n.t('profileView.status.offline'),
      editProfile: this.i18n.t('profileView.action.editProfile'),
      message: this.i18n.t('profileView.action.message'),
      messaging: this.i18n.t('profileView.action.messaging'),
      follow: this.i18n.t('profileView.action.follow'),
      following: this.i18n.t('profileView.action.following'),
      followLoading: this.i18n.t('profileView.action.followLoading'),
      more: this.i18n.t('profileView.action.more'),
      posts: this.i18n.t('profileView.tab.posts'),
      history: this.i18n.t('profileView.tab.history'),
      initiatives: this.i18n.t('profileView.tab.initiatives'),
      interests: this.i18n.t('profileView.tab.interests'),
      noPosts: this.i18n.t('profileView.posts.empty'),
      createPost: this.i18n.t('profileView.posts.create'),
      loadingPosts: this.i18n.t('profileView.posts.loading'),
      loadMorePosts: this.i18n.t('profileView.posts.loadMore'),
      loadingMorePosts: this.i18n.t('profileView.posts.loadingMore'),
      postsErrorFallback: this.i18n.t('profileView.posts.errorFallback'),
      retryPosts: this.i18n.t('profileView.posts.retry'),
      noHistory: this.i18n.t('profileView.history.empty'),
      createdInitiatives: this.i18n.t('profileView.initiatives.created'),
      joinedInitiatives: this.i18n.t('profileView.initiatives.joined'),
      noCreatedInitiatives: this.i18n.t('profileView.initiatives.createdEmpty'),
      noJoinedInitiatives: this.i18n.t('profileView.initiatives.joinedEmpty'),
      creator: this.i18n.t('profileView.initiatives.creator'),
      participant: this.i18n.t('profileView.initiatives.participant'),
      noInterests: this.i18n.t('profileView.interests.empty'),
      about: this.i18n.t('profileView.about.title'),
      bioEmpty: this.i18n.t('profileView.about.bioEmpty'),
      city: this.i18n.t('profileView.about.city'),
      memberSince: this.i18n.t('profileView.about.memberSince'),
      socialLinks: this.i18n.t('profileView.about.socialLinks'),
      noSocialLinks: this.i18n.t('profileView.about.socialEmpty'),
      interestsCard: this.i18n.t('profileView.interests.title'),
      interestsCount: this.i18n.t('profileView.interests.count'),
      seeAll: this.i18n.t('profileView.interests.seeAll'),
      initiativesCard: this.i18n.t('profileView.initiatives.title'),
      initiativesCount: this.i18n.t('profileView.initiatives.count'),
      viewAllInitiatives: this.i18n.t('profileView.initiatives.viewAll'),
      followers: this.i18n.t('profileView.followers.title'),
      followersCount: this.i18n.t('profileView.followers.count'),
      followingCount: this.i18n.t('profileView.following.count'),
      followersEmpty: this.i18n.t('profileView.followers.empty'),
    };
  });

  ngOnInit(): void {
    this.route.paramMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      this.publicUserId.set(params.get('userId'));
      this.activeTab.set('posts');
      this.loadProfile();
      this.loadPosts();
    });
  }

  loadPosts(): void {
    this.postsLoading.set(true);
    this.postsError.set(null);
    this.posts.set([]);
    this.postsNextCursor.set(null);

    const userId = this.publicUserId();
    const request = userId ? this.postsApi.getUserPosts(userId) : this.postsApi.getMyPosts();

    request.pipe(finalize(() => this.postsLoading.set(false))).subscribe({
      next: (page) => {
        this.posts.set(page.items);
        this.postsNextCursor.set(page.nextCursor);
      },
      error: (error: unknown) => {
        this.postsError.set(getApiErrorMessage(error, this.text().postsErrorFallback));
      },
    });
  }

  loadMorePosts(): void {
    const cursor = this.postsNextCursor();
    if (!cursor || this.postsLoadingMore()) {
      return;
    }

    this.postsLoadingMore.set(true);
    this.postsError.set(null);
    const userId = this.publicUserId();
    const request = userId ? this.postsApi.getUserPosts(userId, cursor) : this.postsApi.getMyPosts(cursor);

    request.pipe(finalize(() => this.postsLoadingMore.set(false))).subscribe({
      next: (page) => {
        this.posts.update((current) => [...current, ...page.items]);
        this.postsNextCursor.set(page.nextCursor);
      },
      error: (error: unknown) => {
        this.postsError.set(getApiErrorMessage(error, this.text().postsErrorFallback));
      },
    });
  }

  loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);

    const userId = this.publicUserId();
    const request = userId ? this.profileApi.getUserProfileView(userId) : this.profileApi.getMyProfileView();

    request.pipe(finalize(() => this.loading.set(false))).subscribe({
      next: (profile) => this.profile.set(profile),
      error: (error: unknown) => {
        this.profile.set(null);
        this.error.set(getApiErrorMessage(error, this.text().errorFallback));
      },
    });
  }

  selectTab(tab: ProfileTab): void {
    this.activeTab.set(tab);
  }

  toggleFollow(): void {
    const profile = this.profile();
    if (!profile || this.isOwnProfile() || this.followLoading()) {
      return;
    }

    const wasFollowing = profile.viewerRelation.isFollowing;
    this.error.set(null);
    this.followLoading.set(true);
    const request = wasFollowing
      ? this.profileApi.unfollowUser(profile.basicProfile.userId)
      : this.profileApi.followUser(profile.basicProfile.userId);

    request.pipe(finalize(() => this.followLoading.set(false))).subscribe({
      next: () => {
        this.profile.update((current) => {
          if (!current) {
            return current;
          }

          return {
            ...current,
            stats: {
              ...current.stats,
              followersCount: Math.max(0, current.stats.followersCount + (wasFollowing ? -1 : 1)),
            },
            viewerRelation: {
              ...current.viewerRelation,
              isFollowing: !wasFollowing,
              canFollow: wasFollowing,
            },
          };
        });
      },
      error: (error: unknown) => this.error.set(getApiErrorMessage(error, this.text().errorFallback)),
    });
  }

  messageUser(): void {
    const profile = this.profile();
    if (!profile?.viewerRelation.canMessage || this.messageLoading()) {
      return;
    }

    this.messageLoading.set(true);
    this.error.set(null);
    this.chatApi
      .getOrCreateDirectConversation(profile.basicProfile.userId)
      .pipe(finalize(() => this.messageLoading.set(false)))
      .subscribe({
        next: (conversation) => void this.router.navigate(['/', APP_ROUTES.chat, 'direct', conversation.id]),
        error: (error: unknown) => this.error.set(getApiErrorMessage(error, this.text().errorFallback)),
      });
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
    return new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(new Date(value));
  }

  initiativeStatus(status: InitiativeStatus): string {
    const keys = {
      [InitiativeStatus.Idea]: 'profileView.initiatives.status.idea',
      [InitiativeStatus.Active]: 'profileView.initiatives.status.active',
      [InitiativeStatus.Completed]: 'profileView.initiatives.status.completed',
      [InitiativeStatus.Archived]: 'profileView.initiatives.status.archived',
    } as const;

    return this.i18n.t(keys[status] ?? 'profileView.initiatives.status.idea');
  }

  activityStatusLabel(status: ProfileActivityStatus): string {
    const keys = {
      online: 'profileView.status.online',
      recently_active: 'profileView.status.recentlyActive',
      offline: 'profileView.status.offline',
      unknown: 'profileView.status.unavailable',
    } as const;

    return this.i18n.t(keys[status]);
  }

  profileStatusLabel(status: ProfileStatus): string {
    const keys = {
      active_member: 'profileView.memberStatus.activeMember',
      new_member: 'profileView.memberStatus.newMember',
      community_builder: 'profileView.memberStatus.communityBuilder',
      initiative_creator: 'profileView.memberStatus.initiativeCreator',
    } as const;

    return this.i18n.t(keys[status]);
  }

  formatPostDate(value: string): string {
    const locale = this.i18n.currentLocale() === 'uk' ? 'uk-UA' : 'en-US';
    return new Intl.DateTimeFormat(locale, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(new Date(value));
  }

  previewInitiatives(profile: ProfileViewResponse): ProfileInitiativePreview[] {
    return [...profile.createdInitiatives.slice(0, 2), ...profile.joinedInitiatives.slice(0, 2)].slice(0, 3);
  }

  isCreatedInitiative(profile: ProfileViewResponse, initiativeId: string): boolean {
    return profile.createdInitiatives.some((initiative) => initiative.id === initiativeId);
  }
}
