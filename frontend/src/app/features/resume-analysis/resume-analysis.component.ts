import { Component, input, output, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalysisResponse, SuggestionDto } from '../../core/models/resume.model';
import { UI_TIMING, ERROR_MESSAGES, SUCCESS_MESSAGES } from '../../core/constants/ui.constants';

@Component({
  selector: 'app-resume-analysis',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './resume-analysis.component.html',
  styleUrl: './resume-analysis.component.scss'
})
export class ResumeAnalysisComponent {
  analysis = input.required<AnalysisResponse>();
  uploadAnother = output<void>();

  showOptimized = signal(false);
  copied = signal(false);
  copyError = signal<string | null>(null);

  scoreClass = computed(() => {
    const score = this.analysis().score;
    if (score < 50) return 'score-low';
    if (score < 75) return 'score-medium';
    return 'score-high';
  });

  suggestionsByCategory = computed(() => {
    const suggestions = this.analysis().suggestions;
    const grouped = new Map<string, SuggestionDto[]>();

    suggestions.forEach(suggestion => {
      const category = suggestion.category;
      if (!grouped.has(category)) {
        grouped.set(category, []);
      }
      grouped.get(category)!.push(suggestion);
    });

    grouped.forEach(items => {
      items.sort((a, b) => b.priority - a.priority);
    });

    return Array.from(grouped.entries()).map(([name, suggestions]) => ({
      name,
      suggestions
    }));
  });

  copyButtonText = computed(() =>
    this.copied() ? SUCCESS_MESSAGES.COPIED : SUCCESS_MESSAGES.COPY_TO_CLIPBOARD
  );

  toggleOptimized(): void {
    this.showOptimized.update(v => !v);
  }

  async copyToClipboard(): Promise<void> {
    const content = this.analysis().optimizedContent;
    if (!content) return;

    try {
      await navigator.clipboard.writeText(content);
      this.copied.set(true);
      this.copyError.set(null);
      setTimeout(() => this.copied.set(false), UI_TIMING.COPY_SUCCESS_DURATION);
    } catch (error) {
      console.error('Failed to copy:', error);
      this.copyError.set(ERROR_MESSAGES.COPY_FAILED);
      setTimeout(() => this.copyError.set(null), UI_TIMING.COPY_ERROR_DURATION);
    }
  }

  formatDate(dateString: string): string {
    try {
      const date = new Date(dateString);
      return date.toLocaleString();
    } catch {
      return dateString;
    }
  }
}
