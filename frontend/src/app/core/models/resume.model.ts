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
  category: string;
  description: string;
  priority: number;
}

export interface UploadResumeRequest {
  userId: string;
  file: File;
}

export interface AnalyzeResumeResponse {
  score: number;
  optimizedContent: string;
  suggestions: Suggestion[];
  metadata: Record<string, any>;
}
