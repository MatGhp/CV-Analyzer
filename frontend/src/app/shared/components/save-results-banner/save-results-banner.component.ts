import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-save-results-banner',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './save-results-banner.component.html',
  styleUrl: './save-results-banner.component.scss'
})
export class SaveResultsBannerComponent {
  isAnonymous = input.required<boolean>();
  onSaveClick = output<void>();

  handleSaveClick(): void {
    this.onSaveClick.emit();
  }
}
