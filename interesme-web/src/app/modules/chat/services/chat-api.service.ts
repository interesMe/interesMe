import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { CHAT_API_ENDPOINTS } from '../../../core/constants/api.constants';
import { Channel, ChatMessage, DirectConversation, GroupChat, SendMessageRequest } from '../models';

@Injectable({
  providedIn: 'root',
})
export class ChatApiService {
  private readonly http = inject(HttpClient);

  getDirectConversations(): Observable<DirectConversation[]> {
    return this.http.get<DirectConversation[]>(CHAT_API_ENDPOINTS.direct);
  }

  getOrCreateDirectConversation(userId: string): Observable<DirectConversation> {
    return this.http.post<DirectConversation>(CHAT_API_ENDPOINTS.directConversation(userId), {});
  }

  getDirectMessages(conversationId: string, take = 50): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(CHAT_API_ENDPOINTS.directMessages(conversationId, take));
  }

  sendDirectMessage(
    conversationId: string,
    request: SendMessageRequest,
    attachments: readonly File[] = [],
  ): Observable<ChatMessage> {
    if (attachments.length > 0) {
      const formData = new FormData();
      const text = request.text.trim();

      if (text) {
        formData.append('body', text);
      }

      for (const attachment of attachments) {
        formData.append('attachments', attachment);
      }

      return this.http.post<ChatMessage>(CHAT_API_ENDPOINTS.directMessagesBase(conversationId), formData);
    }

    return this.http.post<ChatMessage>(CHAT_API_ENDPOINTS.directMessagesBase(conversationId), request);
  }

  getGroupChats(): Observable<GroupChat[]> {
    return this.http.get<GroupChat[]>(CHAT_API_ENDPOINTS.groupChats);
  }

  getGroupChat(groupChatId: string): Observable<GroupChat> {
    return this.http.get<GroupChat>(CHAT_API_ENDPOINTS.groupChat(groupChatId));
  }

  getGroupMessages(groupChatId: string, take = 50): Observable<ChatMessage[]> {
    return this.http.get<ChatMessage[]>(CHAT_API_ENDPOINTS.groupMessages(groupChatId, take));
  }

  sendGroupMessage(groupChatId: string, request: SendMessageRequest): Observable<ChatMessage> {
    return this.http.post<ChatMessage>(CHAT_API_ENDPOINTS.groupMessagesBase(groupChatId), request);
  }

  getChannels(): Observable<Channel[]> {
    return this.http.get<Channel[]>(CHAT_API_ENDPOINTS.channels);
  }

  getChannel(channelId: string): Observable<Channel> {
    return this.http.get<Channel>(CHAT_API_ENDPOINTS.channel(channelId));
  }
}
