import { Component, output, signal, input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileValidationResult } from '../../core/models/resume.model';
import { ResumeService } from '../../core/services/resume.service';

@Component({
  selector: 'app-file-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './file-upload.component.html',
  styleUrl: './file-upload.component.scss'
})
export class FileUploadComponent {
  private readonly resumeService = inject(ResumeService);

  disabled = input<boolean>(false);
  fileSelected = output<File>();
  fileRemoved = output<void>();

  selectedFile = signal<File | null>(null);
  isDragging = signal(false);
  validationError = signal<string | null>(null);

  onDragOver(event: DragEvent): void {
    if (this.disabled()) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(true);
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    if (this.disabled()) return;
    event.preventDefault();
    event.stopPropagation();
    this.isDragging.set(false);

    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleFile(files[0]);
    }
  }

  onFileSelected(event: Event): void {
    if (this.disabled()) return;
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
    input.value = '';
  }

  private handleFile(file: File): void {
    const validation = this.resumeService.validateFile(file);

    if (!validation.valid) {
      this.validationError.set(validation.error || 'Invalid file');
      this.selectedFile.set(null);
      return;
    }

    this.validationError.set(null);
    this.selectedFile.set(file);
    this.fileSelected.emit(file);
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.validationError.set(null);
    this.fileRemoved.emit();
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
}
