import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthStore } from '../../../auth/state/auth.store';
import { Channel, DirectConversation, GroupChat } from '../../models';
import { ChatApiService } from '../../services/chat-api.service';

@Component({
  selector: 'app-chat-list-page',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './chat-list.html',
  styleUrl: './chat-list.scss',
})
export class ChatListPage implements OnInit {
  private readonly chatApi = inject(ChatApiService);
  private readonly authStore = inject(AuthStore);

  readonly directConversations = signal<DirectConversation[]>([]);
  readonly groupChats = signal<GroupChat[]>([]);
  readonly channels = signal<Channel[]>([]);
  readonly isDirectLoading = signal(true);
  readonly isGroupsLoading = signal(true);
  readonly isChannelsLoading = signal(true);
  readonly directErrorMessage = signal<string | null>(null);
  readonly groupsErrorMessage = signal<string | null>(null);
  readonly channelsErrorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadChats();
  }

  loadChats(): void {
    this.loadDirectConversations();
    this.loadGroupChats();
    this.loadChannels();
  }

  private loadDirectConversations(): void {
    this.isDirectLoading.set(true);
    this.directErrorMessage.set(null);

    this.chatApi.getDirectConversations().subscribe({
      next: (conversations) => {
        this.directConversations.set(conversations);
        this.isDirectLoading.set(false);
      },
      error: (error: unknown) => {
        this.directConversations.set([]);
        this.directErrorMessage.set(getApiErrorMessage(error, 'Could not load direct messages.'));
        this.isDirectLoading.set(false);
      },
    });
  }

  private loadGroupChats(): void {
    this.isGroupsLoading.set(true);
    this.groupsErrorMessage.set(null);

    this.chatApi.getGroupChats().subscribe({
      next: (groups) => {
        this.groupChats.set(groups);
        this.isGroupsLoading.set(false);
      },
      error: (error: unknown) => {
        this.groupChats.set([]);
        this.groupsErrorMessage.set(getApiErrorMessage(error, 'Could not load group chats.'));
        this.isGroupsLoading.set(false);
      },
    });
  }

  private loadChannels(): void {
    this.isChannelsLoading.set(true);
    this.channelsErrorMessage.set(null);

    this.chatApi.getChannels().subscribe({
      next: (channels) => {
        this.channels.set(channels);
        this.isChannelsLoading.set(false);
      },
      error: (error: unknown) => {
        this.channels.set([]);
        this.channelsErrorMessage.set(getApiErrorMessage(error, 'Could not load channels.'));
        this.isChannelsLoading.set(false);
      },
    });
  }

  directTitle(conversation: DirectConversation): string {
    const currentUserId = this.authStore.user()?.id;
    const otherParticipants = conversation.participants.filter((participant) => participant.userId !== currentUserId);
    const names = otherParticipants.map((participant) => participant.displayName).filter(Boolean);

    return names.length > 0 ? names.join(', ') : 'Direct message';
  }

  directPath(conversation: DirectConversation): string {
    return `/${APP_ROUTES.chat}/direct/${conversation.id}`;
  }

  groupPath(groupChat: GroupChat): string {
    return `/${APP_ROUTES.chat}/groups/${groupChat.id}`;
  }

  channelPath(channel: Channel): string {
    return `/${APP_ROUTES.chat}/channels/${channel.id}`;
  }

  formatDate(value: string): string {
    return new Intl.DateTimeFormat(undefined, {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(new Date(value));
  }
}
