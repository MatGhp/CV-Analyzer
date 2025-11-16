export interface Resume {
  id: string;
  userId: string;
  fileName: string;
  blobUrl: string;
  content: string;
  score?: number;
  status: ResumeStatus;
  uploadedAt: Date;
  analyzedAt?: Date;
  suggestions: Suggestion[];
}

export enum ResumeStatus {
  Uploaded = 'Uploaded',
  Processing = 'Processing',
  Analyzed = 'Analyzed',
  Failed = 'Failed'
}

export interface Suggestion {
  id: string;
  resumeId: string;
  category: SuggestionCategory;
  description: string;
  priority: SuggestionPriority; // 1-5, higher = more important
}

export type SuggestionCategory =
  | 'Formatting'
  | 'Keywords'
  | 'Experience'
  | 'Clarity'
  | 'Structure'
  | 'Content';

export type SuggestionPriority = 1 | 2 | 3 | 4 | 5;

export interface UploadResumeRequest {
  userId?: string;  // Optional - will be generated from resume if not provided
  file: File;
}

export interface UploadResumeResponse {
  id: string;
}

// New interfaces for async processing (Task 5)
export interface UploadResponse {
  resumeId: string;
  status: string;
  message: string;
}

export interface ResumeStatusResponse {
  resumeId: string;
  status: 'pending' | 'processing' | 'complete' | 'failed';
  progress: number;
  errorMessage?: string;
}

export interface CandidateInfo {
  fullName: string;
  email: string;
  phone?: string;
  location?: string;
  skills: string[];
  yearsOfExperience?: number;
  currentJobTitle?: string;
  education?: string;
}

export interface AnalysisResponse {
  id: string;
  fileName: string;
  score?: number;
  uploadedAt: string;
  analyzedAt?: string;
  status: string;
  candidateInfo?: CandidateInfo;
  suggestions: SuggestionDto[];
  optimizedContent?: string;
  metadata?: AnalysisMetadata;
}

export interface SuggestionDto {
  category: SuggestionCategory;
  description: string;
  priority: SuggestionPriority;
}

export interface AnalysisMetadata {
  analyzedAt?: string;
  model?: string;
  processingTimeMs?: number;
}

// Client-side file validation
export interface FileValidationResult {
  valid: boolean;
  error?: string;
}

export const FILE_CONSTRAINTS = {
  maxSizeBytes: 5 * 1024 * 1024, // 5MB
  allowedTypes: ['application/pdf', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'],
  allowedExtensions: ['.pdf', '.docx']
} as const;
