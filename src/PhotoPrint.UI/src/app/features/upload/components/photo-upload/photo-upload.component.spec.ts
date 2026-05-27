import { PhotoUploadComponent, FileValidationError, MAX_FILE_SIZE_BYTES, MAX_UPLOAD_COUNT, getExtension } from './photo-upload.component';

function makeFile(name: string, sizeBytes = 1024): File {
  const blob = new Blob([new Uint8Array(sizeBytes)], { type: 'image/jpeg' });
  return new File([blob], name, { type: 'image/jpeg' });
}

describe('PhotoUploadComponent – processFiles', () => {
  let component: PhotoUploadComponent;
  let accepted: File[][];
  let rejected: FileValidationError[][];

  beforeEach(() => {
    component = new PhotoUploadComponent();
    accepted = [];
    rejected = [];
    component.filesAccepted.subscribe((f: File[]) => accepted.push(f));
    component.filesRejected.subscribe((e: FileValidationError[]) => rejected.push(e));
  });

  it('accepts a valid JPEG file', () => {
    component.processFiles([makeFile('photo.jpg')]);
    expect(accepted.length).toBe(1);
    expect(accepted[0].length).toBe(1);
    expect(rejected.length).toBe(0);
  });

  it('rejects a PDF file with reason "type"', () => {
    component.processFiles([makeFile('document.pdf')]);
    expect(accepted.length).toBe(0);
    expect(rejected.length).toBe(1);
    expect(rejected[0][0].reason).toBe('type');
    expect(rejected[0][0].fileName).toBe('document.pdf');
  });

  it('rejects a file larger than 50 MB with reason "size"', () => {
    const bigFile = makeFile('big.jpg', MAX_FILE_SIZE_BYTES + 1);
    component.processFiles([bigFile]);
    expect(accepted.length).toBe(0);
    expect(rejected.length).toBe(1);
    expect(rejected[0][0].reason).toBe('size');
  });

  it('accepts a file exactly at the size limit', () => {
    const exactFile = makeFile('exact.jpg', MAX_FILE_SIZE_BYTES);
    component.processFiles([exactFile]);
    expect(accepted.length).toBe(1);
  });

  it('rejects a file when currentUploadCount is already at MAX_UPLOAD_COUNT with reason "limit"', () => {
    component.currentUploadCount = MAX_UPLOAD_COUNT;
    component.processFiles([makeFile('extra.jpg')]);
    expect(accepted.length).toBe(0);
    expect(rejected.length).toBe(1);
    expect(rejected[0][0].reason).toBe('limit');
  });

  it('rejects the file that exceeds MAX_UPLOAD_COUNT across the batch', () => {
    component.currentUploadCount = MAX_UPLOAD_COUNT - 1;
    const files = [makeFile('a.jpg'), makeFile('b.jpg')];
    component.processFiles(files);
    // First accepted, second rejected
    expect(accepted[0].length).toBe(1);
    expect(rejected[0][0].reason).toBe('limit');
  });

  it('accepts .jpeg extension', () => {
    component.processFiles([makeFile('photo.jpeg')]);
    expect(accepted.length).toBe(1);
  });

  it('accepts .png extension', () => {
    component.processFiles([makeFile('photo.png')]);
    expect(accepted.length).toBe(1);
  });

  it('accepts .heic extension', () => {
    component.processFiles([makeFile('photo.heic')]);
    expect(accepted.length).toBe(1);
  });

  it('rejects mixed: type error + valid in same batch', () => {
    component.processFiles([makeFile('doc.bmp'), makeFile('good.jpg')]);
    expect(accepted[0].length).toBe(1);
    expect(rejected[0].length).toBe(1);
    expect(rejected[0][0].reason).toBe('type');
  });
});

describe('getExtension', () => {
  it('returns lowercase extension with dot', () => {
    expect(getExtension('Photo.JPG')).toBe('.jpg');
  });

  it('returns empty string for files with no extension', () => {
    expect(getExtension('noextension')).toBe('');
  });

  it('handles multiple dots', () => {
    expect(getExtension('my.photo.backup.jpg')).toBe('.jpg');
  });
});
