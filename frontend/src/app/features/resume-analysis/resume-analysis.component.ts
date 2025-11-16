import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AnalysisResponse, SuggestionDto, ResumeStatusResponse } from '../../core/models/resume.model';
import { ResumeService } from '../../core/services/resume.service';
import { CandidateInfoCardComponent } from '../../shared/components/candidate-info-card/candidate-info-card.component';
import { UI_TIMING, ERROR_MESSAGES, SUCCESS_MESSAGES } from '../../core/constants/ui.constants';

@Component({
  selector: 'app-resume-analysis',
  standalone: true,
  imports: [CommonModule, CandidateInfoCardComponent],
  templateUrl: './resume-analysis.component.html',
  styleUrl: './resume-analysis.component.scss'
})
export class ResumeAnalysisComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly resumeService = inject(ResumeService);

  resumeId = signal<string>('');
  status = signal<ResumeStatusResponse | null>(null);
  analysis = signal<AnalysisResponse | null>(null);
  isLoading = signal<boolean>(true);
  errorMessage = signal<string | null>(null);

  showOptimized = signal(false);
  copied = signal(false);
  copyError = signal<string | null>(null);

  scoreClass = computed(() => {
    const analysisData = this.analysis();
    if (!analysisData?.score) return 'score-medium';
    const score = analysisData.score;
    if (score < 50) return 'score-low';
    if (score < 75) return 'score-medium';
    return 'score-high';
  });

  suggestionsByCategory = computed(() => {
    const analysisData = this.analysis();
    if (!analysisData) return [];
    
    const suggestions = analysisData.suggestions;
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

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.errorMessage.set('Invalid resume ID');
      this.isLoading.set(false);
      return;
    }

    this.resumeId.set(id);
    this.startPolling();
  }

  private startPolling(): void {
    this.resumeService.pollResumeStatus(this.resumeId())
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (statusUpdate) => {
          this.status.set(statusUpdate);
          
          if (statusUpdate.status === 'complete') {
            this.loadAnalysis();
          }
          
          if (statusUpdate.status === 'failed') {
            this.errorMessage.set(statusUpdate.errorMessage || 'Analysis failed');
            this.isLoading.set(false);
          }
        },
        error: (error) => {
          console.error('Polling error:', error);
          this.errorMessage.set('Failed to check status. Please refresh the page.');
          this.isLoading.set(false);
        }
      });
  }

  private loadAnalysis(): void {
    this.resumeService.getAnalysis(this.resumeId())
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (analysisData) => {
          this.analysis.set(analysisData);
          this.isLoading.set(false);
        },
        error: (error) => {
          console.error('Failed to load analysis:', error);
          this.errorMessage.set('Failed to load analysis results');
          this.isLoading.set(false);
        }
      });
  }

  retryAnalysis(): void {
    this.errorMessage.set(null);
    this.isLoading.set(true);
    this.startPolling();
  }

  goToUpload(): void {
    this.router.navigate(['/upload']);
  }

  toggleOptimized(): void {
    this.showOptimized.update(v => !v);
  }

  async copyToClipboard(): Promise<void> {
    const analysisData = this.analysis();
    const content = analysisData?.optimizedContent;
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
