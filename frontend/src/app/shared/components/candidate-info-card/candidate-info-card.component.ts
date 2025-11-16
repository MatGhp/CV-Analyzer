import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CandidateInfo } from '../../../core/models/resume.model';

@Component({
  selector: 'app-candidate-info-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './candidate-info-card.component.html',
  styleUrl: './candidate-info-card.component.scss'
})
export class CandidateInfoCardComponent {
  candidateInfo = input.required<CandidateInfo>();
}
