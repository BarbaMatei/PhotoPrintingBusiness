import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEventType, HttpRequest } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { UploadDto } from '../models/upload.model';

export interface UploadEvent {
  type: 'progress' | 'done';
  /** 0–100 when type === 'progress'. */
  progress?: number;
  /** Set when type === 'done'. */
  dto?: UploadDto;
}

export interface BatchUploadItemResult {
  originalFileName: string;
  upload?: UploadDto;
  error?: string;
}

export interface BatchUploadEvent {
  type: 'progress' | 'done';
  /** 0–100 overall when type === 'progress'. */
  progress?: number;
  /** Per-file results when type === 'done'. */
  results?: BatchUploadItemResult[];
}

@Injectable({ providedIn: 'root' })
export class UploadService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/uploads`;

  /**
   * Uploads a single file and emits progress events followed by a 'done' event.
   * Caller is responsible for error handling.
   */
  upload(file: File): Observable<UploadEvent> {
    const form = new FormData();
    form.append('file', file, file.name);

    const req = new HttpRequest('POST', this.base, form, {
      reportProgress: true,
    });

    return this.http.request<UploadDto>(req).pipe(
      map(event => {
        if (event.type === HttpEventType.UploadProgress) {
          const progress = event.total
            ? Math.round((event.loaded / event.total) * 100)
            : 0;
          return { type: 'progress' as const, progress };
        }
        if (event.type === HttpEventType.Response) {
          return { type: 'done' as const, dto: event.body! };
        }
        // Intermediate events (headers sent, etc.) — emit 0 progress
        return { type: 'progress' as const, progress: 0 };
      }),
    );
  }

  /**
   * Uploads multiple files in a single multipart request and emits overall
   * progress followed by a 'done' event carrying per-file results.
   * Files that fail validation on the server are reported with an error string
   * rather than throwing, so successful files in the same batch still succeed.
   */
  uploadBatch(files: File[]): Observable<BatchUploadEvent> {
    const form = new FormData();
    for (const file of files) {
      form.append('files', file, file.name);
    }

    const req = new HttpRequest('POST', `${this.base}/batch`, form, {
      reportProgress: true,
    });

    return this.http.request<BatchUploadItemResult[]>(req).pipe(
      map(event => {
        if (event.type === HttpEventType.UploadProgress) {
          const progress = event.total
            ? Math.round((event.loaded / event.total) * 100)
            : 0;
          return { type: 'progress' as const, progress };
        }
        if (event.type === HttpEventType.Response) {
          return { type: 'done' as const, results: event.body! };
        }
        return { type: 'progress' as const, progress: 0 };
      }),
    );
  }

  /**
   * Fetches a previously-uploaded photo's preview as a blob and returns a
   * local object URL suitable for use in <img [src]>.
   * The caller is responsible for revoking the URL when it is no longer needed.
   */
  getPreviewBlob(id: string): Observable<string> {
    return this.http
      .get(`${this.base}/${id}/preview`, { responseType: 'blob' })
      .pipe(map(blob => URL.createObjectURL(blob)));
  }
}
