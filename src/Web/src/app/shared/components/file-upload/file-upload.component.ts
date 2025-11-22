import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpEventType } from '@angular/common/http';
import { environment } from '@environments/environment';
import { TranslatePipe } from '@shared/pipes/translate.pipe';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  template: `
    <div class="file-upload-container">
      <div class="upload-area" [class.dragging]="isDragging"
           (click)="fileInput.click()"
           (dragover)="onDragOver($event)"
           (dragleave)="onDragLeave($event)"
           (drop)="onDrop($event)">

        <div class="upload-icon">📁</div>
        <div class="upload-text">
          <p class="upload-title">{{ 'file-upload.drag-drop' | translate }}</p>
          <p class="upload-subtitle">{{ 'file-upload.or-click' | translate }}</p>
          @if (maxSize) {
            <p class="upload-info">{{ 'file-upload.max-size' | translate }}: {{ maxSize }}MB</p>
          }
          @if (allowedTypes.length > 0) {
            <p class="upload-info">{{ 'file-upload.allowed-types' | translate }}: {{ allowedTypes.join(', ') }}</p>
          }
        </div>

        <input #fileInput
               type="file"
               [multiple]="multiple"
               [accept]="acceptAttribute"
               (change)="onFileSelected($event)"
               hidden>
      </div>

      @if (selectedFiles.length > 0) {
        <div class="selected-files">
          <h4>{{ 'file-upload.selected-files' | translate }}</h4>
          @for (file of selectedFiles; track file.name) {
            <div class="file-item">
              <div class="file-info">
                <span class="file-name">{{ file.name }}</span>
                <span class="file-size">{{ formatFileSize(file.size) }}</span>
              </div>
              <div class="file-actions">
                @if (uploadProgress[file.name] !== undefined) {
                  <div class="progress-bar">
                    <div class="progress-fill" [style.width.%]="uploadProgress[file.name]"></div>
                  </div>
                  <span class="progress-text">{{ uploadProgress[file.name] }}%</span>
                }
                <button class="btn-remove" (click)="removeFile(file)" type="button">✕</button>
              </div>
            </div>
          }
        </div>
      }

      @if (selectedFiles.length > 0 && !uploading) {
        <button class="btn-upload" (click)="upload()" type="button">
          {{ 'file-upload.upload' | translate }}
        </button>
      }

      @if (error) {
        <div class="error-message">{{ error }}</div>
      }
    </div>
  `,
  styles: [`
    .file-upload-container {
      width: 100%;
    }

    .upload-area {
      border: 2px dashed #ccc;
      border-radius: 8px;
      padding: 2rem;
      text-align: center;
      cursor: pointer;
      transition: all 0.3s ease;
      background-color: #fafafa;

      &:hover {
        border-color: #007bff;
        background-color: #f0f8ff;
      }

      &.dragging {
        border-color: #007bff;
        background-color: #e3f2fd;
      }
    }

    .upload-icon {
      font-size: 3rem;
      margin-bottom: 1rem;
    }

    .upload-text {
      .upload-title {
        font-size: 1.125rem;
        font-weight: 500;
        margin-bottom: 0.5rem;
        color: #333;
      }

      .upload-subtitle {
        font-size: 0.875rem;
        color: #666;
        margin-bottom: 0.5rem;
      }

      .upload-info {
        font-size: 0.75rem;
        color: #999;
        margin: 0.25rem 0;
      }
    }

    .selected-files {
      margin-top: 1.5rem;

      h4 {
        font-size: 1rem;
        margin-bottom: 1rem;
        color: #333;
      }
    }

    .file-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.75rem;
      background-color: #f5f5f5;
      border-radius: 4px;
      margin-bottom: 0.5rem;

      .file-info {
        display: flex;
        flex-direction: column;
        flex: 1;

        .file-name {
          font-weight: 500;
          color: #333;
        }

        .file-size {
          font-size: 0.875rem;
          color: #666;
        }
      }

      .file-actions {
        display: flex;
        align-items: center;
        gap: 0.5rem;

        .progress-bar {
          width: 100px;
          height: 6px;
          background-color: #ddd;
          border-radius: 3px;
          overflow: hidden;

          .progress-fill {
            height: 100%;
            background-color: #007bff;
            transition: width 0.3s ease;
          }
        }

        .progress-text {
          font-size: 0.75rem;
          color: #666;
          min-width: 40px;
        }

        .btn-remove {
          background: #dc3545;
          color: white;
          border: none;
          border-radius: 50%;
          width: 24px;
          height: 24px;
          cursor: pointer;
          font-size: 0.875rem;
          display: flex;
          align-items: center;
          justify-content: center;

          &:hover {
            background: #c82333;
          }
        }
      }
    }

    .btn-upload {
      width: 100%;
      margin-top: 1rem;
      padding: 0.75rem;
      background-color: #007bff;
      color: white;
      border: none;
      border-radius: 4px;
      font-size: 1rem;
      cursor: pointer;
      transition: background-color 0.2s;

      &:hover {
        background-color: #0056b3;
      }

      &:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }
    }

    .error-message {
      margin-top: 1rem;
      padding: 0.75rem;
      background-color: #fee;
      color: #c33;
      border-radius: 4px;
      font-size: 0.875rem;
    }
  `]
})
export class FileUploadComponent {
  @Input() multiple = false;
  @Input() maxSize = 10; // MB
  @Input() allowedTypes: string[] = [];
  @Input() folder: string | null = null;

  @Output() filesUploaded = new EventEmitter<any[]>();
  @Output() uploadError = new EventEmitter<string>();

  selectedFiles: File[] = [];
  uploadProgress: { [key: string]: number } = {};
  uploading = false;
  error: string | null = null;
  isDragging = false;

  constructor(private http: HttpClient) {}

  get acceptAttribute(): string {
    return this.allowedTypes.length > 0 ? this.allowedTypes.join(',') : '*';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files) {
      this.addFiles(Array.from(input.files));
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    if (event.dataTransfer?.files) {
      this.addFiles(Array.from(event.dataTransfer.files));
    }
  }

  addFiles(files: File[]): void {
    this.error = null;

    for (const file of files) {
      // Validate file size
      if (file.size > this.maxSize * 1024 * 1024) {
        this.error = `File "${file.name}" exceeds maximum size of ${this.maxSize}MB`;
        continue;
      }

      // Validate file type
      if (this.allowedTypes.length > 0) {
        const extension = '.' + file.name.split('.').pop()?.toLowerCase();
        if (!this.allowedTypes.includes(extension)) {
          this.error = `File type "${extension}" is not allowed`;
          continue;
        }
      }

      if (this.multiple) {
        this.selectedFiles.push(file);
      } else {
        this.selectedFiles = [file];
      }
    }
  }

  removeFile(file: File): void {
    this.selectedFiles = this.selectedFiles.filter(f => f !== file);
    delete this.uploadProgress[file.name];
  }

  async upload(): Promise<void> {
    if (this.selectedFiles.length === 0) return;

    this.uploading = true;
    this.error = null;
    const uploadResults: any[] = [];

    try {
      if (this.multiple && this.selectedFiles.length > 1) {
        // Upload multiple files
        const formData = new FormData();
        this.selectedFiles.forEach(file => formData.append('files', file));
        if (this.folder) formData.append('folder', this.folder);

        const result = await this.uploadMultiple(formData);
        uploadResults.push(...(result.uploaded || []));
      } else {
        // Upload single file or each file individually
        for (const file of this.selectedFiles) {
          const formData = new FormData();
          formData.append('file', file);
          if (this.folder) formData.append('folder', this.folder);

          const result = await this.uploadSingle(formData, file.name);
          uploadResults.push(result);
        }
      }

      this.filesUploaded.emit(uploadResults);
      this.selectedFiles = [];
      this.uploadProgress = {};
    } catch (error: any) {
      this.error = error.message || 'Upload failed';
      this.uploadError.emit(this.error);
    } finally {
      this.uploading = false;
    }
  }

  private uploadSingle(formData: FormData, fileName: string): Promise<any> {
    return new Promise((resolve, reject) => {
      this.http.post(`${environment.apiUrl}/files/upload`, formData, {
        reportProgress: true,
        observe: 'events'
      }).subscribe({
        next: (event) => {
          if (event.type === HttpEventType.UploadProgress && event.total) {
            this.uploadProgress[fileName] = Math.round((100 * event.loaded) / event.total);
          } else if (event.type === HttpEventType.Response) {
            resolve(event.body);
          }
        },
        error: (error) => reject(error)
      });
    });
  }

  private uploadMultiple(formData: FormData): Promise<any> {
    return new Promise((resolve, reject) => {
      this.http.post(`${environment.apiUrl}/files/upload-multiple`, formData).subscribe({
        next: (result) => resolve(result),
        error: (error) => reject(error)
      });
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
  }
}
