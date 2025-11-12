import { Component, signal, viewChild, ElementRef, effect } from '@angular/core';
import { ResumeUploadComponent } from './features/resume-upload/resume-upload.component';
import { ResumeAnalysisComponent } from './features/resume-analysis/resume-analysis.component';
import { AnalysisResponse } from './core/models/resume.model';

@Component({
  selector: 'app-root',
  imports: [ResumeUploadComponent, ResumeAnalysisComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = 'CV Analyzer';
  protected analysisResult = signal<AnalysisResponse | null>(null);
  private resultsSection = viewChild<ElementRef>('resultsSection');

  constructor() {
    effect(() => {
      const result = this.analysisResult();
      const section = this.resultsSection();

      if (result && section) {
        section.nativeElement.scrollIntoView({
          behavior: 'smooth',
          block: 'start'
        });
      }
    });
  }

  onAnalysisComplete(result: AnalysisResponse): void {
    this.analysisResult.set(result);
  }

  onUploadAnother(): void {
    this.analysisResult.set(null);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }
}
