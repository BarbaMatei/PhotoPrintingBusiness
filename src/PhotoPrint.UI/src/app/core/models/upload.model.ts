export interface UploadDto {
  id: string;
  originalFileName: string;
  contentType: string;
  widthPx: number;
  heightPx: number;
  fileSizeBytes: number;
  uploadedAt: string;
}

export interface UploadProgress {
  uploadId: string; // client-assigned temp ID before server responds
  fileName: string;
  progress: number; // 0–100
}

export type UploadStatus = 'pending' | 'uploading' | 'done' | 'error';

export interface UploadState {
  /** Client-assigned temporary ID (used as key before server responds). */
  clientId: string;
  /** The original File object. Absent for states restored from sessionStorage. */
  file?: File;
  /** 0–100 during upload, 100 when complete. */
  progress: number;
  status: UploadStatus;
  /** Set once the server confirms the upload. */
  dto?: UploadDto;
  /** Human-readable error message when status === 'error'. */
  error?: string;
  /** Per-photo print quantity (default: 1). */
  quantity: number;
  /** Blob object URL for restored uploads (replaces URL.createObjectURL(file)). */
  previewUrl?: string;
}
