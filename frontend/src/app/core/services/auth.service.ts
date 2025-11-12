import { Injectable, signal } from '@angular/core';
import { ERROR_MESSAGES } from '../constants/ui.constants';

export interface User {
  id: string;
  email: string;
  name: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  currentUser = signal<User | null>({
    id: 'default-user',
    email: 'demo@cvanalyzer.com',
    name: 'Demo User'
  });

  getCurrentUserId(): string {
    const user = this.currentUser();
    if (!user) {
      throw new Error(ERROR_MESSAGES.NOT_AUTHENTICATED);
    }
    return user.id;
  }

  /**
   * Checks if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.currentUser() !== null;
  }

  /**
   * Sets the current user (for future auth implementation)
   */
  setUser(user: User | null): void {
    this.currentUser.set(user);
  }

  /**
   * Signs out the current user
   */
  signOut(): void {
    this.currentUser.set(null);
  }
}
