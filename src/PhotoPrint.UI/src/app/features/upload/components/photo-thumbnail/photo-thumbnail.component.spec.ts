import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { PhotoThumbnailComponent } from './photo-thumbnail.component';
import { UploadState } from '../../../../core/models/upload.model';

function makeState(file: File, overrides: Partial<UploadState> = {}): UploadState {
  return {
    clientId: 'c1',
    file,
    progress: 10,
    status: 'uploading',
    quantity: 1,
    ...overrides,
  };
}

describe('PhotoThumbnailComponent — localUrl object-URL lifecycle (F8, review 043-v3)', () => {
  let createSpy: ReturnType<typeof vi.fn>;
  let revokeSpy: ReturnType<typeof vi.fn>;
  let seq: number;

  // Swap ONLY the two static methods (not the whole URL global) so the URL constructor other
  // test files rely on via `new URL(...)` stays intact — replacing URL wholesale hangs them.
  const realCreate = URL.createObjectURL;
  const realRevoke = URL.revokeObjectURL;

  beforeEach(() => {
    seq = 0;
    createSpy = vi.fn(() => `blob:mock/${++seq}`);
    revokeSpy = vi.fn();
    URL.createObjectURL = createSpy as unknown as typeof URL.createObjectURL;
    URL.revokeObjectURL = revokeSpy as unknown as typeof URL.revokeObjectURL;
  });

  afterEach(() => {
    URL.createObjectURL = realCreate;
    URL.revokeObjectURL = realRevoke;
  });

  function create(state: UploadState): PhotoThumbnailComponent {
    const fixture = TestBed.createComponent(PhotoThumbnailComponent);
    fixture.componentInstance.state = state;
    return fixture.componentInstance;
  }

  it('mints the object URL once and reuses it across repeated calls (no per-CD leak)', () => {
    const file = new File(['x'], 'a.jpg', { type: 'image/jpeg' });
    const cmp = create(makeState(file));

    const first = cmp.localUrl();
    const second = cmp.localUrl();
    const third = cmp.localUrl();

    expect(first).toBe(second);
    expect(second).toBe(third);
    expect(createSpy).toHaveBeenCalledTimes(1); // pre-fix: 3 calls, 3 leaked URLs
  });

  it('keeps the same URL when state is reassigned with the same File (upload-progress tick)', () => {
    const file = new File(['x'], 'a.jpg', { type: 'image/jpeg' });
    const cmp = create(makeState(file, { progress: 10 }));
    const url1 = cmp.localUrl();

    // A progress event rebuilds UploadState (new object) but keeps the same File reference.
    cmp.state = makeState(file, { progress: 80 });
    const url2 = cmp.localUrl();

    expect(url2).toBe(url1);
    expect(createSpy).toHaveBeenCalledTimes(1);
  });

  it('prefers a restored previewUrl and never creates an object URL', () => {
    const cmp = create(makeState(undefined as unknown as File, {
      file: undefined,
      previewUrl: 'blob:restored/1',
      status: 'done',
    }));

    expect(cmp.localUrl()).toBe('blob:restored/1');
    expect(createSpy).not.toHaveBeenCalled();
  });

  it('revokes the cached object URL on destroy', () => {
    const file = new File(['x'], 'a.jpg', { type: 'image/jpeg' });
    const cmp = create(makeState(file));
    const url = cmp.localUrl();

    cmp.ngOnDestroy();

    expect(revokeSpy).toHaveBeenCalledWith(url);
  });
});
