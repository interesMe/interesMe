import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { APP_ENVIRONMENT } from '../../../../core/constants/app-environment.constants';
import { APP_ROUTES } from '../../../../core/constants/routes.constants';
import { getApiErrorMessage } from '../../../../core/utils/api-error.util';
import { AuthStore } from '../../../auth/state/auth.store';
import { Channel, ChatMessage, DirectConversation, GroupChat } from '../../models';
import { ChatApiService } from '../../services/chat-api.service';

type ThreadType = 'direct' | 'group' | 'channel';
type SelectedChatAttachment = {
  id: string;
  file: File;
  previewUrl: string | null;
};

const MAX_ATTACHMENT_COUNT = 10;
const MAX_ATTACHMENT_SIZE_BYTES = 25 * 1024 * 1024;
const MAX_TOTAL_ATTACHMENT_SIZE_BYTES = 50 * 1024 * 1024;
const IMAGE_CONTENT_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);
const ALLOWED_ATTACHMENT_TYPES = new Map<string, string[]>([
  ['jpg', ['image/jpeg']],
  ['jpeg', ['image/jpeg']],
  ['png', ['image/png']],
  ['webp', ['image/webp']],
  ['pdf', ['application/pdf']],
  ['docx', ['application/vnd.openxmlformats-officedocument.wordprocessingml.document']],
  ['xlsx', ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet']],
  ['txt', ['text/plain']],
  ['zip', ['application/zip', 'application/x-zip-compressed']],
]);

@Component({
  selector: 'app-chat-thread-page',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './chat-thread.html',
  styleUrl: './chat-thread.scss',
})
export class ChatThreadPage implements OnInit, OnDestroy {
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
  readonly selectedImages = signal<SelectedChatAttachment[]>([]);
  readonly composerError = signal<string | null>(null);
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
  readonly canAttachImages = computed(() => this.threadType() === 'direct');
  readonly canSubmit = computed(() => {
    if (!this.canSend() || this.isSending()) {
      return false;
    }

    return Boolean(this.draft().trim()) || this.selectedImages().length > 0;
  });

  ngOnInit(): void {
    const routeType = this.route.snapshot.data['threadType'];
    const paramMap = this.route.snapshot.paramMap;

    if (paramMap.has('groupChatId')) {
      this.threadType.set('group');
    } else if (paramMap.has('channelId')) {
      this.threadType.set('channel');
    } else if (paramMap.has('conversationId')) {
      this.threadType.set('direct');
    } else if (routeType === 'group' || routeType === 'channel' || routeType === 'direct') {
      this.threadType.set(routeType);
    }

    this.loadThread();
  }

  ngOnDestroy(): void {
    this.clearSelectedImages();
  }

  loadThread(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.messages.set([]);
    this.clearSelectedImages();
    this.composerError.set(null);

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
    const images = this.selectedImages();

    if (!text && images.length === 0) {
      this.composerError.set('Write a message or attach an image.');
      return;
    }

    this.isSending.set(true);
    this.errorMessage.set(null);
    this.composerError.set(null);

    const id = this.currentId();
    const request = { text };
    const send$ =
      this.threadType() === 'group'
        ? this.chatApi.sendGroupMessage(id, request)
        : this.chatApi.sendDirectMessage(
            id,
            request,
            images.map((image) => image.file),
          );

    send$.subscribe({
      next: (message) => {
        this.messages.update((messages) => [...messages, message]);
        this.draft.set('');
        this.clearSelectedImages();
        this.isSending.set(false);
      },
      error: (error: unknown) => {
        this.errorMessage.set(getApiErrorMessage(error, 'Could not send message. Please try again.'));
        this.isSending.set(false);
      },
    });
  }

  selectImages(event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    input.value = '';

    if (files.length === 0 || !this.canAttachImages()) {
      return;
    }

    const existingImages = this.selectedImages();
    if (existingImages.length + files.length > MAX_ATTACHMENT_COUNT) {
      this.composerError.set(`You can attach at most ${MAX_ATTACHMENT_COUNT} files.`);
      return;
    }

    const totalSize = existingImages.reduce((sum, image) => sum + image.file.size, 0) +
      files.reduce((sum, file) => sum + file.size, 0);
    if (totalSize > MAX_TOTAL_ATTACHMENT_SIZE_BYTES) {
      this.composerError.set('Attached files must be 50 MiB or smaller in total.');
      return;
    }

    const nextImages: SelectedChatAttachment[] = [];
    for (const file of files) {
      if (!this.isSupportedAttachment(file)) {
        this.composerError.set('Choose JPEG, PNG, WebP, PDF, DOCX, XLSX, TXT, or ZIP files only.');
        this.revokeImages(nextImages);
        return;
      }

      if (file.size <= 0) {
        this.composerError.set('Empty files cannot be attached.');
        this.revokeImages(nextImages);
        return;
      }

      if (file.size > MAX_ATTACHMENT_SIZE_BYTES) {
        this.composerError.set('Each file must be 25 MiB or smaller.');
        this.revokeImages(nextImages);
        return;
      }

      nextImages.push({
        id: crypto.randomUUID(),
        file,
        previewUrl: this.isImageFile(file) ? URL.createObjectURL(file) : null,
      });
    }

    this.selectedImages.set([...existingImages, ...nextImages]);
    this.composerError.set(null);
  }

  removeSelectedImage(imageId: string): void {
    const image = this.selectedImages().find((current) => current.id === imageId);
    if (image?.previewUrl) {
      URL.revokeObjectURL(image.previewUrl);
    }

    this.selectedImages.update((images) => images.filter((current) => current.id !== imageId));
    this.composerError.set(null);
  }

  isOwnMessage(message: ChatMessage): boolean {
    return message.senderUserId === this.authStore.user()?.id;
  }

  senderName(message: ChatMessage): string {
    return message.senderDisplayName || 'InteresMe user';
  }

  hasText(message: ChatMessage): boolean {
    return Boolean(message.text?.trim());
  }

  imageUrl(url: string): string {
    if (/^https?:\/\//i.test(url) || !url.startsWith('/uploads')) {
      return url;
    }

    return `${APP_ENVIRONMENT.apiBaseUrl.replace(/\/api\/?$/, '')}${url}`;
  }

  attachmentUrl(url: string): string {
    return this.imageUrl(url);
  }

  isImageAttachment(contentType: string): boolean {
    return IMAGE_CONTENT_TYPES.has(contentType);
  }

  formatFileSize(sizeBytes: number): string {
    if (sizeBytes < 1024 * 1024) {
      return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`;
    }

    return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  fileExtension(fileName: string): string {
    return fileName.split('.').pop()?.toUpperCase() ?? 'FILE';
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

  private clearSelectedImages(): void {
    this.revokeImages(this.selectedImages());
    this.selectedImages.set([]);
  }

  private revokeImages(images: readonly SelectedChatAttachment[]): void {
    for (const image of images) {
      if (image.previewUrl) {
        URL.revokeObjectURL(image.previewUrl);
      }
    }
  }

  private isSupportedAttachment(file: File): boolean {
    const extension = file.name.split('.').pop()?.toLowerCase() ?? '';
    const contentTypes = ALLOWED_ATTACHMENT_TYPES.get(extension);

    return Boolean(contentTypes?.some((contentType) => contentType.toLowerCase() === file.type.toLowerCase()));
  }

  private isImageFile(file: File): boolean {
    return IMAGE_CONTENT_TYPES.has(file.type);
  }
}
