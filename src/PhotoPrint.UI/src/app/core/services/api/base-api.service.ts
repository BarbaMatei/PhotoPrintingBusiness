import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type ApiParamValue = string | number | boolean | null | undefined;

export interface ApiOptions {
  params?: Record<string, ApiParamValue>;
  headers?: Record<string, string>;
}

/**
 * Shared HTTP plumbing for the API services: absolute URLs off `environment.apiUrl`,
 * one place that turns an options object into HttpClient's shapes.
 *
 * Authentication and error handling are NOT here — `jwtInterceptor`/`guestInterceptor`
 * attach the credentials and `errorInterceptor` owns the user-facing messages.
 */
@Injectable({ providedIn: 'root' })
export class BaseApiService {
  private readonly http = inject(HttpClient);

  /** Absolute URL for a resource path; the interceptors only match requests that start with the API root. */
  url(path: string): string {
    return path.startsWith('/') ? `${environment.apiUrl}${path}` : `${environment.apiUrl}/${path}`;
  }

  get<T>(path: string, options?: ApiOptions): Observable<T> {
    return this.http.get<T>(this.url(path), this.toRequestOptions(options));
  }

  /** Same as `get`, for endpoints that answer with a file rather than JSON. */
  getBlob(path: string, options?: ApiOptions): Observable<Blob> {
    return this.http.get(this.url(path), {
      ...this.toRequestOptions(options),
      responseType: 'blob',
    });
  }

  post<T>(path: string, body: unknown, options?: ApiOptions): Observable<T> {
    return this.http.post<T>(this.url(path), body, this.toRequestOptions(options));
  }

  put<T>(path: string, body: unknown, options?: ApiOptions): Observable<T> {
    return this.http.put<T>(this.url(path), body, this.toRequestOptions(options));
  }

  patch<T>(path: string, body: unknown, options?: ApiOptions): Observable<T> {
    return this.http.patch<T>(this.url(path), body, this.toRequestOptions(options));
  }

  delete<T>(path: string, options?: ApiOptions): Observable<T> {
    return this.http.delete<T>(this.url(path), this.toRequestOptions(options));
  }

  private toRequestOptions(options?: ApiOptions): {
    params?: Record<string, string>;
    headers?: Record<string, string>;
  } {
    const request: { params?: Record<string, string>; headers?: Record<string, string> } = {};

    if (options?.params) {
      const params: Record<string, string> = {};
      for (const [key, value] of Object.entries(options.params)) {
        if (value !== undefined && value !== null) params[key] = String(value);
      }
      if (Object.keys(params).length > 0) request.params = params;
    }

    if (options?.headers) request.headers = options.headers;

    return request;
  }
}
