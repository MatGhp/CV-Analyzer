import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CandidateInfo } from '../../../core/models/resume.model';

@Component({
  selector: 'app-register-prompt-dialog',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './register-prompt-dialog.component.html',
  styleUrl: './register-prompt-dialog.component.scss'
})
export class RegisterPromptDialogComponent {
  @Input() candidateInfo: CandidateInfo | null = null;
  @Input() resumeId: string = '';
  @Input() score: number | null = null;
  
  @Output() registerClicked = new EventEmitter<void>();
  @Output() continueAsGuest = new EventEmitter<void>();

  isClosing = signal(false);

  onRegister(): void {
    this.registerClicked.emit();
  }

  onContinueAsGuest(): void {
    this.isClosing.set(true);
    // Small delay for animation
    setTimeout(() => {
      this.continueAsGuest.emit();
    }, 200);
  }

  onOverlayClick(event: MouseEvent): void {
    // Only close if clicking the overlay itself, not the modal content
    if ((event.target as HTMLElement).classList.contains('modal-overlay')) {
      this.onContinueAsGuest();
    }
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.onContinueAsGuest();
    }
  }
}
