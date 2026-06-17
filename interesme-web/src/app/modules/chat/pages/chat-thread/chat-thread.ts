import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthStore } from '../../../auth/state/auth.store';
import { Channel, ChatMessage, DirectConversation, GroupChat } from '../../models';
import { ChatApiService } from '../../services/chat-api.service';

type ThreadType = 'direct' | 'group' | 'channel';

@Component({
  selector: 'app-chat-thread-page',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './chat-thread.html',
  styleUrl: './chat-thread.scss',
})
export class ChatThreadPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly chatApi = inject(ChatApiService);
  private readonly authStore = inject(AuthStore);

  readonly backPath = `/${APP_ROUTES.chat}`;
  readonly threadType = signal<ThreadType>('direct');
  readonly directConversation = signal<DirectConversation | null>(null);
  readonly groupChat = signal<GroupChat | null>(null);
  readonly channel = signal<Channel | null>(null);
  readonly messages = signal<ChatMessage[]>([]);
  readonly draft = signal('');
  readonly isLoading = signal(true);
  readonly isSending = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly title = computed(() => {
    const type = this.threadType();

    if (type === 'group') {
      return this.groupChat()?.title ?? 'Group chat';
    }

    if (type === 'channel') {
      return this.channel()?.title ?? 'Channel';
    }

    const currentUserId = this.authStore.user()?.id;
    const participants = this.directConversation()?.participants ?? [];
    const names = participants
      .filter((participant) => participant.userId !== currentUserId)
      .map((participant) => participant.displayName)
      .filter(Boolean);

    return names.length > 0 ? names.join(', ') : 'Direct message';
  });

  readonly typeLabel = computed(() => {
    switch (this.threadType()) {
      case 'group':
        return 'Group';
      case 'channel':
        return 'Channel';
      default:
        return 'Direct';
    }
  });

  readonly canSend = computed(() => this.threadType() !== 'channel');

  ngOnInit(): void {
    const routeType = this.route.snapshot.data['threadType'];

    if (routeType === 'group' || routeType === 'channel' || routeType === 'direct') {
      this.threadType.set(routeType);
    }

    this.loadThread();
  }

  loadThread(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.messages.set([]);

    switch (this.threadType()) {
      case 'group':
        this.loadGroupThread();
        return;
      case 'channel':
        this.loadChannelThread();
        return;
      default:
        this.loadDirectThread();
    }
  }

  sendMessage(): void {
    if (!this.canSend() || this.isSending()) {
      return;
    }

    const text = this.draft().trim();

    if (!text) {
      return;
    }

    this.isSending.set(true);
    this.errorMessage.set(null);

    const id = this.currentId();
    const request = { text };
    const send$ =
      this.threadType() === 'group'
        ? this.chatApi.sendGroupMessage(id, request)
        : this.chatApi.sendDirectMessage(id, request);

    send$.subscribe({
      next: (message) => {
        this.messages.update((messages) => [...messages, message]);
        this.draft.set('');
        this.isSending.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, 'Could not send message. Please try again.'));
        this.isSending.set(false);
      },
    });
  }

  isOwnMessage(message: ChatMessage): boolean {
    return message.senderUserId === this.authStore.user()?.id;
  }

  senderName(message: ChatMessage): string {
    return message.senderDisplayName || 'InteresMe user';
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }

  private loadDirectThread(): void {
    const conversationId = this.currentId();
    let pendingRequests = 2;

    const finish = (): void => {
      pendingRequests -= 1;

      if (pendingRequests === 0) {
        this.isLoading.set(false);
      }
    };

    this.chatApi.getDirectConversations().subscribe({
      next: (conversations) => {
        this.directConversation.set(
          conversations.find((conversation) => conversation.id === conversationId) ?? null,
        );
        finish();
      },
      error: (error: unknown) => this.handleLoadError(error),
    });

    this.chatApi.getDirectMessages(conversationId).subscribe({
      next: (messages) => {
        this.messages.set(messages);
        finish();
      },
      error: (error: unknown) => this.handleLoadError(error),
    });
  }

  private loadGroupThread(): void {
    const groupChatId = this.currentId();
    let pendingRequests = 2;

    const finish = (): void => {
      pendingRequests -= 1;

      if (pendingRequests === 0) {
        this.isLoading.set(false);
      }
    };

    this.chatApi.getGroupChat(groupChatId).subscribe({
      next: (groupChat) => {
        this.groupChat.set(groupChat);
        finish();
      },
      error: (error: unknown) => this.handleLoadError(error),
    });

    this.chatApi.getGroupMessages(groupChatId).subscribe({
      next: (messages) => {
        this.messages.set(messages);
        finish();
      },
      error: (error: unknown) => this.handleLoadError(error),
    });
  }

  private loadChannelThread(): void {
    this.chatApi.getChannel(this.currentId()).subscribe({
      next: (channel) => {
        this.channel.set(channel);
        this.isLoading.set(false);
      },
      error: (error: unknown) => this.handleLoadError(error),
    });
  }

  private handleLoadError(error: unknown): void {
    this.errorMessage.set(getApiErrorMessage(error, 'Could not load this chat. Please try again.'));
    this.isLoading.set(false);
  }

  private currentId(): string {
    switch (this.threadType()) {
      case 'group':
        return this.route.snapshot.paramMap.get('groupChatId') ?? '';
      case 'channel':
        return this.route.snapshot.paramMap.get('channelId') ?? '';
      default:
        return this.route.snapshot.paramMap.get('conversationId') ?? '';
    }
  }
}
