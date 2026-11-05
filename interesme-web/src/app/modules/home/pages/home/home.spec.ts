import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Observable, of, throwError } from 'rxjs';

import { AuthService } from '../../../auth/services/auth.service';
import { ChatApiService } from '../../../chat/services/chat-api.service';
import { InitiativesApiService } from '../../../initiatives/services/initiatives-api.service';
import { Post, PostPage } from '../../../posts/models';
import { PostsApiService } from '../../../posts/services/posts-api.service';
import { HomePage } from './home';

function post(id: string, overrides: Partial<Post> = {}): Post {
  return {
    id,
    authorId: 'author-' + id,
    authorDisplayName: 'Author ' + id,
    authorAvatarUrl: null,
    body: 'Body ' + id,
    initiativeId: null,
    initiativeTitle: null,
    likesCount: 0,
    isLikedByCurrentUser: false,
    attachments: [],
    createdAt: '2026-11-05T10:00:00Z',
    ...overrides,
  };
}

// IME-16: home feed built from API data only.
describe('HomePage', () => {
  let fixture: ComponentFixture<HomePage>;
  let feedResponses: Observable<PostPage>[] = [];
  const feedCursors: (string | null)[] = [];

  async function render(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [HomePage],
      providers: [
        provideRouter([]),
        {
          provide: PostsApiService,
          useValue: {
            getFeed: (cursor: string | null) => {
              feedCursors.push(cursor);
              return feedResponses.shift() ?? of({ items: [], nextCursor: null });
            },
            likePost: (postId: string) => of({ postId, likesCount: 1, isLikedByCurrentUser: true }),
            unlikePost: (postId: string) => of({ postId, likesCount: 0, isLikedByCurrentUser: false }),
          },
        },
        { provide: InitiativesApiService, useValue: { getAll: () => of([]) } },
        { provide: ChatApiService, useValue: { getDirectConversations: () => of([]) } },
        { provide: AuthService, useValue: { user: () => ({ id: 'me' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomePage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    feedResponses = [];
    feedCursors.length = 0;
  });

  it('renders posts with author and date, and loads the next page by cursor', async () => {
    feedResponses = [of({ items: [post('1')], nextCursor: 'c1' }), of({ items: [post('2')], nextCursor: null })];

    const element = await render();
    expect(element.textContent).toContain('Author 1');
    expect(element.textContent).toContain('Body 1');
    expect(element.querySelector('time')?.getAttribute('datetime')).toBe('2026-11-05T10:00:00Z');

    fixture.componentInstance.loadMore();
    expect(feedCursors).toEqual([null, 'c1']);
    expect(fixture.componentInstance.posts().map((item) => item.id)).toEqual(['1', '2']);
    expect(fixture.componentInstance.nextCursor()).toBeNull();
  });

  it('shows an empty state without errors when the feed is empty', async () => {
    const element = await render();

    expect(fixture.componentInstance.posts()).toEqual([]);
    expect(fixture.componentInstance.feedError()).toBeNull();
    expect(element.querySelector('.state-card')).not.toBeNull();
    expect(element.querySelector('.post-card')).toBeNull();
  });

  it('shows an error with retry when the feed request fails', async () => {
    feedResponses = [throwError(() => new Error('boom'))];

    const element = await render();

    expect(element.querySelector('.state-card.is-error')).not.toBeNull();
  });

  it('toggles a like using the server state', async () => {
    feedResponses = [of({ items: [post('1')], nextCursor: null })];
    await render();

    fixture.componentInstance.toggleLike(fixture.componentInstance.posts()[0]);

    expect(fixture.componentInstance.posts()[0].likesCount).toBe(1);
    expect(fixture.componentInstance.posts()[0].isLikedByCurrentUser).toBe(true);
  });
});
