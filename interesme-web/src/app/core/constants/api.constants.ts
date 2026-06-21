import { APP_ENVIRONMENT } from './app-environment.constants';

export const API_BASE_URL = APP_ENVIRONMENT.apiBaseUrl;

export const AUTH_API_ENDPOINTS = {
  login: `${API_BASE_URL}/auth/login`,
  register: `${API_BASE_URL}/auth/register`,
  refreshToken: `${API_BASE_URL}/auth/refresh`,
  googleLogin: `${API_BASE_URL}/auth/google`,
  githubLogin: `${API_BASE_URL}/auth/github`,
  githubSession: `${API_BASE_URL}/auth/github/session`,
} as const;

export const PROFILE_API_ENDPOINTS = {
  me: `${API_BASE_URL}/profile/me`,
  myInterests: `${API_BASE_URL}/profile/me/interests`,
  myView: `${API_BASE_URL}/users/me/profile-view`,
  userView: (userId: string) => `${API_BASE_URL}/users/${userId}/profile-view`,
  follow: (userId: string) => `${API_BASE_URL}/users/${userId}/follow`,
} as const;

export const HISTORY_API_ENDPOINTS = {
  myHistory: `${API_BASE_URL}/users/me/history`,
} as const;

export const POSTS_API_ENDPOINTS = {
  create: `${API_BASE_URL}/posts`,
  byId: (postId: string) => `${API_BASE_URL}/posts/${postId}`,
  myPosts: `${API_BASE_URL}/users/me/posts`,
  userPosts: (userId: string) => `${API_BASE_URL}/users/${userId}/posts`,
} as const;

export const INTERESTS_API_ENDPOINTS = {
  list: `${API_BASE_URL}/interests`,
  catalog: `${API_BASE_URL}/interests/catalog`,
} as const;

export const INITIATIVES_API_ENDPOINTS = {
  list: `${API_BASE_URL}/initiatives`,
  create: `${API_BASE_URL}/initiatives`,
  byId: (id: string) => `${API_BASE_URL}/initiatives/${id}`,
  joinRequests: (initiativeId: string) => `${API_BASE_URL}/initiatives/${initiativeId}/join-requests`,
  acceptJoinRequest: (initiativeId: string, requestId: string) =>
    `${API_BASE_URL}/initiatives/${initiativeId}/join-requests/${requestId}/accept`,
  rejectJoinRequest: (initiativeId: string, requestId: string) =>
    `${API_BASE_URL}/initiatives/${initiativeId}/join-requests/${requestId}/reject`,
} as const;

export const PUBLIC_INITIATIVES_API_ENDPOINTS = {
  bySlug: (slug: string) => `${API_BASE_URL}/public/initiatives/${slug}`,
} as const;

export const CHAT_API_ENDPOINTS = {
  direct: `${API_BASE_URL}/chats/direct`,
  directConversation: (userId: string) => `${API_BASE_URL}/chats/direct/${userId}`,
  directMessagesBase: (conversationId: string) => `${API_BASE_URL}/chats/direct/${conversationId}/messages`,
  directMessages: (conversationId: string, take = 50) =>
    `${API_BASE_URL}/chats/direct/${conversationId}/messages?take=${take}`,
  groupChats: `${API_BASE_URL}/chats/groups`,
  groupChat: (groupChatId: string) => `${API_BASE_URL}/chats/groups/${groupChatId}`,
  groupMessagesBase: (groupChatId: string) => `${API_BASE_URL}/chats/groups/${groupChatId}/messages`,
  groupMessages: (groupChatId: string, take = 50) =>
    `${API_BASE_URL}/chats/groups/${groupChatId}/messages?take=${take}`,
  channels: `${API_BASE_URL}/chats/channels`,
  channel: (channelId: string) => `${API_BASE_URL}/chats/channels/${channelId}`,
} as const;
