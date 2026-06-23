import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of } from 'rxjs';

import { AuthStore } from '../../../auth/state/auth.store';
import { ChatApiService } from '../../services/chat-api.service';
import { ChatThreadPage } from './chat-thread';

describe('ChatThreadPage', () => {
  let fixture: ComponentFixture<ChatThreadPage>;
  const routeSnapshot = {
    data: { threadType: 'direct' },
    paramMap: convertToParamMap({ conversationId: 'conversation-1' }),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatThreadPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: routeSnapshot,
          },
        },
        {
          provide: ChatApiService,
          useValue: {
            getDirectConversations: () =>
              of([
                {
                  id: 'conversation-1',
                  updatedAt: new Date().toISOString(),
                  participantsCount: 2,
                  lastMessagePreview: null,
                  participants: [
                    { userId: 'current-user', displayName: 'You', joinedAt: new Date().toISOString() },
                    { userId: 'other-user', displayName: 'Alex', joinedAt: new Date().toISOString() },
                  ],
                },
              ]),
            getDirectMessages: () => of([]),
            sendDirectMessage: () => of(null),
          },
        },
        {
          provide: AuthStore,
          useValue: {
            user: () => ({ id: 'current-user' }),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatThreadPage);
    fixture.detectChanges();
  });

  it('renders an enabled attach image button for direct chats', () => {
    const nativeElement = fixture.nativeElement as HTMLElement;

    expect(fixture.componentInstance.threadType()).toBe('direct');
    expect(fixture.componentInstance.canAttachImages()).toBe(true);
    expect(routeSnapshot.paramMap.get('conversationId')).toBe('conversation-1');
    expect(fixture.componentInstance.selectedImages().length).toBe(0);

    const composer = nativeElement.querySelector('.composer');
    const input = nativeElement.querySelector<HTMLInputElement>('.attachment-input');
    const button = nativeElement.querySelector<HTMLButtonElement>('.attachment-button');

    expect(composer).not.toBeNull();
    expect(input).not.toBeNull();
    expect(button).not.toBeNull();
    expect(button?.disabled).toBe(false);
  });
});
