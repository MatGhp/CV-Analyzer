export interface User {
  userId: string;
  email: string;
  fullName: string;
  phone?: string;
  createdAt: Date;
  lastLoginAt?: Date;
  isEmailVerified: boolean;
  totalResumes: number;
}

export interface RegisterRequest {
  email: string;
  password: string;
  fullName: string;
  phone?: string;
  guestSessionToken?: string;
}

export interface RegisterResponse {
  userId: string;
  token: string;
  migratedGuestData: boolean;
  migratedResumeCount: number;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  userId: string;
  token: string;
  fullName: string;
  email: string;
  lastLoginAt: Date;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
}
