export interface PostAttachment {
  id: string;
  kind: 'image';
  url: string;
  contentType: 'image/jpeg' | 'image/png' | 'image/webp';
  sizeBytes: number;
  order: number;
}
